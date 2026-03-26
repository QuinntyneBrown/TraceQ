import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { Facets } from '../models/facet.model';
import { PaginatedResult } from '../models/paginated-result.model';
import { Requirement } from '../models/requirement.model';
import { SearchResult } from '../models/search.model';

@Injectable({ providedIn: 'root' })
export class RequirementsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getFacets(): Observable<Facets> {
    return this.http.get<Facets>(`${this.baseUrl}/api/requirements/facets`);
  }

  list(params?: {
    q?: string;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortDesc?: boolean;
  }): Observable<PaginatedResult<Requirement>> {
    let httpParams = new HttpParams();
    if (params?.q) httpParams = httpParams.set('q', params.q);
    if (params?.page != null) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize != null) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params?.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params?.sortDesc != null) httpParams = httpParams.set('sortDesc', params.sortDesc);
    return this.http.get<PaginatedResult<Requirement>>(`${this.baseUrl}/api/requirements`, {
      params: httpParams,
    });
  }

  get(id: string): Observable<Requirement> {
    return this.http.get<Requirement>(`${this.baseUrl}/api/requirements/${id}`);
  }

  getSimilar(id: string, top?: number): Observable<SearchResult[]> {
    let httpParams = new HttpParams();
    if (top != null) httpParams = httpParams.set('top', top);
    return this.http.get<SearchResult[]>(`${this.baseUrl}/api/requirements/${id}/similar`, {
      params: httpParams,
    });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/requirements/${id}`);
  }
}
