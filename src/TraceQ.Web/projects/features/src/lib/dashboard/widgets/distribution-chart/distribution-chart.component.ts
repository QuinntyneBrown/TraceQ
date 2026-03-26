import { Component, inject, input, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ReportsService } from 'api';
import type { Distribution } from 'api';
import type { DistributionField } from 'api';
import { TqIconButtonComponent, TqEmptyStateComponent } from 'components';

@Component({
  selector: 'feat-distribution-chart',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    TqIconButtonComponent,
    TqEmptyStateComponent,
  ],
  templateUrl: './distribution-chart.component.html',
  styleUrl: './distribution-chart.component.scss',
})
export class DistributionChartComponent implements OnInit {
  private readonly reportsService = inject(ReportsService);

  readonly field = input.required<DistributionField>();
  readonly title = input.required<string>();

  protected readonly data = signal<Distribution[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly maxCount = signal(0);

  private readonly colors = [
    '#6366f1', '#8b5cf6', '#a855f7', '#d946ef', '#ec4899',
    '#f43f5e', '#ef4444', '#f97316', '#eab308', '#84cc16',
    '#22c55e', '#14b8a6', '#06b6d4', '#3b82f6', '#2563eb',
  ];

  ngOnInit(): void {
    this.loadData();
  }

  refresh(): void {
    this.loadData();
  }

  getColor(index: number): string {
    return this.colors[index % this.colors.length];
  }

  getBarWidth(count: number): string {
    const max = this.maxCount();
    return max > 0 ? `${(count / max) * 100}%` : '0%';
  }

  private loadData(): void {
    this.isLoading.set(true);
    this.reportsService.getDistribution(this.field()).subscribe({
      next: (data) => {
        this.data.set(data);
        this.maxCount.set(Math.max(...data.map((d) => d.count), 1));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}
