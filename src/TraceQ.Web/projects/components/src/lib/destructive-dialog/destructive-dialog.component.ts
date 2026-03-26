import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

export interface TqDestructiveDialogData {
  title: string;
  message: string;
  warningText?: string;
  confirmationWord?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  itemCount?: number;
}

@Component({
  selector: 'tq-destructive-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatDialogModule,
  ],
  templateUrl: './destructive-dialog.component.html',
  styleUrl: './destructive-dialog.component.scss',
})
export class TqDestructiveDialogComponent {
  readonly data = inject<TqDestructiveDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<TqDestructiveDialogComponent>);

  protected readonly isConfirmed = signal(false);

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.isConfirmed.set(value === (this.data.confirmationWord || 'DELETE'));
  }
}
