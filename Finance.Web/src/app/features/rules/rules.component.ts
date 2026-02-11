import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectorRef } from '@angular/core';
import { CategoriesService } from '../../core/services/categories.service';
import { ApiClientWrapperService } from '../../core/services/api-client-wrapper.service';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { DetailsDialogComponent } from './details-dialog.component';
import { CategoryDialogComponent } from './category-dialog.component';
import { RuleDialogComponent } from './rule-dialog.component';
import { inject } from '@angular/core';
import { AccountYearStore } from '../../stores/account-year.store';
import {CategoryDto} from "../../core/api/api-client";
import { CategoryStore } from '../../stores/category.store';

interface RuleCondition {
  field: string;
  operator: string;
  value: string;
}

interface CategorizationRule {
  id?: string;
  name: string;
  priority: number;
  isEnabled: boolean;
  categoryId?: string;
  conditions: RuleCondition[];
}

export type UniqueCombo = { description: string; transactionType: string; name?: string };

@Component({
  selector: 'app-rules',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatDialogModule, MatTabsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule, MatPaginatorModule, MatIconModule
  ],
  templateUrl: './rules.component.html',
  styleUrls: ['./rules.component.scss']
})
export class RulesComponent implements OnInit {
  rules: CategorizationRule[] = [];
  rulesPaged: CategorizationRule[] = [];
  rulesPageSize = 5;
  rulesPageIndex = 0;
  rulesTotal = 0;

  isNew = false;

  uniqueCombos: UniqueCombo[] = [];
  isLoadingCombos = false;

  pageSize = 5;
  pageIndex = 0;
  pagedCombos: UniqueCombo[] = [];
  totalCombos = 0;

  categories: CategoryDto[] = [];
  categoriesPaged: CategoryDto[] = [];
  categoriesPageSize = 5;
  categoriesPageIndex = 0;
  categoriesTotal = 0;

  editingCategory: CategoryDto | null = null;
  isNewCategory = false;

  detailsTransactions: any[] = [];
  detailsName: string = '';

  tabIndex = 0;

  filterType: string = '';
  filteredCombos: UniqueCombo[] = [];

  uniqueTypes: string[] = [];

  isSavingCategory = false;
  categoryError: string | null = null;

  ruleError: string | null = null;

  private accountYearStore = inject(AccountYearStore);
  private categoryStore = inject(CategoryStore);

  constructor(
      private categoriesService: CategoriesService,
      private apiClientWrapper: ApiClientWrapperService,
      private cdr: ChangeDetectorRef,
      private dialog: MatDialog
  ) {
  }

  ngOnInit(): void {
    this.tabIndex = 0;
    this.pageSize = 5;
    this.loadUniqueCombos();
    this.loadCategories();
    this.loadRules();
  }

  loadRules() {
    this.apiClientWrapper.getRules().subscribe(rules => {
      this.rules = (rules || []).sort((a, b) => (a.name || '').localeCompare(b.name || ''));
      this.rulesTotal = this.rules.length;
      this.setRulesPaged();
      this.cdr.detectChanges();
    });
  }

  setRulesPaged() {
    const start = this.rulesPageIndex * this.rulesPageSize;
    this.rulesPaged = this.rules.slice(start, start + this.rulesPageSize);
  }

  onRulesPage(event: any) {
    this.rulesPageIndex = event.pageIndex;
    this.rulesPageSize = event.pageSize || 5;
    this.setRulesPaged();
    this.cdr.detectChanges();
  }

  loadUniqueCombos() {
    this.isLoadingCombos = true;
    // Haal accountId en boekjaar uit de signal store
    const accountId = this.accountYearStore.selectedAccountId();
    const year = this.accountYearStore.selectedBookYear() ?? undefined;
    if (!accountId) {
      this.isLoadingCombos = false;
      this.uniqueCombos = [];
      this.filteredCombos = [];
      this.pagedCombos = [];
      this.totalCombos = 0;
      this.cdr.detectChanges();
      return;
    }
    this.apiClientWrapper.getTransactions([accountId], year)
        .subscribe({
          next: (result: any[]) => {
            const seen = new Set<string>();
            let combos = (result || [])
                .map((t: any) => ({
                  description: t.description || '',
                  transactionType: t.transactionType || '',
                  name: t.name || ''
                }))
                .filter((c: any) => {
                  if (c.transactionType === 'Online Banking' || c.transactionType === 'iDEAL') return false;
                  const key = (c.name || '').trim().toLowerCase();
                  if (!key) return false;
                  if (seen.has(key)) return false;
                  seen.add(key);
                  return true;
                });
            this.uniqueCombos = combos;
            this.filteredCombos = combos;
            this.totalCombos = combos.length;
            this.setPagedCombos();
            this.uniqueTypes = Array.from(new Set(combos.map(c => c.transactionType))).sort();
            this.isLoadingCombos = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.isLoadingCombos = false;
            this.cdr.detectChanges();
          }
        });
  }

  applyFilter() {
    if (this.filterType) {
      this.filteredCombos = this.uniqueCombos.filter(c => c.transactionType === this.filterType);
    } else {
      this.filteredCombos = [...this.uniqueCombos];
    }
    this.uniqueTypes = Array.from(new Set(this.uniqueCombos.map(c => c.transactionType))).sort();
    this.pageIndex = 0;
    this.pageSize = 5; // Forceer altijd 5 bij filteren
    this.totalCombos = this.filteredCombos.length;
    this.setPagedCombos();
  }

  setPagedCombos() {
    const start = this.pageIndex * this.pageSize;
    this.pagedCombos = this.filteredCombos.slice(start, start + this.pageSize);
  }

  deleteCategory(cat: CategoryDto) {
    this.categoriesService.deleteCategory(cat.categoryId ?? '').subscribe({
      next: () => {
        this.loadCategories();
        this.categoryStore.reload(); // <-- store opnieuw laden
        this.cdr.detectChanges();
      },
      error: () => {
        this.categoryError = 'Verwijderen mislukt.';
        this.cdr.detectChanges();
      }
    });
  }

  deleteRule(rule: CategorizationRule) {
    if (rule.id) {
      this.apiClientWrapper.deleteRule(rule.id).subscribe({
        next: () => {
          this.rules = this.rules.filter(r => r.id !== rule.id).sort((a, b) => (a.name || '').localeCompare(b.name || ''));
          this.rulesTotal = this.rules.length;
          this.setRulesPaged();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Fout bij verwijderen regel', err);
          this.ruleError = 'Fout bij verwijderen regel.';
        }
      });
    }
  }

  ignoreCombo(combo: UniqueCombo) {
    const rule: any = {
      name: combo.name || '',
      priority: 1,
      isEnabled: true,
      conditions: [
        {field: 'Name', operator: 'Equals', value: combo.name || ''}
      ],
      isIgnored: true // Markeer als genegeerd
    };
    // categoryId niet meesturen bij ignoreCombo
    this.apiClientWrapper.createRule(rule).subscribe({
      next: () => {
        // Verwijder uit unieke transacties
        this.uniqueCombos = this.uniqueCombos.filter(c => (c.name || '').trim().toLowerCase() !== (combo.name || '').trim().toLowerCase());
        this.applyFilter();
        this.totalCombos = this.filteredCombos.length;
        this.setPagedCombos();
        this.loadRules();
        this.loadUniqueCombos(); // <-- Direct refreshen na negeren
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Fout bij negeren combo', err);
        this.ruleError = 'Fout bij negeren combo.';
      }
    });
  }

  addCategory() {
    const dialogRef = this.dialog.open(CategoryDialogComponent, {
      data: {
        category: {
          categoryId: '',
          name: '',
          kind: 'Expense',
          colorHex: '#2196F3'
        },
        isNew: true
      },
      width: '500px',
      maxHeight: '98vh',
      autoFocus: false,
      restoreFocus: false
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.isSavingCategory = true;
        this.categoryError = null;
        this.categoriesService.addCategory({
          name: result.name,
          kind: result.kind,
          colorHex: result.colorHex
        }).subscribe({
          next: () => {
            this.loadCategories();
            this.categoryStore.reload(); // <-- store opnieuw laden
            this.isSavingCategory = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.categoryError = 'Categorie opslaan mislukt.';
            this.isSavingCategory = false;
          }
        });
      }
    });
  }

  editCategory(cat: CategoryDto) {
    const dialogRef = this.dialog.open(CategoryDialogComponent, {
      data: {
        category: cat,
        isNew: false
      },
      width: '500px',
      maxHeight: '98vh',
      autoFocus: false,
      restoreFocus: false
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.isSavingCategory = true;
        this.categoryError = null;
        this.categoriesService.updateCategory({
          categoryId: cat.categoryId ?? '',
          name: result.name,
          kind: result.kind,
          colorHex: result.colorHex
        }).subscribe({
          next: () => {
            this.loadCategories();
            this.categoryStore.reload(); // <-- store opnieuw laden
            this.isSavingCategory = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.categoryError = 'Categorie opslaan mislukt.';
            this.isSavingCategory = false;
          }
        });
      }
    });
  }

  cancelCategoryEdit() {
    this.editingCategory = null;
    this.isNewCategory = false;
  }

  saveCategory() {
    if (!this.editingCategory) return;
    this.isSavingCategory = true;
    this.categoryError = null;
    if (this.isNewCategory) {
      this.categoriesService.addCategory({
        name: this.editingCategory.name!,
        kind: this.editingCategory.kind ?? 'Expense',
        colorHex: this.editingCategory.colorHex ?? '#2196F3'
      }).subscribe({
        next: () => {
          this.loadCategories(); // <-- direct na toevoegen
          this.editingCategory = null;
          this.isNewCategory = false;
          this.isSavingCategory = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.categoryError = 'Categorie opslaan mislukt.';
          this.isSavingCategory = false;
          this.cdr.detectChanges();
        }
      });
    } else {
      this.categoriesService.updateCategory({
        categoryId: this.editingCategory.categoryId ?? '',
        name: this.editingCategory.name!,
        kind: this.editingCategory.kind!,
        colorHex: this.editingCategory.colorHex ?? null
      }).subscribe({
        next: () => {
          this.loadCategories(); // <-- direct na bewerken
          this.editingCategory = null;
          this.isNewCategory = false;
          this.isSavingCategory = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.categoryError = 'Categorie opslaan mislukt.';
          this.isSavingCategory = false;
          this.cdr.detectChanges();
        }
      });
    }
  }

  openDetails(combo: UniqueCombo) {
    this.apiClientWrapper.getTransactionsByName(combo.name || '')
        .subscribe((detailsTransactions: any[]) => {
          this.dialog.open(DetailsDialogComponent, {
            width: '600px',
            data: {detailsName: combo.name || '', detailsTransactions}
          });
        });
  }

  loadCategories() {
    this.categoriesService.getCategories().subscribe(cats => {
      this.categories = cats;
      this.categoriesTotal = this.categories.length;
      this.setCategoriesPaged();
      this.cdr.detectChanges();
    });
  }

  setCategoriesPaged() {
    const start = this.categoriesPageIndex * this.categoriesPageSize;
    this.categoriesPaged = this.categories.slice(start, start + this.categoriesPageSize);
  }

  onCategoriesPage(event: any) {
    this.categoriesPageIndex = event.pageIndex;
    this.categoriesPageSize = event.pageSize || 10;
    this.setCategoriesPaged();
    this.cdr.detectChanges();
  }

  colorStyle(colorHex?: string | null): { [key: string]: string } {
    return colorHex
        ? {'background-color': colorHex, color: '#fff', padding: '0.2rem 0.5rem', 'border-radius': '4px'}
        : {};
  }

  addRule() {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      data: {
        rule: {name: '', priority: 1, categoryId: null, conditions: []}, // stuur null ipv ''
        isNew: true,
        categories: this.categories
      },
      width: '500px',
      maxHeight: '98vh',
      autoFocus: false,
      restoreFocus: false
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        if (result.categoryId === '') result.categoryId = null;
        this.apiClientWrapper.createRule(result).subscribe({
          next: () => {
            this.loadRules();
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Fout bij aanmaken regel', err);
            this.ruleError = 'Fout bij aanmaken regel.';
          }
        });
      }
    });
  }

  openAddRuleDialogForCombo(combo: UniqueCombo) {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      data: {
        rule: {
          name: combo.name || '',
          priority: 1,
          categoryId: null, // <-- stuur null ipv lege string
          isEnabled: true,
          isIgnored: false,
          conditions: [
            {
              id: crypto.randomUUID(),
              field: 'Name',
              operator: 'Equals',
              value: combo.name || ''
            }
          ]
        },
        isNew: true,
        categories: this.categories
      },
      width: '500px',
      maxHeight: '98vh',
      autoFocus: false,
      restoreFocus: false
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // categoryId mag niet een lege string zijn, stuur null als niet gekozen
        if (result.categoryId === '') result.categoryId = null;
        this.apiClientWrapper.createRule(result).subscribe({
          next: () => {
            this.loadRules();
            this.loadUniqueCombos();
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Fout bij aanmaken regel', err);
            this.ruleError = 'Fout bij aanmaken regel.';
          }
        });
      }
    });
  }

  editRule(rule: CategorizationRule) {
    const dialogRef = this.dialog.open(RuleDialogComponent, {
      data: {
        rule: {...rule, conditions: rule.conditions ? rule.conditions.map(c => ({...c})) : []},
        isNew: false,
        categories: this.categories
      },
      width: '500px',
      maxHeight: '98vh',
      autoFocus: false,
      restoreFocus: false
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // categoryId mag niet een lege string zijn, stuur null als niet gekozen
        if (result.categoryId === '') result.categoryId = null;
        this.apiClientWrapper.updateRule(result).subscribe({
          next: () => {
            this.loadRules();
            this.loadUniqueCombos();
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Fout bij updaten regel', err);
            this.ruleError = 'Fout bij updaten regel.';
          }
        });
      }
    });
  }

  onPage(event: any) {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize || 5;
    this.setPagedCombos();
  }

  getCategoryById(categoryId: string | undefined | null): CategoryDto | undefined {
    if (!categoryId) return undefined;
    return this.categories.find(c => c.categoryId === categoryId);
  }
}