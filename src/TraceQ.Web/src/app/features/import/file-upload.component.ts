import { Component, EventEmitter, Output } from '@angular/core';
import { HttpEventType } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../shared/services/api.service';
import { ImportResultDto } from '../../shared/models/import.model';

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.scss']
})
export class FileUploadComponent {
  @Output() importComplete = new EventEmitter<ImportResultDto>();

  selectedFile: File | null = null;
  isDragOver = false;
  isUploading = false;
  uploadProgress = 0;

  constructor(
    private apiService: ApiService,
    private snackBar: MatSnackBar
  ) {}

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      this.validateAndSetFile(file);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.validateAndSetFile(input.files[0]);
    }
  }

  upload(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadProgress = 0;

    this.apiService.postFileWithProgress('/import/csv', this.selectedFile)
      .subscribe({
        next: (event) => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            this.uploadProgress = Math.round((event.loaded / event.total) * 100);
          } else if (event.type === HttpEventType.Response) {
            const result = event.body as ImportResultDto;
            this.isUploading = false;
            this.uploadProgress = 100;
            this.importComplete.emit(result);
            this.snackBar.open('Import completed successfully', 'Dismiss', { duration: 5000 });
            this.selectedFile = null;
          }
        },
        error: (error) => {
          this.isUploading = false;
          this.uploadProgress = 0;
          const message = error?.error?.error || 'Upload failed. Please try again.';
          this.snackBar.open(message, 'Dismiss', { duration: 8000 });
        }
      });
  }

  removeFile(): void {
    this.selectedFile = null;
    this.uploadProgress = 0;
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  private validateAndSetFile(file: File): void {
    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.snackBar.open('Only .csv files are accepted', 'Dismiss', { duration: 5000 });
      return;
    }
    this.selectedFile = file;
  }
}
