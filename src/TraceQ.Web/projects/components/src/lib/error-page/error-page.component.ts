import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type TqErrorPageType = '404' | '500';

@Component({
  selector: 'tq-error-page',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './error-page.component.html',
  styleUrl: './error-page.component.scss',
})
export class TqErrorPageComponent {
  readonly type = input<TqErrorPageType>('404');
  readonly title = input<string>();
  readonly message = input<string>();
  readonly actionLabel = input<string>();
  readonly actionClicked = output<void>();

  readonly iconMap: Record<TqErrorPageType, string> = {
    '404': 'find_in_page',
    '500': 'cloud_off',
  };

  readonly defaultTitles: Record<TqErrorPageType, string> = {
    '404': 'Page Not Found',
    '500': 'Something Went Wrong',
  };

  readonly defaultMessages: Record<TqErrorPageType, string> = {
    '404': "The page you're looking for doesn't exist or has been moved.",
    '500': 'We encountered an unexpected error. Please try again.',
  };

  readonly defaultActions: Record<TqErrorPageType, string> = {
    '404': 'GO TO DASHBOARD',
    '500': 'RETRY',
  };
}
