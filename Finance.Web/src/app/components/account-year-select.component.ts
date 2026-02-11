import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { AccountYearStore } from '../stores/account-year.store';

@Component({
  selector: 'app-account-year-select',
  standalone: true,
  imports: [CommonModule, MatFormFieldModule, MatSelectModule],
  template: `
    <div class="d-flex flex-wrap align-items-end gap-3" style="min-width:320px">
      <mat-form-field appearance="outline" class="account-select-field">
        <mat-label>Account</mat-label>
        <mat-select [value]="store.selectedAccountId()" (selectionChange)="onAccountChange($event)">
          @for (acc of store.normalAccounts(); track acc.accountId) {
            <mat-option [value]="acc.accountId">{{ acc.name || acc.accountIdentifier }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-form-field appearance="outline" class="year-select-field">
        <mat-label>Boekjaar</mat-label>
        <mat-select [value]="store.selectedBookYear()" (selectionChange)="onBookYearChange($event)">
          @for (year of store.bookYears(); track year) {
            <mat-option [value]="year">{{ year }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>
  `,
  styles: [
    `
      .account-select-field {
        min-width: 220px;
        flex: 1 1 220px;
      }
      .year-select-field {
        min-width: 140px;
        max-width: 180px;
        flex: 0 1 140px;
      }
      @media (max-width: 600px) {
        .account-select-field, .year-select-field {
          min-width: 120px;
          max-width: 100%;
          flex: 1 1 100%;
        }
      }
    `
  ]
})
export class AccountYearSelectComponent implements OnInit {
  store = inject(AccountYearStore);

  ngOnInit() {
    this.store.load(); // Zorgt dat accounts en boekjaren worden geladen
  }

  onAccountChange(event: any) {
    const value = event.value;
    if (value) this.store.setAccount(value);
  }

  onBookYearChange(event: any) {
    const value = event.value;
    if (value) this.store.setBookYear(Number(value));
  }
}
