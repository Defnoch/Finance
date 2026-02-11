import { Inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-tokens';
import { TransactionDto } from '../models/transaction-report.dto';
import { MonthlySummaryDto } from '../models/monthly-summary.dto';
import { CategorySummaryDto } from '../models/category-summary.dto';
import { GetTransactionsQuery } from '../models/transactions-query';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  getTransactions(query: GetTransactionsQuery = {}): Observable<TransactionDto[]> {
    let params = new HttpParams();
    if (query.fromDate)   params = params.set('from', query.fromDate);
    if (query.toDate)     params = params.set('to', query.toDate);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.minAmount != null) params = params.set('minAmount', query.minAmount);
    if (query.maxAmount != null) params = params.set('maxAmount', query.maxAmount);
    if (query.searchText) params = params.set('searchText', query.searchText);

    return this.http.get<TransactionDto[]>(`${this.baseUrl}/api/reports/transactions`, { params });
  }

  getMonthlySummary(fromDate?: string, toDate?: string): Observable<MonthlySummaryDto[]> {
    let params = new HttpParams();
    if (fromDate) params = params.set('from', fromDate);
    if (toDate)   params = params.set('to', toDate);

    return this.http.get<MonthlySummaryDto[]>(`${this.baseUrl}/api/reports/monthly-summary`, { params });
  }

  getCategorySummary(fromDate?: string, toDate?: string): Observable<CategorySummaryDto[]> {
    let params = new HttpParams();
    if (fromDate) params = params.set('from', fromDate);
    if (toDate)   params = params.set('to', toDate);

    return this.http.get<CategorySummaryDto[]>(`${this.baseUrl}/api/reports/category-summary`, { params });
  }
}

