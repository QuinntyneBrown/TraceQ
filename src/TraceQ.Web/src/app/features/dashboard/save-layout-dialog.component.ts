import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-save-layout-dialog',
  templateUrl: './save-layout-dialog.component.html',
  styleUrls: ['./save-layout-dialog.component.scss'],
})
export class SaveLayoutDialogComponent {
  layoutName = '';

  constructor(private dialogRef: MatDialogRef<SaveLayoutDialogComponent>) {}

  onSave(): void {
    const name = this.layoutName.trim();
    if (name) {
      this.dialogRef.close(name);
    }
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }
}
