import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type TqButtonVariant = 'primary' | 'secondary' | 'danger' | 'danger-outlined' | 'ghost';
export type TqButtonSize = 'lg' | 'md' | 'sm';

@Component({
  selector: 'tq-button',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  template: `
    <button
      mat-flat-button
      [class]="buttonClasses()"
      [disabled]="disabled()"
      (click)="clicked.emit($event)"
    >
      @if (icon()) {
        <mat-icon [fontSet]="iconFontSet()" [fontIcon]="icon()!">{{ useLigature() ? icon() : '' }}</mat-icon>
      }
      <span class="tq-btn-label"><ng-content /></span>
    </button>
  `,
  styles: `
    :host {
      display: inline-block;
    }

    button {
      font-family: 'IBM Plex Mono', monospace;
      font-weight: 600;
      letter-spacing: 1px;
      text-transform: uppercase;
      border-radius: 0;
      line-height: 1;
      transition: opacity 0.2s, background-color 0.2s;
    }

    button:hover:not(:disabled) {
      opacity: 0.85;
    }

    .tq-btn-label {
      white-space: nowrap;
    }

    /* Sizes */
    .tq-btn-lg {
      height: 44px;
      padding: 0 28px;
      font-size: 13px;

      mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
        margin-right: 8px;
      }
    }

    .tq-btn-md {
      height: 36px;
      padding: 0 24px;
      font-size: 12px;

      mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
        margin-right: 8px;
      }
    }

    .tq-btn-sm {
      height: 28px;
      padding: 0 16px;
      font-size: 11px;

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
        margin-right: 6px;
      }
    }

    /* Primary */
    .tq-btn-primary {
      background-color: #00897b;
      color: #ffffff;

      &:disabled {
        background-color: rgba(0, 137, 123, 0.25);
        color: rgba(255, 255, 255, 0.5);
      }
    }

    /* Secondary (Outlined) */
    .tq-btn-secondary {
      background-color: transparent;
      color: #00897b;
      border: 1px solid #00897b;

      &:disabled {
        color: rgba(85, 85, 85, 0.5);
        border-color: rgba(51, 51, 51, 0.5);
      }
    }

    /* Danger */
    .tq-btn-danger {
      background-color: #c62828;
      color: #ffffff;

      &:disabled {
        background-color: rgba(198, 40, 40, 0.25);
        color: rgba(255, 255, 255, 0.31);
      }
    }

    /* Danger Outlined */
    .tq-btn-danger-outlined {
      background-color: transparent;
      color: #c62828;
      border: 1px solid #c62828;
    }

    /* Ghost */
    .tq-btn-ghost {
      background-color: transparent;
      color: #00897b;

      mat-icon {
        color: #00897b;
      }
    }

    .tq-btn-ghost.tq-ghost-neutral {
      color: #ffffff;
    }

    .tq-btn-ghost.tq-ghost-muted {
      color: #777777;
    }
  `,
})
export class TqButtonComponent {
  readonly variant = input<TqButtonVariant>('primary');
  readonly size = input<TqButtonSize>('md');
  readonly icon = input<string>();
  readonly iconFontSet = input<string>('material-symbols-outlined');
  readonly disabled = input(false);
  readonly ghostTone = input<'default' | 'neutral' | 'muted'>('default');
  readonly clicked = output<MouseEvent>();

  useLigature(): boolean {
    return this.iconFontSet() === 'material-symbols-outlined';
  }

  buttonClasses(): string {
    const classes = [`tq-btn-${this.variant()}`, `tq-btn-${this.size()}`];
    if (this.variant() === 'ghost' && this.ghostTone() !== 'default') {
      classes.push(`tq-ghost-${this.ghostTone()}`);
    }
    return classes.join(' ');
  }
}
