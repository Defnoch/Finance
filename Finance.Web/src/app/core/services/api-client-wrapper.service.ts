import { Inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { API_BASE_URL } from '../api/api-tokens';
import {
  ApiClient,
  ImportResultDto,
  TransactionDto,
  MonthlySummaryDto,
  CategorySummaryDto,
  AssignCategoryCommand,
  FileParameter,
  AccountDto,
  CategoryDto
} from '../api/api-client';

@Injectable({ providedIn: 'root' })
export class ApiClientWrapperService {
  private readonly client: ApiClient;

  constructor(http: HttpClient, @Inject(API_BASE_URL) baseUrl: string) {
    this.client = new ApiClient(http, baseUrl);
  }

  // Import
  uploadAsnCsv(file: File, overrideExisting: boolean = false): Observable<ImportResultDto> {
    const fp: FileParameter = { data: file, fileName: file.name };
    return this.client.asn(overrideExisting, fp);
  }

  uploadAsnSpaar(file: File, overrideExisting: boolean = false): Observable<ImportResultDto> {
    const fp: FileParameter = { data: file, fileName: file.name };
    return this.client.asnSpaar(overrideExisting, fp);
  }

  // Transactions / reports
  getTransactions(
    accountIds: string[],
    year?: number,
    from?: string,
    to?: string,
    categoryId?: string,
    minAmount?: number,
    maxAmount?: number,
    searchText?: string,
    name?: string
  ): Observable<TransactionDto[]> {
    return this.client.transactions(accountIds, year, from, to, categoryId, minAmount, maxAmount, searchText, name);
  }

  getMonthlySummary(from?: string, to?: string): Observable<MonthlySummaryDto[]> {
    return this.client.monthlySummary(from, to);
  }

  getCategorySummary(from?: string, to?: string): Observable<CategorySummaryDto[]> {
    return this.client.categorySummary(from, to);
  }

  assignCategory(command: AssignCategoryCommand): Observable<void> {
    return this.client.assign(command);
  }

  unassignCategory(transactionId: string): Observable<void> {
    return this.client.unassign(transactionId);
  }

  // Categorization Rules
  getRules(): Observable<any[]> {
    return this.client.rulesAll();
  }

  createRule(rule: any): Observable<any> {
    return this.client.rulesPOST(rule);
  }

  updateRule(rule: any): Observable<void> {
    return this.client.rulesPUT(rule.id, rule);
  }

  deleteRule(id: string): Observable<void> {
    return this.client.rulesDELETE(id);
  }

  /**
   * Haal transacties op, gefilterd op naam.
   */
  getTransactionsByName(name: string) {
    // Vul alle verplichte argumenten, accountIds en name zijn verplicht
    return this.client.transactions([], undefined, undefined, undefined, undefined, undefined, undefined, undefined, name);
  }

  getAccounts(): Observable<AccountDto[]> {
    return this.client.accounts();
  }

  async getAccountBookYears(accountId: string) {
    return await firstValueFrom(this.client.accountBookyears(accountId));
  }

  getCategories(): Observable<CategoryDto[]> {
    return this.client.categoriesAll();
  }
}
