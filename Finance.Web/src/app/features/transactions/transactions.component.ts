import { Component, OnInit, AfterViewInit, ChangeDetectorRef, ViewChild, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientWrapperService } from '../../core/services/api-client-wrapper.service';
import { TransactionDto, AssignCategoryCommand, CategoryDto } from '../../core/api/api-client';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { TransactionDetailsDialogComponent } from './transaction-details-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { TransactionStore } from '../../stores/transaction.store';
import { AccountStore } from '../../stores/account.store';
import { CategoryStore } from '../../stores/category.store';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
  ],
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.scss'],
})
export class TransactionsComponent implements OnInit, AfterViewInit {
  fromDate?: string;
  toDate?: string;
  searchText?: string;
  selectedCategoryIdFilter?: string;
  minAmount?: number;
  maxAmount?: number;

  isLoading = true;
  error?: string;
  transactions: TransactionDto[] = [];

  totalBalance?: number;

  dataSource = new MatTableDataSource<TransactionDto>([]);
  displayedColumns = ['bookingDate', 'name', 'description', 'amount', 'transactionType', 'category'];
  allDisplayedColumns = [...this.displayedColumns, 'actions'];
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  defaultPageSize = 5;

  editingCategoryTransactionId: string | null = null;

  public filteredTransactions: TransactionDto[] = [];
  public pagedTransactions: TransactionDto[] = [];
  public pageIndex = 0;
  public pageSize = 5;

  private transactionStore = inject(TransactionStore);
  private accountStore = inject(AccountStore);
  private categoryStore = inject(CategoryStore);

  // effectCleanup verwijderd, effect direct aangeroepen
  constructor(
      private api: ApiClientWrapperService,
      private cdr: ChangeDetectorRef,
      private dialog: MatDialog
  ) {
    effect(() => {
      const categories = this.categoryStore.categories(); // <-- belangrijk
      // (accounts eventueel ook lezen als signal)
      // const accounts = this.accountStore.accounts();

      this.isLoading = this.transactionStore.isLoading();
      this.error = this.transactionStore.error() ?? undefined;

      const tx = this.transactionStore.transactions() ?? [];
      this.transactions = tx;

      this.filteredTransactions = this.applyAllFilters(this.transactions);

      const maxPage = Math.floor((this.filteredTransactions.length - 1) / this.pageSize);
      if (this.pageIndex > maxPage) this.pageIndex = 0;

      this.updatePagedTransactions();

      if (this.transactions.length > 0) {
        const last = this.transactions[this.transactions.length - 1];
        this.totalBalance = last.resultingBalance ?? undefined;
      } else {
        this.totalBalance = undefined;
      }

      // ✅ meestal genoeg (zeker met OnPush)
      this.cdr.markForCheck();
    });
  }

  get categories(): CategoryDto[] {
    return this.categoryStore.categories();
  }

  ngOnInit(): void {
    this.dataSource.filterPredicate = (data: TransactionDto, filter: string) => {
      const search = filter.trim().toLowerCase();
      const combined = [
        data.bookingDate,
        data.name,
        data.description,
        data.amount?.toString(),
        data.transactionType,
        data.categoryName,
      ]
          .map(x => (x || '').toString().toLowerCase())
          .join(' ');
      return combined.includes(search);
    };

    // ✅ expliciet laden (als je store "load once" doet is dit veilig)
    this.categoryStore.load();
    this.accountStore.load?.(); // alleen als je deze ook zo hebt opgezet
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      if (this.paginator) {
        this.paginator.pageSize = this.pageSize;
        this.paginator.pageIndex = this.pageIndex;
        this.paginator.page.subscribe((event) => {
          this.pageIndex = event.pageIndex;
          this.pageSize = event.pageSize;
          this.updatePagedTransactions();
        });
      }
    });
  }

  applyAllFilters(transactions: TransactionDto[]): TransactionDto[] {
    return transactions.filter(t => {
      if (this.searchText && this.searchText.trim()) {
        const search = this.searchText.trim().toLowerCase();
        const combined = [
          t.bookingDate,
          t.name,
          t.description,
          t.amount?.toString(),
          t.transactionType,
          t.categoryName
        ].map(x => (x || '').toString().toLowerCase()).join(' ');
        if (!combined.includes(search)) return false;
      }

      if (this.selectedCategoryIdFilter && t.categoryId !== this.selectedCategoryIdFilter) {
        return false;
      }

      if (this.minAmount !== undefined && this.minAmount !== null && (t.amount ?? 0) < this.minAmount) {
        return false;
      }
      if (this.maxAmount !== undefined && this.maxAmount !== null && (t.amount ?? 0) > this.maxAmount) {
        return false;
      }

      if (this.fromDate && t.bookingDate && t.bookingDate < this.fromDate) {
        return false;
      }
      if (this.toDate && t.bookingDate && t.bookingDate > this.toDate) {
        return false;
      }
      return true;
    });
  }

  updatePagedTransactions(): void {
    const start = this.pageIndex * this.pageSize;
    const end = start + this.pageSize;
    this.pagedTransactions = this.filteredTransactions.slice(start, end);
  }

  onApplyFilters(): void {
    this.filteredTransactions = this.applyAllFilters(this.transactions);
    this.pageIndex = 0;
    if (this.paginator) this.paginator.firstPage();
    this.updatePagedTransactions();
  }

  onCategoryClick(transaction: TransactionDto): void {
    this.editingCategoryTransactionId = transaction.transactionId ?? null;
  }

  onCategorySelect(transaction: TransactionDto, newCategoryId: string | null | undefined): void {
    if (!newCategoryId) {
      if (!transaction.transactionId) {
        this.error = 'Transactie-ID ontbreekt.';
        return;
      }
      this.api.unassignCategory(transaction.transactionId).subscribe({
        next: () => {
          transaction.categoryId = undefined;
          transaction.categoryName = undefined;
          setTimeout(() => {
            this.editingCategoryTransactionId = null;
            this.cdr.markForCheck();
          });
        },
        error: (err: unknown) => {
          console.error('Error while removing category', err);
          this.error = 'Kon categorie niet verwijderen.';
          setTimeout(() => {
            this.editingCategoryTransactionId = null;
            this.cdr.markForCheck();
          });
        },
      });
      return;
    }

    const command = new AssignCategoryCommand({
      transactionId: transaction.transactionId,
      categoryId: newCategoryId,
    });

    this.api.assignCategory(command).subscribe({
      next: () => {
        transaction.categoryId = newCategoryId;
        const cat = this.categories.find(c => c.categoryId === newCategoryId);
        transaction.categoryName = cat?.name ?? transaction.categoryName;

        setTimeout(() => {
          this.editingCategoryTransactionId = null;
          this.cdr.markForCheck();
        });
      },
      error: (err: unknown) => {
        console.error('Error while assigning category', err);
        this.error = 'Kon categorie niet toewijzen.';
        setTimeout(() => {
          this.editingCategoryTransactionId = null;
          this.cdr.markForCheck();
        });
      },
    });
  }

  onCategoryDropdownBlur(): void {
    setTimeout(() => {
      this.editingCategoryTransactionId = null;
      this.cdr.markForCheck();
    }, 200);
  }

  getCategoryName(categoryId: string | undefined): string {
    return this.categories.find(c => c.categoryId === categoryId)?.name ?? '(geen)';
  }

  openTransactionDetailsDialog(transaction: TransactionDto): void {
    this.dialog.open(TransactionDetailsDialogComponent, {
      data: transaction,
      width: '500px',
    });
  }

  clearFilters(): void {
    this.fromDate = undefined;
    this.toDate = undefined;
    this.searchText = undefined;
    this.selectedCategoryIdFilter = undefined;
    this.minAmount = undefined;
    this.maxAmount = undefined;

    this.pageIndex = 0;
    if (this.paginator) this.paginator.firstPage();

    this.filteredTransactions = this.applyAllFilters(this.transactions);
    this.updatePagedTransactions();
  }
}
