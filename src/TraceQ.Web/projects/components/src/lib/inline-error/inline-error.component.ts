import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'tq-inline-error',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './inline-error.component.html',
  styleUrl: './inline-error.component.scss',
})
export class TqInlineErrorComponent {
  readonly message = input.required<string>();
}
