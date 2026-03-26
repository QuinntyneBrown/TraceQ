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
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
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
