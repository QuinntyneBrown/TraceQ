import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MAT_SNACK_BAR_DATA, MatSnackBarRef } from '@angular/material/snack-bar';

export type TqToastType = 'success' | 'error' | 'warning' | 'info';

export interface TqToastData {
  type: TqToastType;
  title: string;
  message: string;
  actionLabel?: string;
  actionCallback?: () => void;
  showProgress?: boolean;
  progressValue?: number;
}

@Component({
  selector: 'tq-toast',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss',
})
export class TqToastComponent {
  readonly data = inject<TqToastData>(MAT_SNACK_BAR_DATA);
  private readonly snackBarRef = inject(MatSnackBarRef);

  readonly iconMap: Record<TqToastType, string> = {
    success: 'check_circle',
    error: 'cancel',
    warning: 'warning',
    info: 'info',
  };

  onAction(): void {
    this.data.actionCallback?.();
    this.snackBarRef.dismissWithAction();
  }

  dismiss(): void {
    this.snackBarRef.dismiss();
  }
}
