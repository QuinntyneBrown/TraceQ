import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { GridsterModule } from 'angular-gridster2';
import { NgChartsModule } from 'ng2-charts';
import { MatSliderModule } from '@angular/material/slider';
import { SharedModule } from '../../shared/shared.module';
import { DashboardPageComponent } from './dashboard-page.component';
import { StatsWidgetComponent } from './stats-widget.component';
import { DistributionChartWidgetComponent } from './distribution-chart-widget.component';
import { TraceabilityWidgetComponent } from './traceability-widget.component';
import { SimilarityClustersWidgetComponent } from './similarity-clusters-widget.component';
import { RecentImportsWidgetComponent } from './recent-imports-widget.component';
import { AddWidgetDialogComponent } from './add-widget-dialog.component';
import { SaveLayoutDialogComponent } from './save-layout-dialog.component';

const routes: Routes = [
  { path: '', component: DashboardPageComponent }
];

@NgModule({
  declarations: [
    DashboardPageComponent,
    StatsWidgetComponent,
    DistributionChartWidgetComponent,
    TraceabilityWidgetComponent,
    SimilarityClustersWidgetComponent,
    RecentImportsWidgetComponent,
    AddWidgetDialogComponent,
    SaveLayoutDialogComponent,
  ],
  imports: [
    SharedModule,
    GridsterModule,
    NgChartsModule,
    MatSliderModule,
    RouterModule.forChild(routes),
  ]
})
export class DashboardModule {}
