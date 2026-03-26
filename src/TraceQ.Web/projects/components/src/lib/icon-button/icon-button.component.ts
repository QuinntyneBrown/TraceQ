import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type TqIconButtonVariant = 'outlined' | 'filled';
export type TqIconButtonSize = 'default' | 'small';

@Component({
  selector: 'tq-icon-button',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './icon-button.component.html',
  styleUrl: './icon-button.component.scss',
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
