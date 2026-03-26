import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TqToastComponent, TqToastData, TqToastType } from './toast.component';

export interface TqToastOptions {
  title: string;
  message: string;
  actionLabel?: string;
  actionCallback?: () => void;
  duration?: number;
  showProgress?: boolean;
  progressValue?: number;
}

@Injectable({ providedIn: 'root' })
export class TqToastService {
  private readonly snackBar = inject(MatSnackBar);

  success(options: TqToastOptions) {
    return this.show('success', options);
  }

  error(options: TqToastOptions) {
    return this.show('error', options);
  }

  warning(options: TqToastOptions) {
    return this.show('warning', options);
  }

  info(options: TqToastOptions) {
    return this.show('info', options);
  }

  private show(type: TqToastType, options: TqToastOptions) {
    const data: TqToastData = { type, ...options };

    return this.snackBar.openFromComponent(TqToastComponent, {
      data,
      duration: options.duration ?? 5000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      panelClass: ['tq-toast-panel'],
    });
  }
}
