import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { RequirementsListComponent } from './requirements-list.component';
import { RequirementDetailDialogComponent } from './requirement-detail-dialog.component';

const routes: Routes = [
  { path: '', component: RequirementsListComponent }
];

@NgModule({
  declarations: [
    RequirementsListComponent,
    RequirementDetailDialogComponent,
  ],
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
  ]
})
export class RequirementsModule {}
