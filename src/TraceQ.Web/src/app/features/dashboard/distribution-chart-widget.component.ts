import { Component, Input, OnInit, OnDestroy, OnChanges, SimpleChanges } from '@angular/core';
import { ChartData, ChartEvent, ChartOptions, ChartType } from 'chart.js';
import { Subject, takeUntil } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { DistributionDto } from '../../shared/models/dashboard.model';
import { getChartColors, getChartColorsTranslucent } from './chart-theme';

@Component({
  selector: 'app-distribution-chart-widget',
  templateUrl: './distribution-chart-widget.component.html',
  styleUrls: ['./distribution-chart-widget.component.scss'],
})
export class DistributionChartWidgetComponent implements OnInit, OnDestroy, OnChanges {
  @Input() field: 'type' | 'state' | 'priority' | 'module' = 'type';

  chartType: ChartType = 'doughnut';
  chartLabels: string[] = [];
  chartDatasets: ChartData['datasets'] = [];
  chartOptions: ChartOptions = {};
  hasData = false;
  isLoading = true;
  errorMessage = '';

  private destroy$ = new Subject<void>();

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.configureChart();
    this.loadData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['field'] && !changes['field'].firstChange) {
      this.configureChart();
      this.loadData();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  refresh(): void {
    this.loadData();
  }

  private configureChart(): void {
    switch (this.field) {
      case 'type':
        this.chartType = 'doughnut';
        this.chartOptions = {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { position: 'right' } },
        };
        break;
      case 'state':
        this.chartType = 'bar';
        this.chartOptions = {
          responsive: true,
          maintainAspectRatio: false,
          indexAxis: 'y',
          plugins: { legend: { display: false } },
          scales: {
            x: { beginAtZero: true, ticks: { precision: 0 } },
            y: { grid: { display: false } },
          },
        };
        break;
      case 'priority':
        this.chartType = 'pie';
        this.chartOptions = {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { position: 'right' } },
        };
        break;
      case 'module':
        this.chartType = 'bar';
        this.chartOptions = {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            x: { grid: { display: false } },
            y: { beginAtZero: true, ticks: { precision: 0 } },
          },
        };
        break;
    }
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.dashboardService
      .getDistribution(this.field)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.buildChart(items);
          this.isLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load distribution data.';
          this.isLoading = false;
        },
      });
  }

  private buildChart(items: DistributionDto[]): void {
    let data = items.filter(d => d.count > 0);

    if (this.field === 'module') {
      data = data.sort((a, b) => b.count - a.count).slice(0, 15);
    }

    this.hasData = data.length > 0;
    if (!this.hasData) {
      this.chartLabels = [];
      this.chartDatasets = [];
      return;
    }

    const labels = data.map(d => d.label || '(empty)');
    const values = data.map(d => d.count);
    const colors = getChartColors(data.length);
    const bgColors = getChartColorsTranslucent(data.length);

    this.chartLabels = labels;

    if (this.chartType === 'bar') {
      this.chartDatasets = [
        {
          data: values,
          backgroundColor: bgColors,
          borderColor: colors,
          borderWidth: 1,
        },
      ];
    } else {
      this.chartDatasets = [
        {
          data: values,
          backgroundColor: colors,
          hoverBackgroundColor: bgColors,
        },
      ];
    }
  }

  onChartClick(event: { event?: ChartEvent; active?: object[] }): void {
    if (event.active && event.active.length > 0) {
      const activeEl = event.active[0] as { index?: number };
      const idx = activeEl.index ?? 0;
      const label = this.chartLabels[idx] ?? 'unknown';
      console.log(`[DistributionChart] Clicked segment: field=${this.field}, category=${label}`);
    }
  }
}
