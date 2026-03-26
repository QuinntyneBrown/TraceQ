import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';

import { RequirementsService } from 'api';
import type { Requirement } from 'api';
import {
  TqButtonComponent,
  TqIconButtonComponent,
  TqEmptyStateComponent,
  TqDestructiveDialogComponent,
  TqToastService,
} from 'components';
import type { TqDestructiveDialogData } from 'components';
import { RequirementDetailComponent } from './requirement-detail.component';
import type { RequirementDetailData } from './requirement-detail.component';

@Component({
  selector: 'feat-requirements',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    TqButtonComponent,
    TqIconButtonComponent,
    TqEmptyStateComponent,
  ],
  templateUrl: './requirements.component.html',
  styleUrl: './requirements.component.scss',
})
export class RequirementsComponent {
  private readonly requirementsService = inject(RequirementsService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(TqToastService);

  protected readonly items = signal<Requirement[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly isLoading = signal(false);
  protected readonly page = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly sortBy = signal<string | undefined>(undefined);
  protected readonly sortDesc = signal(false);
  protected keyword = '';

  protected readonly displayedColumns = [
    'requirementNumber',
    'name',
    'type',
    'state',
    'priority',
    'module',
    'actions',
  ];

  constructor() {
    this.loadRequirements();
  }

  search(): void {
    this.page.set(0);
    this.loadRequirements();
  }

  clearSearch(): void {
    this.keyword = '';
    this.page.set(0);
    this.loadRequirements();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.search();
    }
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadRequirements();
  }

  onSortChange(sort: Sort): void {
    this.sortBy.set(sort.active || undefined);
    this.sortDesc.set(sort.direction === 'desc');
    this.page.set(0);
    this.loadRequirements();
  }

  openDetail(requirement: Requirement): void {
    const data: RequirementDetailData = {
      requirement,
      onNavigate: (reqNumber) => this.navigateToRequirement(reqNumber),
    };

    this.dialog.open(RequirementDetailComponent, {
      data,
      width: '680px',
      maxHeight: '85vh',
    });
  }

  confirmDelete(event: MouseEvent, requirement: Requirement): void {
    event.stopPropagation();

    const data: TqDestructiveDialogData = {
      title: 'Delete Requirement',
      message: `This will permanently delete requirement ${requirement.requirementNumber} and its vector embedding.`,
      warningText: 'This action cannot be undone.',
      confirmationWord: 'DELETE',
      confirmLabel: 'DELETE',
    };

    const dialogRef = this.dialog.open(TqDestructiveDialogComponent, { data, width: '440px' });
    dialogRef.afterClosed().subscribe((confirmed) => {
      if (confirmed) {
        this.deleteRequirement(requirement);
      }
    });
  }

  private deleteRequirement(requirement: Requirement): void {
    this.requirementsService.delete(requirement.id).subscribe({
      next: () => {
        this.toast.success({
          title: 'Deleted',
          message: `Requirement ${requirement.requirementNumber} has been deleted.`,
        });
        this.loadRequirements();
      },
      error: () => {
        this.toast.error({ title: 'Error', message: 'Failed to delete requirement.' });
      },
    });
  }

  private navigateToRequirement(requirementNumber: string): void {
    this.keyword = requirementNumber;
    this.page.set(0);
    this.loadRequirements();
  }

  private loadRequirements(): void {
    this.isLoading.set(true);

    this.requirementsService.list({
      q: this.keyword || undefined,
      page: this.page() + 1,
      pageSize: this.pageSize(),
      sortBy: this.sortBy(),
      sortDesc: this.sortDesc() || undefined,
    }).pipe(
      finalize(() => this.isLoading.set(false)),
    ).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
      },
      error: () => {
        this.toast.error({ title: 'Error', message: 'Failed to load requirements.' });
      },
    });
  }
}
