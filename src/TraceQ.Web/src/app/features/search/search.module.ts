import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { SearchPageComponent } from './search-page.component';
import { SearchResultsComponent } from './search-results.component';

const routes: Routes = [
  { path: '', component: SearchPageComponent }
];

@NgModule({
  declarations: [
    SearchPageComponent,
    SearchResultsComponent,
  ],
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
  ]
})
export class SearchModule {}
