import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type TqIconButtonVariant = 'outlined' | 'filled';
export type TqIconButtonSize = 'default' | 'small';

@Component({
  selector: 'tq-icon-button',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  template: `
    <button
      mat-icon-button
      [class]="buttonClasses()"
      [disabled]="disabled()"
      [attr.aria-label]="ariaLabel()"
      (click)="clicked.emit($event)"
    >
      <mat-icon>{{ icon() }}</mat-icon>
    </button>
  `,
  styles: `
    :host {
      display: inline-block;
    }

    button {
      border-radius: 0;
      transition: opacity 0.2s;
    }

    button:hover:not(:disabled) {
      opacity: 0.85;
    }

    /* Outlined */
    .tq-ico-outlined {
      border: 1px solid #333333;
      background: transparent;

      mat-icon {
        color: #ffffff;
        font-size: 18px;
      }
    }

    .tq-ico-outlined.tq-ico-default {
      width: 44px;
      height: 44px;
    }

    .tq-ico-outlined.tq-ico-small {
      width: 36px;
      height: 36px;

      mat-icon {
        font-size: 16px;
        color: #777777;
      }
    }

    /* Filled (FAB-like in button grid) */
    .tq-ico-filled {
      background: #00897b;
      border-radius: 50%;

      mat-icon {
        color: #ffffff;
        font-size: 20px;
      }
    }

    .tq-ico-filled.tq-ico-default {
      width: 44px;
      height: 44px;
    }

    .tq-ico-filled.tq-ico-small {
      width: 36px;
      height: 36px;

      mat-icon {
        font-size: 16px;
      }
    }
  `,
})
export class TqIconButtonComponent {
  readonly icon = input.required<string>();
  readonly variant = input<TqIconButtonVariant>('outlined');
  readonly size = input<TqIconButtonSize>('default');
  readonly disabled = input(false);
  readonly ariaLabel = input<string>('');
  readonly clicked = output<MouseEvent>();

  buttonClasses(): string {
    return `tq-ico-${this.variant()} tq-ico-${this.size()}`;
  }
}
