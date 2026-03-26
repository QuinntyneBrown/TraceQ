import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { Distribution, SimilarityCluster, TraceabilityCoverage } from '../models/report.model';

export type DistributionField = 'type' | 'state' | 'priority' | 'module' | 'owner';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getDistribution(field: DistributionField): Observable<Distribution[]> {
    return this.http.get<Distribution[]>(`${this.baseUrl}/api/reports/distribution/${field}`);
  }

  getTraceability(): Observable<TraceabilityCoverage> {
    return this.http.get<TraceabilityCoverage>(`${this.baseUrl}/api/reports/traceability`);
  }

  getSimilarityClusters(threshold?: number): Observable<SimilarityCluster[]> {
    let httpParams = new HttpParams();
    if (threshold != null) httpParams = httpParams.set('threshold', threshold);
    return this.http.get<SimilarityCluster[]>(
      `${this.baseUrl}/api/reports/similarity-clusters`,
      { params: httpParams },
    );
  }
}
