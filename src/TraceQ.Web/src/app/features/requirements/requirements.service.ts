import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../shared/services/api.service';
import { RequirementDto } from '../../shared/models/requirement.model';
import { PaginatedResult } from '../../shared/models/import.model';

@Injectable({
  providedIn: 'root'
})
export class RequirementsService {

  constructor(private apiService: ApiService) {}

  getRequirements(page: number, pageSize: number, sortBy?: string, sortDir?: string): Observable<PaginatedResult<RequirementDto>> {
    const params: { [key: string]: string | number } = {
      page,
      pageSize
    };
    if (sortBy) {
      params['sortBy'] = sortBy;
    }
    if (sortDir) {
      params['sortDir'] = sortDir;
    }
    return this.apiService.get<PaginatedResult<RequirementDto>>('/requirements', params);
  }

  getRequirement(id: string): Observable<RequirementDto> {
    return this.apiService.get<RequirementDto>(`/requirements/${id}`);
  }

  deleteRequirement(id: string): Observable<void> {
    return this.apiService.delete<void>(`/requirements/${id}`);
  }
}
