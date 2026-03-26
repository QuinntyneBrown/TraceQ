import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { DashboardService, RequirementStats } from './dashboard.service';

@Component({
  selector: 'app-stats-widget',
  templateUrl: './stats-widget.component.html',
  styleUrls: ['./stats-widget.component.scss'],
})
export class StatsWidgetComponent implements OnInit, OnDestroy {
  stats: RequirementStats | null = null;
  coveragePercent = 0;
  embeddedPercent = 0;
  isLoading = true;
  errorMessage = '';

  private destroy$ = new Subject<void>();

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loadStats();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  refresh(): void {
    this.loadStats();
  }

  private loadStats(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.dashboardService
      .getRequirementStats()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats) => {
          this.stats = stats;
          this.embeddedPercent = stats.total > 0 ? Math.round((stats.embedded / stats.total) * 100) : 0;
          this.coveragePercent = stats.total > 0 ? Math.round((stats.traced / stats.total) * 100) : 0;
          this.isLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load statistics.';
          this.isLoading = false;
        },
      });
  }
}
