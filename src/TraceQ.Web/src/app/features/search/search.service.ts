import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../shared/services/api.service';
import { SearchRequestDto, SearchResultDto, FacetsDto } from '../../shared/models/search.model';

@Injectable({
  providedIn: 'root'
})
export class SearchService {

  constructor(private apiService: ApiService) {}

  search(request: SearchRequestDto): Observable<SearchResultDto[]> {
    return this.apiService.post<SearchResultDto[]>('/search', request);
  }

  findSimilar(requirementId: string, top: number = 10): Observable<SearchResultDto[]> {
    return this.apiService.get<SearchResultDto[]>(`/requirements/${requirementId}/similar`, { top });
  }

  getFacets(): Observable<FacetsDto> {
    return this.apiService.get<FacetsDto>('/requirements/facets');
  }
}
