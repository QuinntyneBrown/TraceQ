import { Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { ApiService } from '../../shared/services/api.service';
import { ImportBatchDto, PaginatedResult } from '../../shared/models/import.model';

@Component({
  selector: 'app-import-history',
  templateUrl: './import-history.component.html',
  styleUrls: ['./import-history.component.scss']
})
export class ImportHistoryComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  displayedColumns: string[] = ['importedAt', 'fileName', 'insertedCount', 'updatedCount', 'errorCount'];
  dataSource = new MatTableDataSource<ImportBatchDto>([]);
  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;
  isLoading = false;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadHistory();
  }

  private loadHistory(): void {
    this.isLoading = true;
    this.apiService.get<PaginatedResult<ImportBatchDto>>('/import/history', {
      page: this.pageIndex + 1,
      pageSize: this.pageSize
    }).subscribe({
      next: (result) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}
