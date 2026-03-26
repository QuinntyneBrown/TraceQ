import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ReportsService } from 'api';
import type { TraceabilityCoverage } from 'api';
import { TqIconButtonComponent, TqEmptyStateComponent } from 'components';

@Component({
  selector: 'feat-traceability',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatProgressSpinnerModule,
    TqIconButtonComponent,
    TqEmptyStateComponent,
  ],
  templateUrl: './traceability.component.html',
  styleUrl: './traceability.component.scss',
})
export class TraceabilityComponent implements OnInit {
  private readonly reportsService = inject(ReportsService);

  protected readonly coverage = signal<TraceabilityCoverage | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly untracedColumns = ['requirementNumber', 'name', 'type', 'module'];

  ngOnInit(): void {
    this.loadData();
  }

  refresh(): void {
    this.loadData();
  }

  getMaxDistribution(): number {
    const dist = this.coverage()?.traceLinkDistribution;
    if (!dist?.length) return 1;
    return Math.max(...dist.map((d) => d.count), 1);
  }

  private loadData(): void {
    this.isLoading.set(true);
    this.reportsService.getTraceability().subscribe({
      next: (data) => {
        this.coverage.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}
