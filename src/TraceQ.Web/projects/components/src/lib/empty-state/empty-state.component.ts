import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export type TqEmptyStateType = 'no-data' | 'no-results';

@Component({
  selector: 'tq-empty-state',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
})
export class TqEmptyStateComponent {
  readonly type = input<TqEmptyStateType>('no-data');
  readonly icon = input<string>();
  readonly title = input<string>();
  readonly message = input<string>();
  readonly actionLabel = input<string>();
  readonly actionIcon = input<string>();
  readonly actionClicked = output<void>();

  readonly defaultIcons: Record<TqEmptyStateType, string> = {
    'no-data': 'inbox',
    'no-results': 'search_off',
  };

  readonly defaultTitles: Record<TqEmptyStateType, string> = {
    'no-data': 'No Requirements Yet',
    'no-results': 'No Results Found',
  };

  readonly defaultMessages: Record<TqEmptyStateType, string> = {
    'no-data': 'Import a CSV export from Windchill PLM to get started.',
    'no-results': 'No requirements matched your query. Try different keywords or adjust filters.',
  };

  readonly defaultActions: Record<TqEmptyStateType, string> = {
    'no-data': 'IMPORT CSV',
    'no-results': 'CLEAR SEARCH',
  };
}
