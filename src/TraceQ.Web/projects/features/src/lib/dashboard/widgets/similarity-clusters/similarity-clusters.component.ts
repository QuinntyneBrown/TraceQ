import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatSliderModule } from '@angular/material/slider';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ReportsService } from 'api';
import type { SimilarityCluster } from 'api';
import { TqIconButtonComponent, TqEmptyStateComponent } from 'components';

@Component({
  selector: 'feat-similarity-clusters',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatSliderModule,
    MatTableModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    TqIconButtonComponent,
    TqEmptyStateComponent,
  ],
  templateUrl: './similarity-clusters.component.html',
  styleUrl: './similarity-clusters.component.scss',
})
export class SimilarityClustersComponent implements OnInit {
  private readonly reportsService = inject(ReportsService);

  protected threshold = 0.85;
  protected readonly clusters = signal<SimilarityCluster[]>([]);
  protected readonly isLoading = signal(false);

  ngOnInit(): void {
    this.loadData();
  }

  refresh(): void {
    this.loadData();
  }

  onThresholdChange(): void {
    this.loadData();
  }

  formatThreshold(value: number): string {
    return `${Math.round(value * 100)}%`;
  }

  private loadData(): void {
    this.isLoading.set(true);
    this.reportsService.getSimilarityClusters(this.threshold).subscribe({
      next: (clusters) => {
        this.clusters.set(clusters);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}
