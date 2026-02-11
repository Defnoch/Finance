export interface GetTransactionsQuery {
  fromDate?: string;
  toDate?: string;
  categoryId?: string;
  minAmount?: number;
  maxAmount?: number;
  searchText?: string;
}

