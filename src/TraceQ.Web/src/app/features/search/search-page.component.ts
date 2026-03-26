import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { SearchService } from './search.service';
import { SearchResultDto, FacetsDto, FacetValueDto, SearchFiltersDto } from '../../shared/models/search.model';

@Component({
  selector: 'app-search-page',
  templateUrl: './search-page.component.html',
  styleUrls: ['./search-page.component.scss']
})
export class SearchPageComponent implements OnInit {
  searchForm: FormGroup;
  results: SearchResultDto[] = [];
  facets: FacetsDto | null = null;
  isSearching = false;
  hasSearched = false;

  constructor(
    private fb: FormBuilder,
    private searchService: SearchService
  ) {
    this.searchForm = this.fb.group({
      query: [''],
      type: [null],
      state: [null],
      priority: [null],
      module: [null],
      owner: [null]
    });
  }

  ngOnInit(): void {
    this.loadFacets();
  }

  onSearch(): void {
    const formValue = this.searchForm.value;
    if (!formValue.query?.trim()) return;

    this.isSearching = true;
    this.hasSearched = true;

    const filters: SearchFiltersDto = {
      type: formValue.type || null,
      state: formValue.state || null,
      priority: formValue.priority || null,
      module: formValue.module || null,
      owner: formValue.owner || null
    };

    this.searchService.search({
      query: formValue.query.trim(),
      top: 20,
      filters: this.hasActiveFilters(filters) ? filters : null
    }).subscribe({
      next: (results) => {
        this.results = results;
        this.isSearching = false;
      },
      error: () => {
        this.results = [];
        this.isSearching = false;
      }
    });
  }

  onFindSimilar(requirementId: string): void {
    this.isSearching = true;
    this.hasSearched = true;

    this.searchService.findSimilar(requirementId).subscribe({
      next: (results) => {
        this.results = results;
        this.isSearching = false;
      },
      error: () => {
        this.results = [];
        this.isSearching = false;
      }
    });
  }

  clearFilters(): void {
    this.searchForm.patchValue({
      type: null,
      state: null,
      priority: null,
      module: null,
      owner: null
    });
  }

  private loadFacets(): void {
    this.searchService.getFacets().subscribe({
      next: (facets) => {
        this.facets = facets;
      },
      error: () => {
        this.facets = null;
      }
    });
  }

  private hasActiveFilters(filters: SearchFiltersDto): boolean {
    return !!(filters.type || filters.state || filters.priority || filters.module || filters.owner);
  }
}
