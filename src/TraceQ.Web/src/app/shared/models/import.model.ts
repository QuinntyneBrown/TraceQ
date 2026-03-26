export interface ImportResultDto {
  batchId: string;
  fileName: string;
  insertedCount: number;
  updatedCount: number;
  errorCount: number;
  skippedCount: number;
}

export interface ImportBatchDto {
  id: string;
  fileName: string;
  importedAt: string;
  insertedCount: number;
  updatedCount: number;
  errorCount: number;
  skippedCount: number;
}

export interface ImportBatchDetailDto extends ImportBatchDto {
  records: ImportRecordDto[];
}

export interface ImportRecordDto {
  requirementNumber: string;
  status: string;
  errorMessage: string | null;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
