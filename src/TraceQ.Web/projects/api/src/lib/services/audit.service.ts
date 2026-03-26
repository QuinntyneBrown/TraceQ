import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { AuditLogEntry } from '../models/audit.model';
import { PaginatedResult } from '../models/paginated-result.model';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  list(params?: {
    page?: number;
    pageSize?: number;
    eventType?: string;
  }): Observable<PaginatedResult<AuditLogEntry>> {
    let httpParams = new HttpParams();
    if (params?.page != null) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize != null) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params?.eventType) httpParams = httpParams.set('eventType', params.eventType);
    return this.http.get<PaginatedResult<AuditLogEntry>>(`${this.baseUrl}/api/audit`, {
      params: httpParams,
    });
  }
}
