import { Component, Input, OnInit, OnDestroy, OnChanges, SimpleChanges } from '@angular/core';
import { ChartConfiguration, ChartType } from 'chart.js';
import { Subject, takeUntil } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { DistributionDto } from '../../shared/models/dashboard.model';
import {
  getChartColors,
  getChartColorsTranslucent,
  DEFAULT_BAR_OPTIONS,
  DEFAULT_HORIZONTAL_BAR_OPTIONS,
  DEFAULT_PIE_OPTIONS,
  DEFAULT_DOUGHNUT_OPTIONS,
} from './chart-theme';

type SupportedChartType = 'bar' | 'pie' | 'doughnut';

@Component({
  selector: 'app-distribution-chart-widget',
  templateUrl: './distribution-chart-widget.component.html',
  styleUrls: ['./distribution-chart-widget.component.scss'],
})
export class DistributionChartWidgetComponent implements OnInit, OnDestroy, OnChanges {
  @Input() field: 'type' | 'state' | 'priority' | 'module' = 'type';

  chartType: SupportedChartType = 'doughnut';
  chartData: ChartConfiguration['data'] = { labels: [], datasets: [] };
  chartOptions: ChartConfiguration['options'] = {};
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
        this.chartOptions = { ...DEFAULT_DOUGHNUT_OPTIONS };
        break;
      case 'state':
        this.chartType = 'bar';
        this.chartOptions = { ...DEFAULT_HORIZONTAL_BAR_OPTIONS };
        break;
      case 'priority':
        this.chartType = 'pie';
        this.chartOptions = { ...DEFAULT_PIE_OPTIONS };
        break;
      case 'module':
        this.chartType = 'bar';
        this.chartOptions = { ...DEFAULT_BAR_OPTIONS };
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
      return;
    }

    const labels = data.map(d => d.label || '(empty)');
    const values = data.map(d => d.count);
    const colors = getChartColors(data.length);
    const bgColors = getChartColorsTranslucent(data.length);

    if (this.chartType === 'bar') {
      this.chartData = {
        labels,
        datasets: [
          {
            data: values,
            backgroundColor: bgColors,
            borderColor: colors,
            borderWidth: 1,
          },
        ],
      };
    } else {
      this.chartData = {
        labels,
        datasets: [
          {
            data: values,
            backgroundColor: colors,
            hoverBackgroundColor: bgColors,
          },
        ],
      };
    }
  }

  onChartClick(event: { active?: Array<{ index: number }> }): void {
    if (event.active && event.active.length > 0) {
      const idx = event.active[0].index;
      const label = (this.chartData.labels as string[])?.[idx] ?? 'unknown';
      console.log(`[DistributionChart] Clicked segment: field=${this.field}, category=${label}`);
    }
  }
}
