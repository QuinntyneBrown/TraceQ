import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'tq-detail-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatDialogModule],
  templateUrl: './detail-dialog.component.html',
  styleUrl: './detail-dialog.component.scss',
})
export class TqDetailDialogComponent {
  readonly title = input.required<string>();
  readonly tag = input<string>();
  readonly closeLabel = input('CLOSE');
  readonly secondaryActionLabel = input<string>();
  readonly hasCustomActions = input(false);
  readonly secondaryAction = output<void>();
  readonly closed = output<void>();
}
