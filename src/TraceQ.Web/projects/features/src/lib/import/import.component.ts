import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { finalize } from 'rxjs';

import { ImportService } from 'api';
import type { ImportBatch, ImportResult } from 'api';
import { TqButtonComponent, TqEmptyStateComponent, TqToastService } from 'components';

@Component({
  selector: 'feat-import',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    MatPaginatorModule,
    MatChipsModule,
    TqButtonComponent,
    TqEmptyStateComponent,
  ],
  templateUrl: './import.component.html',
  styleUrl: './import.component.scss',
})
export class ImportComponent {
  private readonly importService = inject(ImportService);
  private readonly toast = inject(TqToastService);

  protected readonly isDragging = signal(false);
  protected readonly isUploading = signal(false);
  protected readonly uploadResult = signal<ImportResult | null>(null);
  protected readonly uploadError = signal<string | null>(null);

  protected readonly historyItems = signal<ImportBatch[]>([]);
  protected readonly historyTotal = signal(0);
  protected readonly historyPage = signal(0);
  protected readonly historyPageSize = signal(10);

  protected readonly displayedColumns = ['fileName', 'importedAt', 'insertedCount', 'updatedCount', 'errorCount', 'skippedCount'];

  constructor() {
    this.loadHistory();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const files = event.dataTransfer?.files;
    if (files?.length) {
      this.uploadFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.uploadFile(input.files[0]);
      input.value = '';
    }
  }

  onPageChange(event: PageEvent): void {
    this.historyPage.set(event.pageIndex);
    this.historyPageSize.set(event.pageSize);
    this.loadHistory();
  }

  private uploadFile(file: File): void {
    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.toast.error({ title: 'Invalid File', message: 'Please select a .csv file.' });
      return;
    }

    if (file.size > 50 * 1024 * 1024) {
      this.toast.error({ title: 'File Too Large', message: 'File must be under 50 MB.' });
      return;
    }

    this.isUploading.set(true);
    this.uploadResult.set(null);
    this.uploadError.set(null);

    this.importService.uploadCsv(file).pipe(
      finalize(() => this.isUploading.set(false)),
    ).subscribe({
      next: (result) => {
        this.uploadResult.set(result);
        this.toast.success({
          title: 'Import Complete',
          message: `${result.insertedCount} inserted, ${result.updatedCount} updated, ${result.errorCount} errors.`,
        });
        this.loadHistory();
      },
      error: (err) => {
        const message = err.error?.message || err.message || 'Import failed.';
        this.uploadError.set(message);
        this.toast.error({ title: 'Import Failed', message });
      },
    });
  }

  private loadHistory(): void {
    this.importService.getHistory({
      page: this.historyPage() + 1,
      pageSize: this.historyPageSize(),
    }).subscribe({
      next: (result) => {
        this.historyItems.set(result.items);
        this.historyTotal.set(result.totalCount);
      },
      error: () => {
        this.toast.error({ title: 'Error', message: 'Failed to load import history.' });
      },
    });
  }
}
