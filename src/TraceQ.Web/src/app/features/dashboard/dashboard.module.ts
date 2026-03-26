import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { GridsterModule } from 'angular-gridster2';
import { SharedModule } from '../../shared/shared.module';
import { DashboardPageComponent } from './dashboard-page.component';

const routes: Routes = [
  { path: '', component: DashboardPageComponent }
];

@NgModule({
  declarations: [
    DashboardPageComponent,
  ],
  imports: [
    SharedModule,
    GridsterModule,
    RouterModule.forChild(routes),
  ]
})
export class DashboardModule {}
