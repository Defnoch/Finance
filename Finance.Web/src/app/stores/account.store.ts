import { Injectable, signal } from '@angular/core';
import { AccountDto } from '../core/api/api-client';
import { ApiClientWrapperService } from '../core/services/api-client-wrapper.service';
import { take } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AccountStore {

  private readonly _accounts = signal<AccountDto[]>([]);
  readonly accounts = this._accounts.asReadonly();

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly _loaded = signal(false);
  readonly loaded = this._loaded.asReadonly();

  constructor(private apiClientWrapper: ApiClientWrapperService) {}

  load(): void {
    if (this.loading() || this._loaded()) return;

    console.log('store load accounts');

    this.loading.set(true);
    this.error.set(null);

    this.apiClientWrapper
        .getAccounts()
        .pipe(take(1))
        .subscribe({
          next: (accounts: AccountDto[]) => {
            this._accounts.set(accounts);
            this._loaded.set(true);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('Fout bij ophalen accounts');
            this.loading.set(false);
          }
        });
  }

  reload(): void {
    this._loaded.set(false);
    this.load();
  }
}
