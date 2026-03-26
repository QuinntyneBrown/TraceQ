import { Component, input, model } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';

export interface TqButtonGroupOption {
  value: string;
  label: string;
  icon?: string;
}

@Component({
  selector: 'tq-button-group',
  standalone: true,
  imports: [CommonModule, MatButtonToggleModule, MatIconModule],
  templateUrl: './button-group.component.html',
  styleUrl: './button-group.component.scss',
})
export class TqButtonGroupComponent {
  readonly options = input.required<TqButtonGroupOption[]>();
  readonly value = model<string>();
}
