import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

import { RequirementsService } from 'api';
import type { Requirement, SearchResult } from 'api';
import { TqButtonComponent } from 'components';

export interface RequirementDetailData {
  requirement: Requirement;
  onNavigate: (requirementNumber: string) => void;
}

@Component({
  selector: 'feat-requirement-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatChipsModule,
    MatDividerModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    TqButtonComponent,
  ],
  templateUrl: './requirement-detail.component.html',
  styleUrl: './requirement-detail.component.scss',
})
export class RequirementDetailComponent {
  private readonly requirementsService = inject(RequirementsService);

  readonly data = inject<RequirementDetailData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<RequirementDetailComponent>);

  protected readonly similarRequirements = signal<SearchResult[]>([]);
  protected readonly loadingSimilar = signal(false);

  constructor() {
    this.loadSimilar();
  }

  get req(): Requirement {
    return this.data.requirement;
  }

  navigateToRequirement(reqNumber: string): void {
    this.dialogRef.close();
    this.data.onNavigate(reqNumber);
  }

  private loadSimilar(): void {
    if (!this.req.isEmbedded) return;
    this.loadingSimilar.set(true);
    this.requirementsService.getSimilar(this.req.id, 5).subscribe({
      next: (results) => {
        this.similarRequirements.set(results);
        this.loadingSimilar.set(false);
      },
      error: () => this.loadingSimilar.set(false),
    });
  }
}
