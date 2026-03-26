import { Component, OnInit, OnDestroy } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { GridsterConfig, GridsterItem, DisplayGrid, GridType, CompactType } from 'angular-gridster2';
import { Subject, takeUntil } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { DashboardLayoutDto, DashboardWidgetConfig } from '../../shared/models/dashboard.model';
import { AddWidgetDialogComponent } from './add-widget-dialog.component';
import { SaveLayoutDialogComponent } from './save-layout-dialog.component';
import { configureChartDefaults } from './chart-theme';

interface DashboardWidget extends GridsterItem {
  widgetType: string;
  title: string;
}

const LOCAL_STORAGE_KEY = 'traceq_dashboard_layout';

@Component({
  selector: 'app-dashboard-page',
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.scss']
})
export class DashboardPageComponent implements OnInit, OnDestroy {
  gridOptions: GridsterConfig = {};
  widgets: DashboardWidget[] = [];
  savedLayouts: DashboardLayoutDto[] = [];

  private destroy$ = new Subject<void>();

  constructor(
    private dashboardService: DashboardService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
  ) {
    configureChartDefaults();
  }

  ngOnInit(): void {
    this.gridOptions = {
      gridType: GridType.Fit,
      compactType: CompactType.None,
      displayGrid: DisplayGrid.OnDragAndResize,
      pushItems: true,
      draggable: {
        enabled: true,
        ignoreContentClass: 'widget-content-area',
      },
      resizable: {
        enabled: true,
      },
      minCols: 12,
      maxCols: 12,
      minRows: 8,
      maxRows: 100,
      margin: 12,
      outerMargin: true,
      outerMarginTop: 12,
      outerMarginRight: 12,
      outerMarginBottom: 12,
      outerMarginLeft: 12,
      itemChangeCallback: () => this.persistToLocalStorage(),
    };

    this.loadFromLocalStorage() || this.resetLayout();
    this.loadSavedLayouts();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  openAddWidgetDialog(): void {
    const dialogRef = this.dialog.open(AddWidgetDialogComponent, {
      width: '480px',
    });

    dialogRef.afterClosed().subscribe((result: Partial<DashboardWidgetConfig> | null) => {
      if (result) {
        const widget: DashboardWidget = {
          x: 0,
          y: 0,
          cols: result.cols ?? 6,
          rows: result.rows ?? 4,
          widgetType: result.widgetType ?? 'stats',
          title: result.title ?? 'Widget',
        };
        this.widgets.push(widget);
        this.persistToLocalStorage();
      }
    });
  }

  openSaveLayoutDialog(): void {
    const dialogRef = this.dialog.open(SaveLayoutDialogComponent, {
      width: '400px',
    });

    dialogRef.afterClosed().subscribe((name: string | null) => {
      if (name) {
        this.saveCurrentLayout(name);
      }
    });
  }

  loadSavedLayout(layout: DashboardLayoutDto): void {
    const configs = this.dashboardService.parseLayoutWidgets(layout);
    this.widgets = configs.map(c => ({
      x: c.x,
      y: c.y,
      cols: c.cols,
      rows: c.rows,
      widgetType: c.widgetType,
      title: c.title,
    }));
    this.persistToLocalStorage();
    this.snackBar.open(`Layout "${layout.name}" loaded.`, 'OK', { duration: 2000 });
  }

  deleteLayout(layout: DashboardLayoutDto, event: Event): void {
    event.stopPropagation();
    this.dashboardService
      .deleteLayout(layout.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.savedLayouts = this.savedLayouts.filter(l => l.id !== layout.id);
          this.snackBar.open(`Layout "${layout.name}" deleted.`, 'OK', { duration: 2000 });
        },
        error: () => {
          this.snackBar.open('Failed to delete layout.', 'OK', { duration: 3000 });
        },
      });
  }

  removeWidget(index: number): void {
    this.widgets.splice(index, 1);
    this.persistToLocalStorage();
  }

  resetLayout(): void {
    this.widgets = [
      { x: 0, y: 0, cols: 12, rows: 2, widgetType: 'stats', title: 'Summary Statistics' },
      { x: 0, y: 2, cols: 6, rows: 4, widgetType: 'distribution:type', title: 'Type Distribution' },
      { x: 6, y: 2, cols: 6, rows: 4, widgetType: 'distribution:state', title: 'State Distribution' },
      { x: 0, y: 6, cols: 6, rows: 4, widgetType: 'distribution:priority', title: 'Priority Distribution' },
      { x: 6, y: 6, cols: 6, rows: 5, widgetType: 'traceability', title: 'Traceability Coverage' },
      { x: 0, y: 10, cols: 8, rows: 4, widgetType: 'similarity', title: 'Similarity Clusters' },
      { x: 8, y: 10, cols: 4, rows: 4, widgetType: 'recent-imports', title: 'Recent Imports' },
    ];
    this.persistToLocalStorage();
  }

  /** Extracts the distribution field from a 'distribution:field' widgetType */
  getDistributionField(widgetType: string): 'type' | 'state' | 'priority' | 'module' {
    const parts = widgetType.split(':');
    const field = parts.length > 1 ? parts[1] : 'type';
    if (['type', 'state', 'priority', 'module'].includes(field)) {
      return field as 'type' | 'state' | 'priority' | 'module';
    }
    return 'type';
  }

  /** Determines the base widget type for ngSwitch */
  getBaseWidgetType(widgetType: string): string {
    if (widgetType.startsWith('distribution:')) {
      return 'distribution';
    }
    return widgetType;
  }

  private saveCurrentLayout(name: string): void {
    const configs: DashboardWidgetConfig[] = this.widgets.map(w => ({
      x: w.x,
      y: w.y,
      cols: w.cols,
      rows: w.rows,
      widgetType: w.widgetType,
      title: w.title,
    }));

    const layout: DashboardLayoutDto = {
      id: '',
      name,
      layoutJson: this.dashboardService.serializeWidgets(configs),
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    this.dashboardService
      .saveLayout(layout)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (saved) => {
          this.savedLayouts.push(saved);
          this.snackBar.open(`Layout "${name}" saved.`, 'OK', { duration: 2000 });
        },
        error: () => {
          this.snackBar.open('Failed to save layout.', 'OK', { duration: 3000 });
        },
      });
  }

  private loadSavedLayouts(): void {
    this.dashboardService
      .getLayouts()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (layouts) => {
          this.savedLayouts = layouts;
        },
        error: () => {
          // Silently fail — saved layouts are optional
        },
      });
  }

  private persistToLocalStorage(): void {
    const configs: DashboardWidgetConfig[] = this.widgets.map(w => ({
      x: w.x,
      y: w.y,
      cols: w.cols,
      rows: w.rows,
      widgetType: w.widgetType,
      title: w.title,
    }));
    try {
      localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(configs));
    } catch {
      // localStorage may be unavailable in air-gapped environments
    }
  }

  private loadFromLocalStorage(): boolean {
    try {
      const stored = localStorage.getItem(LOCAL_STORAGE_KEY);
      if (stored) {
        const configs: DashboardWidgetConfig[] = JSON.parse(stored);
        if (Array.isArray(configs) && configs.length > 0) {
          this.widgets = configs.map(c => ({
            x: c.x,
            y: c.y,
            cols: c.cols,
            rows: c.rows,
            widgetType: c.widgetType,
            title: c.title,
          }));
          return true;
        }
      }
    } catch {
      // Ignore parse errors
    }
    return false;
  }
}
