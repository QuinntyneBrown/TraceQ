import { Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'tq-form-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatDialogModule],
  templateUrl: './form-dialog.component.html',
  styleUrl: './form-dialog.component.scss',
})
export class TqFormDialogComponent {
  readonly title = input.required<string>();
  readonly submitLabel = input('SAVE');
  readonly cancelLabel = input('CANCEL');
  readonly submitDisabled = input(false);
  readonly submitted = output<void>();
  readonly closed = output<void>();
}
