import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { ImportBatchDetail, ImportResult } from '../models/import.model';
import { PaginatedResult } from '../models/paginated-result.model';
import { ImportBatch } from '../models/import.model';

@Injectable({ providedIn: 'root' })
export class ImportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  uploadCsv(file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ImportResult>(`${this.baseUrl}/api/import/csv`, formData);
  }

  getHistory(params?: {
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<ImportBatch>> {
    let httpParams = new HttpParams();
    if (params?.page != null) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize != null) httpParams = httpParams.set('pageSize', params.pageSize);
    return this.http.get<PaginatedResult<ImportBatch>>(`${this.baseUrl}/api/import/history`, {
      params: httpParams,
    });
  }

  getBatch(batchId: string): Observable<ImportBatchDetail> {
    return this.http.get<ImportBatchDetail>(`${this.baseUrl}/api/import/${batchId}`);
  }
}
