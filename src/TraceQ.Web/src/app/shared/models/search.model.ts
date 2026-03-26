import { RequirementDto } from './requirement.model';

export interface SearchRequestDto {
  query: string;
  top: number;
  filters: SearchFiltersDto | null;
}

export interface SearchFiltersDto {
  type: string | null;
  state: string | null;
  priority: string | null;
  module: string | null;
  owner: string | null;
}

export interface SearchResultDto {
  requirement: RequirementDto;
  similarityScore: number;
}

export interface FacetsDto {
  types: FacetValueDto[];
  states: FacetValueDto[];
  priorities: FacetValueDto[];
  modules: FacetValueDto[];
  owners: FacetValueDto[];
}

export interface FacetValueDto {
  value: string;
  count: number;
}
