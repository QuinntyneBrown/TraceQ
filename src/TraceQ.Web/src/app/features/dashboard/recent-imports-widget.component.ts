import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { ImportBatchDto } from '../../shared/models/import.model';

@Component({
  selector: 'app-recent-imports-widget',
  templateUrl: './recent-imports-widget.component.html',
  styleUrls: ['./recent-imports-widget.component.scss'],
})
export class RecentImportsWidgetComponent implements OnInit, OnDestroy {
  imports: ImportBatchDto[] = [];
  displayedColumns: string[] = ['importedAt', 'fileName', 'insertedCount', 'updatedCount', 'errorCount'];
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

  getRelativeTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.floor(diffMs / 1000);
    const diffMin = Math.floor(diffSec / 60);
    const diffHr = Math.floor(diffMin / 60);
    const diffDays = Math.floor(diffHr / 24);

    if (diffSec < 60) {
      return 'just now';
    } else if (diffMin < 60) {
      return `${diffMin} minute${diffMin === 1 ? '' : 's'} ago`;
    } else if (diffHr < 24) {
      return `${diffHr} hour${diffHr === 1 ? '' : 's'} ago`;
    } else if (diffDays < 30) {
      return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
    } else {
      return date.toLocaleDateString();
    }
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.dashboardService
      .getRecentImports()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.imports = data;
          this.isLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load import history.';
          this.isLoading = false;
        },
      });
  }
}
