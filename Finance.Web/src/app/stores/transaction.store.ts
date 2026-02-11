import { Injectable, computed, signal, effect, inject, NgZone } from '@angular/core';
import { ApiClientWrapperService } from '../core/services/api-client-wrapper.service';
import { AccountYearStore } from './account-year.store';
import { TransactionDto } from '../core/api/api-client';

interface CachedTransactions {
  data: TransactionDto[];
  timestamp: number; // ms since epoch
}

@Injectable({ providedIn: 'root' })
export class TransactionStore {
  // Structure: { [accountId]: { [year]: { data, timestamp } } }
  private cache: Record<string, Record<number, CachedTransactions>> = {};
  private readonly transactionsSignal = signal<TransactionDto[] | null>(null);
  private readonly isLoadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<string | null>(null);
  private ngZone = inject(NgZone);

  readonly isReady = computed(() =>
    !!this.accountYearStore.selectedAccountId() &&
    !!this.accountYearStore.selectedBookYear()
  );

  constructor(
    private api: ApiClientWrapperService,
    private accountYearStore: AccountYearStore
  ) {
    // React to account/year changes
    effect(() => {
      if (!this.isReady()) return;
      const accountId = this.accountYearStore.selectedAccountId();
      const year = this.accountYearStore.selectedBookYear();
      if (accountId && year) {
        this.loadTransactions(accountId, year);
      } else {
        this.transactionsSignal.set(null);
      }
    });
  }

  transactions = computed(() => this.transactionsSignal());
  isLoading = computed(() => this.isLoadingSignal());
  error = computed(() => this.errorSignal());

  private async loadTransactions(accountId: string, year: number) {
    this.ngZone.run(() => this.isLoadingSignal.set(true));
    this.ngZone.run(() => this.errorSignal.set(null));
    const now = Date.now();
    // Check cache
    const cached = this.cache[accountId]?.[year];
    if (cached && now - cached.timestamp < 24 * 60 * 60 * 1000) {
      this.ngZone.run(() => this.transactionsSignal.set(cached.data));
      this.ngZone.run(() => this.isLoadingSignal.set(false));
      return;
    }
    try {
      const tx = await this.api.getTransactions([accountId], year).toPromise();
      const txArr = tx ?? [];
      if (!this.cache[accountId]) this.cache[accountId] = {};
      this.cache[accountId][year] = { data: txArr, timestamp: now };
      this.ngZone.run(() => this.transactionsSignal.set(txArr));
    } catch (err: any) {
      this.ngZone.run(() => this.errorSignal.set(err?.message || 'Fout bij laden transacties'));
      this.ngZone.run(() => this.transactionsSignal.set([]));
    } finally {
      this.ngZone.run(() => this.isLoadingSignal.set(false));
    }
  }

  // Optional: force refresh (ignore cache)
  async refresh() {
    const accountId = this.accountYearStore.selectedAccountId();
    const year = this.accountYearStore.selectedBookYear();
    if (accountId && year) {
      // Remove from cache and reload
      if (this.cache[accountId]) delete this.cache[accountId][year];
      await this.loadTransactions(accountId, year);
    }
  }
}
