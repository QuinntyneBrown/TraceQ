export interface Requirement {
  id: string;
  requirementNumber: string;
  name: string;
  description?: string;
  type?: string;
  state?: string;
  priority?: string;
  owner?: string;
  createdDate?: string;
  modifiedDate?: string;
  module?: string;
  parentNumber?: string;
  tracedTo: string[];
  isEmbedded: boolean;
}
