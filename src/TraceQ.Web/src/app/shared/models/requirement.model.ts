export interface RequirementDto {
  id: string;
  requirementNumber: string;
  name: string;
  description: string | null;
  type: string | null;
  state: string | null;
  priority: string | null;
  owner: string | null;
  createdDate: string | null;
  modifiedDate: string | null;
  module: string | null;
  parentNumber: string | null;
  tracedTo: string[];
  isEmbedded: boolean;
}
