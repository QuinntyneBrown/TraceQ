import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { SimilarityClusterDto, ClusterMemberDto } from '../../shared/models/dashboard.model';

@Component({
  selector: 'app-similarity-clusters-widget',
  templateUrl: './similarity-clusters-widget.component.html',
  styleUrls: ['./similarity-clusters-widget.component.scss'],
})
export class SimilarityClustersWidgetComponent implements OnInit, OnDestroy {
  threshold = 0.85;
  clusters: SimilarityClusterDto[] = [];
  expandedClusterId: number | null = null;
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

  onThresholdChange(): void {
    this.loadData();
  }

  toggleCluster(clusterId: number): void {
    this.expandedClusterId = this.expandedClusterId === clusterId ? null : clusterId;
  }

  isExpanded(clusterId: number): boolean {
    return this.expandedClusterId === clusterId;
  }

  getPairwiseEntries(member: ClusterMemberDto): { key: string; score: number }[] {
    return Object.entries(member.pairwiseScores).map(([key, score]) => ({ key, score }));
  }

  formatThreshold(value: number): string {
    return value.toFixed(2);
  }

  refresh(): void {
    this.loadData();
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.dashboardService
      .getSimilarityClusters(this.threshold)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (clusters) => {
          this.clusters = clusters;
          this.expandedClusterId = null;
          this.isLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load similarity clusters.';
          this.isLoading = false;
        },
      });
  }
}
