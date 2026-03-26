import { Component, OnInit, OnDestroy } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { Subject, takeUntil } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { TraceabilityCoverageDto, DistributionDto } from '../../shared/models/dashboard.model';
import { RequirementDto } from '../../shared/models/requirement.model';
import { TraceabilityCoverageReport } from '../../shared/models/report.model';
import { getChartColors } from './chart-theme';

@Component({
  selector: 'app-traceability-widget',
  templateUrl: './traceability-widget.component.html',
  styleUrls: ['./traceability-widget.component.scss'],
})
export class TraceabilityWidgetComponent implements OnInit, OnDestroy {
  coveragePercent = 0;
  totalRequirements = 0;
  tracedRequirements = 0;
  untracedRequirements: RequirementDto[] = [];
  untracedDisplayedColumns: string[] = ['requirementNumber', 'name', 'module'];

  densityChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  densityChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, ticks: { precision: 0 } },
    },
  };

  isLoading = true;
  errorMessage = '';

  private destroy$ = new Subject<void>();

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  refresh(): void {
    this.loadData();
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.dashboardService
      .getTraceabilityCoverage()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.applyData(data);
          this.isLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load traceability data.';
          this.isLoading = false;
        },
      });
  }

  private applyData(data: TraceabilityCoverageDto & Partial<TraceabilityCoverageReport>): void {
    this.coveragePercent = Math.round(data.coveragePercentage);
    this.totalRequirements = data.totalRequirements;
    this.tracedRequirements = data.tracedRequirements;
    this.untracedRequirements = (data.untracedRequirements ?? []).slice(0, 10);

    const dist = data.traceLinkDistribution ?? [];
    this.buildDensityChart(dist);
  }

  private buildDensityChart(dist: DistributionDto[]): void {
    const labels = dist.map(d => d.label);
    const values = dist.map(d => d.count);
    const colors = getChartColors(dist.length);

    this.densityChartData = {
      labels,
      datasets: [
        {
          data: values,
          backgroundColor: colors,
          borderWidth: 0,
        },
      ],
    };
  }
}
