import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { SearchRequest, SearchResult } from '../models/search.model';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  search(request: SearchRequest): Observable<SearchResult[]> {
    return this.http.post<SearchResult[]>(`${this.baseUrl}/api/search`, request);
  }
}
