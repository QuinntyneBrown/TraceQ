import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type TqFabSize = 'lg' | 'md' | 'sm';

@Component({
  selector: 'tq-fab',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './fab.component.html',
  styleUrl: './fab.component.scss',
})
export class TqFabComponent {
  readonly icon = input<string>('add');
  readonly size = input<TqFabSize>('md');
  readonly disabled = input(false);
  readonly ariaLabel = input<string>('Action button');
  readonly clicked = output<MouseEvent>();
}
