import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TransactionDto } from '../../core/api/api-client';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'app-transaction-details-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Transactiedetails</h2>
    <mat-dialog-content>
      <div><strong>Datum:</strong> {{ data.bookingDate }}</div>
      <div><strong>Naam:</strong> {{ data.name }}</div>
      <div><strong>Omschrijving:</strong> {{ data.description }}</div>
      <div><strong>Bedrag:</strong> {{ data.amount }} {{ data.currency }}</div>
      <div><strong>Type:</strong> {{ data.transactionType }}</div>
      <div><strong>Categorie:</strong> {{ data.categoryName || '(geen)' }}</div>
      <div *ngIf="data['accountType']"><strong>Account type:</strong> {{ data['accountType'] }}</div>
      <div *ngIf="data['accountId']"><strong>AccountId:</strong> {{ data['accountId'] }}</div>
      <div *ngIf="data['counterpartyAccountId']"><strong>CounterpartyAccountId:</strong> {{ data['counterpartyAccountId'] }}</div>
      <div *ngIf="data['linkedTransactionId']"><strong>Linked TransactionId:</strong> {{ data['linkedTransactionId'] }}</div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Sluiten</button>
    </mat-dialog-actions>
  `
})
export class TransactionDetailsDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<TransactionDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TransactionDto
  ) {}
}
