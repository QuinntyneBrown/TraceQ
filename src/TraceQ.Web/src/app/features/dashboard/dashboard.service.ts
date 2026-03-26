import { Injectable } from '@angular/core';
import { Observable, forkJoin, map } from 'rxjs';
import { ApiService } from '../../shared/services/api.service';
import {
  DashboardLayoutDto,
  DashboardWidgetConfig,
  DistributionDto,
  TraceabilityCoverageDto,
  SimilarityClusterDto,
} from '../../shared/models/dashboard.model';
import { ImportBatchDto, PaginatedResult } from '../../shared/models/import.model';

export interface RequirementStats {
  total: number;
  embedded: number;
  traced: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  constructor(private api: ApiService) {}

  getDistribution(field: string): Observable<DistributionDto[]> {
    return this.api.get<DistributionDto[]>(`/reports/distribution/${field}`);
  }

  getTraceabilityCoverage(): Observable<TraceabilityCoverageDto> {
    return this.api.get<TraceabilityCoverageDto>('/reports/traceability');
  }

  getSimilarityClusters(threshold: number): Observable<SimilarityClusterDto[]> {
    return this.api.get<SimilarityClusterDto[]>('/reports/similarity-clusters', { threshold });
  }

  getLayouts(): Observable<DashboardLayoutDto[]> {
    return this.api.get<DashboardLayoutDto[]>('/dashboard/layouts');
  }

  saveLayout(layout: DashboardLayoutDto): Observable<DashboardLayoutDto> {
    return this.api.post<DashboardLayoutDto>('/dashboard/layouts', layout);
  }

  deleteLayout(id: string): Observable<void> {
    return this.api.delete<void>(`/dashboard/layouts/${id}`);
  }

  getRecentImports(): Observable<ImportBatchDto[]> {
    return this.api.get<PaginatedResult<ImportBatchDto>>('/import/history', {
      page: 1,
      pageSize: 5,
    }).pipe(map(result => result.items));
  }

  getRequirementStats(): Observable<RequirementStats> {
    return forkJoin({
      traceability: this.getTraceabilityCoverage(),
      embeddedDist: this.getDistribution('embedded'),
    }).pipe(
      map(({ traceability, embeddedDist }) => {
        const embeddedItem = embeddedDist.find(d => d.label.toLowerCase() === 'true');
        return {
          total: traceability.totalRequirements,
          embedded: embeddedItem ? embeddedItem.count : 0,
          traced: traceability.tracedRequirements,
        };
      })
    );
  }

  parseLayoutWidgets(layout: DashboardLayoutDto): DashboardWidgetConfig[] {
    try {
      return JSON.parse(layout.layoutJson) as DashboardWidgetConfig[];
    } catch {
      return [];
    }
  }

  serializeWidgets(widgets: DashboardWidgetConfig[]): string {
    return JSON.stringify(widgets);
  }
}
