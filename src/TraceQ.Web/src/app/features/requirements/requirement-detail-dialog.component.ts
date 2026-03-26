import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { RequirementsService } from './requirements.service';
import { RequirementDto } from '../../shared/models/requirement.model';

export interface RequirementDetailData {
  requirement: RequirementDto;
}

@Component({
  selector: 'app-requirement-detail-dialog',
  templateUrl: './requirement-detail-dialog.component.html',
  styleUrls: ['./requirement-detail-dialog.component.scss']
})
export class RequirementDetailDialogComponent {
  requirement: RequirementDto;
  isDeleting = false;

  constructor(
    private dialogRef: MatDialogRef<RequirementDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) private data: RequirementDetailData,
    private requirementsService: RequirementsService,
    private snackBar: MatSnackBar,
    private router: Router,
    private dialog: MatDialog
  ) {
    this.requirement = data.requirement;
  }

  onFindSimilar(): void {
    this.dialogRef.close();
    this.router.navigate(['/search'], {
      queryParams: { similarTo: this.requirement.id }
    });
  }

  onDelete(): void {
    if (this.isDeleting) return;

    const confirmed = confirm(
      `Are you sure you want to delete requirement "${this.requirement.requirementNumber}"? This action cannot be undone.`
    );

    if (!confirmed) return;

    this.isDeleting = true;
    this.requirementsService.deleteRequirement(this.requirement.id).subscribe({
      next: () => {
        this.snackBar.open('Requirement deleted successfully', 'Dismiss', { duration: 5000 });
        this.dialogRef.close('deleted');
      },
      error: () => {
        this.snackBar.open('Failed to delete requirement', 'Dismiss', { duration: 5000 });
        this.isDeleting = false;
      }
    });
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
