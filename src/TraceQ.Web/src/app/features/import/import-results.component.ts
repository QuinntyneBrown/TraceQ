import { Component, Input } from '@angular/core';
import { ImportResultDto } from '../../shared/models/import.model';

@Component({
  selector: 'app-import-results',
  templateUrl: './import-results.component.html',
  styleUrls: ['./import-results.component.scss']
})
export class ImportResultsComponent {
  @Input() result!: ImportResultDto;

  get totalProcessed(): number {
    return this.result.insertedCount + this.result.updatedCount +
           this.result.skippedCount + this.result.errorCount;
  }
}
