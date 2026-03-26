export interface ImportResult {
  batchId: string;
  fileName: string;
  insertedCount: number;
  updatedCount: number;
  errorCount: number;
  skippedCount: number;
}

export interface ImportBatch {
  id: string;
  fileName: string;
  importedAt: string;
  insertedCount: number;
  updatedCount: number;
  errorCount: number;
  skippedCount: number;
}

export interface ImportBatchDetail extends ImportBatch {
  records: ImportRecord[];
}

export interface ImportRecord {
  requirementNumber: string;
  status: string;
  errorMessage?: string;
}
