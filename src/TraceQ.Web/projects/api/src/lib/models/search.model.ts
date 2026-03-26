import { Requirement } from './requirement.model';

export interface SearchFilters {
  type?: string;
  state?: string;
  priority?: string;
  module?: string;
  owner?: string;
}

export interface SearchRequest {
  query: string;
  top?: number;
  filters?: SearchFilters;
}

export interface SearchResult {
  requirement: Requirement;
  similarityScore: number;
}
