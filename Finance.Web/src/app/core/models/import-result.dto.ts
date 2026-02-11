export interface ImportResultDto {
  importBatchId: string;
  totalRecords: number;
  insertedRecords: number;
  duplicateRecords: number;
  errors: string[];
}
