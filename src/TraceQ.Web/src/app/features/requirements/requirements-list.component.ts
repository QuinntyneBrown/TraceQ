import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { RequirementsService } from './requirements.service';
import { RequirementDto } from '../../shared/models/requirement.model';
import { RequirementDetailDialogComponent } from './requirement-detail-dialog.component';

@Component({
  selector: 'app-requirements-list',
  templateUrl: './requirements-list.component.html',
  styleUrls: ['./requirements-list.component.scss']
})
export class RequirementsListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = ['requirementNumber', 'name', 'type', 'state', 'priority', 'module'];
  dataSource = new MatTableDataSource<RequirementDto>([]);
  totalCount = 0;
  pageSize = 20;
  pageIndex = 0;
  sortBy = '';
  sortDir = '';
  isLoading = false;

  constructor(
    private requirementsService: RequirementsService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadRequirements();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadRequirements();
  }

  onSortChange(sort: Sort): void {
    this.sortBy = sort.active;
    this.sortDir = sort.direction;
    this.pageIndex = 0;
    this.loadRequirements();
  }

  openDetail(requirement: RequirementDto): void {
    const dialogRef = this.dialog.open(RequirementDetailDialogComponent, {
      width: '700px',
      maxHeight: '90vh',
      data: { requirement }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'deleted') {
        this.loadRequirements();
      }
    });
  }

  private loadRequirements(): void {
    this.isLoading = true;
    this.requirementsService.getRequirements(
      this.pageIndex + 1,
      this.pageSize,
      this.sortBy || undefined,
      this.sortDir || undefined
    ).subscribe({
      next: (result) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: () => {
        this.dataSource.data = [];
        this.isLoading = false;
      }
    });
  }
}
