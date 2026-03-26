import { Component, OnInit } from '@angular/core';
import { GridsterConfig, GridsterItem, DisplayGrid, GridType, CompactType } from 'angular-gridster2';

interface DashboardWidget extends GridsterItem {
  widgetType: string;
  title: string;
}

@Component({
  selector: 'app-dashboard-page',
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.scss']
})
export class DashboardPageComponent implements OnInit {
  gridOptions: GridsterConfig = {};
  widgets: DashboardWidget[] = [];

  ngOnInit(): void {
    this.gridOptions = {
      gridType: GridType.Fit,
      compactType: CompactType.None,
      displayGrid: DisplayGrid.OnDragAndResize,
      pushItems: true,
      draggable: {
        enabled: true
      },
      resizable: {
        enabled: true
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
    };

    this.resetLayout();
  }

  addWidget(): void {
    const newWidget: DashboardWidget = {
      x: 0,
      y: 0,
      cols: 4,
      rows: 3,
      widgetType: 'placeholder',
      title: `Widget ${this.widgets.length + 1}`
    };
    this.widgets.push(newWidget);
  }

  removeWidget(index: number): void {
    this.widgets.splice(index, 1);
  }

  resetLayout(): void {
    this.widgets = [
      { x: 0, y: 0, cols: 3, rows: 2, widgetType: 'stat', title: 'Total Requirements' },
      { x: 3, y: 0, cols: 3, rows: 2, widgetType: 'stat', title: 'Traceability Coverage' },
      { x: 6, y: 0, cols: 3, rows: 2, widgetType: 'stat', title: 'Recent Imports' },
      { x: 9, y: 0, cols: 3, rows: 2, widgetType: 'stat', title: 'Embedding Status' },
      { x: 0, y: 2, cols: 6, rows: 4, widgetType: 'chart', title: 'Requirements by Type' },
      { x: 6, y: 2, cols: 6, rows: 4, widgetType: 'chart', title: 'Requirements by State' },
      { x: 0, y: 6, cols: 8, rows: 4, widgetType: 'table', title: 'Recent Activity' },
      { x: 8, y: 6, cols: 4, rows: 4, widgetType: 'chart', title: 'Priority Distribution' },
    ];
  }
}
