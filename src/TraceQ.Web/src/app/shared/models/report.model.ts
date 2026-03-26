import { RequirementDto } from './requirement.model';
import { DistributionDto } from './dashboard.model';

export interface TraceabilityCoverageReport {
  coveragePercentage: number;
  totalRequirements: number;
  tracedRequirements: number;
  untracedRequirements: RequirementDto[];
  traceLinkDistribution: DistributionDto[];
}

export interface SimilarityClusterReport {
  clusterId: number;
  members: ClusterMemberReport[];
}

export interface ClusterMemberReport {
  requirement: RequirementDto;
  pairwiseScores: { [key: string]: number };
}
