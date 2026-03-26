import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

import { DistributionChartComponent } from './widgets/distribution-chart/distribution-chart.component';
import { TraceabilityComponent } from './widgets/traceability/traceability.component';
import { SimilarityClustersComponent } from './widgets/similarity-clusters/similarity-clusters.component';

@Component({
  selector: 'feat-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    DistributionChartComponent,
    TraceabilityComponent,
    SimilarityClustersComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {}
