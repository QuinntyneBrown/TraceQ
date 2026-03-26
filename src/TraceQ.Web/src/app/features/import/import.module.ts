import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { ImportPageComponent } from './import-page.component';
import { FileUploadComponent } from './file-upload.component';
import { ImportResultsComponent } from './import-results.component';
import { ImportHistoryComponent } from './import-history.component';

const routes: Routes = [
  { path: '', component: ImportPageComponent }
];

@NgModule({
  declarations: [
    ImportPageComponent,
    FileUploadComponent,
    ImportResultsComponent,
    ImportHistoryComponent,
  ],
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
  ]
})
export class ImportModule {}
