export interface FacetValue {
  value: string;
  count: number;
}

export interface Facets {
  types: FacetValue[];
  states: FacetValue[];
  priorities: FacetValue[];
  modules: FacetValue[];
  owners: FacetValue[];
}
