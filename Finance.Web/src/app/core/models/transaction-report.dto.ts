export interface TransactionDto {
  transactionId: string;
  bookingDate: string;
  amount: number;
  currency: string;
  description: string;
  categoryId?: string | null;
  categoryName?: string | null;
  resultingBalance?: number | null;
  accountId?: string | null;
  linkedTransactionId?: string | null;
}
