import { Requirement } from './requirement.model';

export interface Distribution {
  label: string;
  count: number;
}

export interface TraceabilityCoverage {
  coveragePercentage: number;
  totalRequirements: number;
  tracedRequirements: number;
  untracedRequirements: Requirement[];
  traceLinkDistribution: Distribution[];
}

export interface ClusterMember {
  requirement: Requirement;
  pairwiseScores: Record<string, number>;
}

export interface SimilarityCluster {
  clusterId: number;
  members: ClusterMember[];
}
