import { Component } from '@angular/core';
import { ImportResultDto } from '../../shared/models/import.model';

@Component({
  selector: 'app-import-page',
  templateUrl: './import-page.component.html',
  styleUrls: ['./import-page.component.scss']
})
export class ImportPageComponent {
  importResult: ImportResultDto | null = null;

  onImportComplete(result: ImportResultDto): void {
    this.importResult = result;
  }
}
