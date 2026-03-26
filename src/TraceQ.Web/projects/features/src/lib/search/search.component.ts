import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';

import { RequirementsService, SearchService } from 'api';
import type { Facets, SearchFilters, SearchResult } from 'api';
import { TqButtonComponent, TqEmptyStateComponent, TqToastService } from 'components';

@Component({
  selector: 'feat-search',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    TqButtonComponent,
    TqEmptyStateComponent,
  ],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
})
export class SearchComponent {
  private readonly searchService = inject(SearchService);
  private readonly requirementsService = inject(RequirementsService);
  private readonly toast = inject(TqToastService);

  protected query = '';
  protected filters: SearchFilters = {};
  protected readonly facets = signal<Facets | null>(null);
  protected readonly results = signal<SearchResult[]>([]);
  protected readonly isSearching = signal(false);
  protected readonly hasSearched = signal(false);
  protected readonly similarMode = signal<string | null>(null);

  constructor() {
    this.loadFacets();
  }

  search(): void {
    if (!this.query.trim() && !this.similarMode()) {
      return;
    }

    this.isSearching.set(true);
    this.similarMode.set(null);

    this.searchService.search({
      query: this.query.trim(),
      top: 20,
      filters: this.hasActiveFilters() ? this.filters : undefined,
    }).pipe(
      finalize(() => this.isSearching.set(false)),
    ).subscribe({
      next: (results) => {
        this.results.set(results);
        this.hasSearched.set(true);
      },
      error: () => {
        this.toast.error({ title: 'Search Failed', message: 'Could not perform search. Please try again.' });
      },
    });
  }

  findSimilar(requirementId: string, requirementNumber: string): void {
    this.isSearching.set(true);
    this.similarMode.set(requirementNumber);

    this.requirementsService.getSimilar(requirementId, 10).pipe(
      finalize(() => this.isSearching.set(false)),
    ).subscribe({
      next: (results) => {
        this.results.set(results);
        this.hasSearched.set(true);
      },
      error: () => {
        this.toast.error({ title: 'Error', message: 'Could not find similar requirements.' });
        this.similarMode.set(null);
      },
    });
  }

  clearSearch(): void {
    this.query = '';
    this.filters = {};
    this.results.set([]);
    this.hasSearched.set(false);
    this.similarMode.set(null);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.search();
    }
  }

  formatScore(score: number): string {
    return `${Math.round(score * 100)}%`;
  }

  hasActiveFilters(): boolean {
    return !!(this.filters.type || this.filters.state || this.filters.priority || this.filters.module || this.filters.owner);
  }

  private loadFacets(): void {
    this.requirementsService.getFacets().subscribe({
      next: (facets) => this.facets.set(facets),
      error: () => {
        this.toast.warning({ title: 'Warning', message: 'Could not load filter options.' });
      },
    });
  }
}
