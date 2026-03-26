import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'tq-validation-banner',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './validation-banner.component.html',
  styleUrl: './validation-banner.component.scss',
})
export class TqValidationBannerComponent {
  readonly title = input('Please fix the following errors:');
  readonly errors = input.required<string[]>();
  readonly dismissible = input(true);
  readonly dismissed = output<void>();
}
