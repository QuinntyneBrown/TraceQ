import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DashboardWidgetConfig } from '../../shared/models/dashboard.model';

interface WidgetTypeOption {
  value: string;
  label: string;
  icon: string;
}

interface DistributionFieldOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-add-widget-dialog',
  templateUrl: './add-widget-dialog.component.html',
  styleUrls: ['./add-widget-dialog.component.scss'],
})
export class AddWidgetDialogComponent {
  widgetTypes: WidgetTypeOption[] = [
    { value: 'stats', label: 'Summary Statistics', icon: 'analytics' },
    { value: 'distribution', label: 'Distribution Chart', icon: 'bar_chart' },
    { value: 'traceability', label: 'Traceability Coverage', icon: 'link' },
    { value: 'similarity', label: 'Similarity Clusters', icon: 'hub' },
    { value: 'recent-imports', label: 'Recent Imports', icon: 'cloud_upload' },
  ];

  distributionFields: DistributionFieldOption[] = [
    { value: 'type', label: 'Type' },
    { value: 'state', label: 'State' },
    { value: 'priority', label: 'Priority' },
    { value: 'module', label: 'Module' },
  ];

  selectedType = 'stats';
  selectedField = 'type';
  customTitle = '';

  constructor(private dialogRef: MatDialogRef<AddWidgetDialogComponent>) {}

  get isDistribution(): boolean {
    return this.selectedType === 'distribution';
  }

  get resolvedTitle(): string {
    if (this.customTitle.trim()) {
      return this.customTitle.trim();
    }
    switch (this.selectedType) {
      case 'stats': return 'Summary Statistics';
      case 'distribution': {
        const field = this.distributionFields.find(f => f.value === this.selectedField);
        return `${field?.label ?? 'Distribution'} Distribution`;
      }
      case 'traceability': return 'Traceability Coverage';
      case 'similarity': return 'Similarity Clusters';
      case 'recent-imports': return 'Recent Imports';
      default: return 'Widget';
    }
  }

  onAdd(): void {
    const widgetType = this.isDistribution
      ? `distribution:${this.selectedField}`
      : this.selectedType;

    const config: Partial<DashboardWidgetConfig> = {
      widgetType,
      title: this.resolvedTitle,
      cols: this.getDefaultCols(),
      rows: this.getDefaultRows(),
    };

    this.dialogRef.close(config);
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  private getDefaultCols(): number {
    switch (this.selectedType) {
      case 'stats': return 12;
      case 'similarity': return 8;
      case 'recent-imports': return 4;
      default: return 6;
    }
  }

  private getDefaultRows(): number {
    switch (this.selectedType) {
      case 'stats': return 2;
      case 'traceability': return 5;
      case 'similarity': return 4;
      case 'recent-imports': return 4;
      default: return 4;
    }
  }
}
