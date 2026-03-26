export interface DashboardLayoutDto {
  id: string;
  name: string;
  layoutJson: string;
  createdAt: string;
  updatedAt: string;
}

export interface DashboardWidgetConfig {
  x: number;
  y: number;
  cols: number;
  rows: number;
  widgetType: string;
  title: string;
}

export interface DistributionDto {
  label: string;
  count: number;
}

export interface TraceabilityCoverageDto {
  coveragePercentage: number;
  totalRequirements: number;
  tracedRequirements: number;
  traceLinkDistribution: DistributionDto[];
}

export interface SimilarityClusterDto {
  clusterId: number;
  members: ClusterMemberDto[];
}

export interface ClusterMemberDto {
  requirement: import('./requirement.model').RequirementDto;
  pairwiseScores: { [key: string]: number };
}
