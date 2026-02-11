import { Injectable, computed, signal, effect } from '@angular/core';
import { ApiClientWrapperService } from '../core/services/api-client-wrapper.service';
import { AccountDto } from '../core/api/api-client';
import { AccountStore } from './account.store';

@Injectable({ providedIn: 'root' })
export class AccountYearStore {
  // Signals
  readonly accounts = signal<AccountDto[]>([]);
  readonly normalAccounts = signal<AccountDto[]>([]);
  readonly selectedAccountId = signal<string | null>(null);
  readonly bookYears = signal<number[]>([]);
  readonly selectedBookYear = signal<number | null>(null);

  readonly loadingBookYears = signal(false);
  readonly error = signal<string | null>(null);

  private readonly _initialized = signal(false);
  private readonly _loadedBookYearsForAccountId = signal<string | null>(null);

  constructor(private api: ApiClientWrapperService, private accountStore: AccountStore) {
    // Sync accounts uit AccountStore (reactief)
    effect(() => {
      const all = this.accountStore.accounts();
      this.accounts.set(all);

      const normaal = all.filter(a => a.type === 'Normaal');
      this.normalAccounts.set(normaal);

      // default selectie (alleen als nog niet gekozen)
      if (normaal.length > 0 && !this.selectedAccountId()) {
        this.selectedAccountId.set(normaal[0].accountId ?? null);
      }
    });

    // Laad bookyears wanneer selectedAccountId verandert (en we init gedaan hebben)
    effect(() => {
      const initialized = this._initialized();
      const accountId = this.selectedAccountId();

      if (!initialized || !accountId) return;

      // als al geladen voor dit account, niks doen
      if (this._loadedBookYearsForAccountId() === accountId) return;

      void this.loadBookYears(accountId);
    });
  }

  // ✅ expliciet aanroepen vanuit component(s)
  load(): void {
    if (this._initialized()) return;
    this._initialized.set(true);

    // Zorg dat accounts geladen zijn
    this.accountStore.load();

    // Als er al accounts waren en selectie al gezet is, zal effect hierboven bookyears laden
  }

  private async loadBookYears(accountId: string): Promise<void> {
    if (this.loadingBookYears()) return;

    this.loadingBookYears.set(true);
    this.error.set(null);

    try {
      const years = await this.api.getAccountBookYears(accountId);

      if (Array.isArray(years)) {
        this.bookYears.set(years);

        if (years.length > 0 && !this.selectedBookYear()) {
          this.selectedBookYear.set(Math.max(...years));
        }

        this._loadedBookYearsForAccountId.set(accountId);
      } else {
        this.bookYears.set([]);
        this._loadedBookYearsForAccountId.set(null);
      }
    } catch {
      this.error.set('Fout bij ophalen boekjaren');
      this.bookYears.set([]);
      this._loadedBookYearsForAccountId.set(null);
    } finally {
      this.loadingBookYears.set(false);
    }
  }

  setAccount(accountId: string) {
    if (this.selectedAccountId() === accountId) return;

    this.selectedAccountId.set(accountId);
    this.selectedBookYear.set(null);

    // force reload for new account
    this._loadedBookYearsForAccountId.set(null);
  }

  setBookYear(year: number) {
    this.selectedBookYear.set(year);
  }

  ensureSelection() {
    if (!this.selectedAccountId() && this.normalAccounts().length > 0) {
      this.selectedAccountId.set(this.normalAccounts()[0].accountId ?? null);
    }
    if (!this.selectedBookYear() && this.bookYears().length > 0) {
      this.selectedBookYear.set(Math.max(...this.bookYears()));
    }
  }

  readonly selectedAccount = computed(() =>
      this.normalAccounts().find(a => a.accountId === this.selectedAccountId()) ?? null
  );

  readonly isReady = computed(() =>
      this.normalAccounts().length > 0 &&
      this.bookYears().length > 0 &&
      !!this.selectedAccountId() &&
      !!this.selectedBookYear()
  );
}
