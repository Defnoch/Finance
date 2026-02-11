import { Component, OnInit, ChangeDetectorRef, ViewChild, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule, registerLocaleData } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { BaseChartDirective } from 'ng2-charts';
import { FormsModule } from '@angular/forms';
import {AccountDto, CategoryDto} from '../../core/api/api-client';
import { TransactionDto } from '../../core/api/api-client';
import { effect } from '@angular/core';
import localeNl from '@angular/common/locales/nl';
import { Chart, ChartType, ChartConfiguration, LineController, LineElement, PointElement, LinearScale, CategoryScale, BarController, BarElement, DoughnutController, ArcElement, Legend, Tooltip, Filler } from 'chart.js';
import { AccountYearStore } from '../../stores/account-year.store';
import { AccountStore } from '../../stores/account.store';
import { CategoryStore } from '../../stores/category.store';
import { TransactionStore } from '../../stores/transaction.store';

// Verwijder statische Chart.js imports

@Component({
  selector: 'app-rapportage',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    MatCardModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    BaseChartDirective,
    FormsModule // <-- toegevoegd voor ngModel
  ],
  templateUrl: './rapportage.component.html',
  styleUrls: ['./rapportage.component.scss']
})
export class RapportageComponent implements OnInit {
  transactions: Array<{
    date: string;
    amount: number;
    resultingBalance?: number | null;
    category: string;
    merchant: string;
    categoryId?: string | null;
    accountId?: string | null;
    linkedTransactionId?: string | null;
  }> = [];
  categories: CategoryDto[] = [];
  accounts: AccountDto[] = [];
  loading = true;
  error: string | null = null;
  private fixedIndexes = new Set([3, 10, 17, 24, 29, 33]);
  spaarrekeningSaldoData: { [accountId: string]: { date: string, saldo: number | null }[] } = {};
  allTransactions: TransactionDto[] = [];

  @ViewChild('saldoChart') saldoChart?: BaseChartDirective;
  @ViewChild('stackedBarChart') stackedBarChart?: BaseChartDirective;

  constructor(
    private cd: ChangeDetectorRef,
    private accountStore: AccountStore,
    private categoryStore: CategoryStore
  ) {
    registerLocaleData(localeNl, 'nl-NL');
    this.accountYearStore = inject(AccountYearStore);
    this.transactionStore = inject(TransactionStore);
    this.accountStore = inject(AccountStore);
    effect(() => {
      const txRaw = this.transactionStore.transactions();
      this.accounts = this.accountStore.accounts();
      this.categories = this.categoryStore.categories();
      
      const tx = Array.isArray(txRaw) ? txRaw : [];
      const accounts = this.accounts;
      const categories = this.categories;

      if (categories.length > 0 && accounts.length > 0 && tx.length > 0 && this.accountYearStore.isReady()) {
        this.allTransactions = tx;
        this.updateAvailablePeriods();
        if (this.selectedPeriod) {
          this.fetchData(this.selectedPeriod.from, this.selectedPeriod.to);
        }
        this.loadSpaarrekeningSaldoData();
        this.loading = false;
        this.cd.markForCheck();
      }
    });
  }

  private accountYearStore: AccountYearStore;

  // Toegevoegd: Periodes/selectie
  availablePeriods: { label: string, from: string, to: string }[] = [];
  selectedPeriod?: { label: string, from: string, to: string };

  updateAvailablePeriods() {
    // Verzamel unieke maanden op basis van bookingDate
    const months = new Set<string>();
    this.allTransactions.forEach(dto => {
      if (dto.bookingDate && dto.bookingDate.length >= 7) {
        months.add(dto.bookingDate.substring(0, 7)); // yyyy-MM
      }
    });
    const sortedMonths = Array.from(months).sort().reverse();
    this.availablePeriods = sortedMonths.map(month => {
      const [year, m] = month.split('-');
      const from = `${year}-${m}-01`;
      const to = new Date(Number(year), Number(m), 0); // laatste dag van maand
      const toStr = `${year}-${m}-${to.getDate().toString().padStart(2, '0')}`;
      return {
        label: `${from} t/m ${toStr}`,
        from,
        to: toStr
      };
    });
    // Voeg een custom optie toe voor een eigen periode (optioneel)
    // this.availablePeriods.push({ label: 'Aangepaste periode...', from: '', to: '' });
    if (this.availablePeriods.length > 0) {
      this.selectedPeriod = this.availablePeriods[0];
    }
  }

  onPeriodChange() {
    if (!this.selectedPeriod) return;
    // Bepaal het bereik van vier maanden
    const fromDate = new Date(this.selectedPeriod.from);
    const minMonth = new Date(fromDate);
    minMonth.setMonth(minMonth.getMonth() - 2);
    const maxMonth = new Date(fromDate);
    maxMonth.setMonth(maxMonth.getMonth() + 1);
    const from = `${minMonth.getFullYear()}-${(minMonth.getMonth() + 1).toString().padStart(2, '0')}-01`;
    const toDate = new Date(maxMonth.getFullYear(), maxMonth.getMonth() + 1, 0);
    const to = `${maxMonth.getFullYear()}-${(maxMonth.getMonth() + 1).toString().padStart(2, '0')}-${toDate.getDate().toString().padStart(2, '0')}`;
    this.fetchData(from, to);
    this.loadSpaarrekeningSaldoData();
    // Update alleen de relevante grafieken
    if (this.saldoChart) this.saldoChart.update();
    if (this.stackedBarChart && typeof this.stackedBarChart.update === 'function') {
      this.stackedBarChart.update();
    }
  }

  fetchData(fromDate: string, toDate: string) {
    this.loading = true;
    // Filter uit allTransactions
    const dtos = this.allTransactions.filter(dto => (dto.bookingDate ?? '') >= fromDate && (dto.bookingDate ?? '') <= toDate);
    this.transactions = dtos.map(dto => ({
      date: dto.bookingDate || '',
      amount: Number(dto.amount),
      resultingBalance: dto.resultingBalance ?? null,
      category: dto.categoryName || 'Onbekend',
      merchant: dto.description || '-',
      categoryId: dto.categoryId || null,
      accountId: dto.accountId || null,
      linkedTransactionId: dto.linkedTransactionId || null
    }));
    this.updateSaldoChartData();
    this.updateStackedBarData();
    this.updateDoughnutData();
    this.loading = false;
    this.cd.markForCheck();
  }

  updateDoughnutData() {
    // Map: categoryId -> totaalbedrag
    const cats: { [catId: string]: number } = {};
    this.kpiTransactions.filter(t => t.amount < 0).forEach(t => {
      const catId = (t as any).categoryId || 'onbekend';
      cats[catId] = (cats[catId] || 0) + Math.abs(t.amount);
    });
    this.doughnutLabels = Object.keys(cats).map(catId =>
        this.categories.find(c => c.categoryId === catId)?.name || 'Onbekend'
    );
    const colors = Object.keys(cats).map(catId =>
        this.categories.find(c => c.categoryId === catId)?.colorHex || '#9E9E9E'
    );
    this.doughnutData = {
      datasets: [{
        data: Object.values(cats),
        backgroundColor: colors,
      }]
    };
  }

  loadSpaarrekeningSaldoData() {
    const spaarrekeningen = this.accounts.filter(a => (a.type || '').toLowerCase().includes('spaar'));
    for (const account of spaarrekeningen) {
      // Filter uit allTransactions
      const dtos = this.allTransactions.filter(dto => dto.accountId === account.accountId);
      const sorted = [...dtos].sort((a, b) => (a.bookingDate || '').localeCompare(b.bookingDate || ''));
      const saldoPerDag: Array<{ date: string; saldo: number | null }> = [];
      if (this.selectedPeriod) {
        const from = new Date(this.selectedPeriod.from);
        const to = new Date(this.selectedPeriod.to);
        for (let d = new Date(from); d <= to; d.setDate(d.getDate() + 1)) {
          const dateStr = d.toISOString().slice(0, 10);
          const dagTrans = sorted.filter(t => (t.bookingDate || '') <= dateStr && t.resultingBalance != null);
          if (dagTrans.length > 0) {
            const last = dagTrans[dagTrans.length - 1];
            saldoPerDag.push({date: dateStr, saldo: last.resultingBalance ?? null});
          } else {
            saldoPerDag.push({date: dateStr, saldo: null});
          }
        }
      }
      this.spaarrekeningSaldoData[account.accountId || ''] = saldoPerDag;
      this.cd.markForCheck();
    }
    this.updateSaldoChartData();
  }

  updateSaldoChartData() {
    const saldoColors = [
      '#1976d2', // Hoofdrekening: blauw
      '#43a047', // Spaarrekening 1: groen
      '#ff9800', // Spaarrekening 2: oranje
      '#8e24aa', // Spaarrekening 3: paars
      '#e53935', // Spaarrekening 4: rood
      '#00bcd4', // Spaarrekening 5: turquoise
    ];
    const hoofdData = [{
      data: this.saldoData.map(d => d.saldo ?? 0),
      label: 'Saldo',
      borderColor: saldoColors[0],
      backgroundColor: 'rgba(25, 118, 210, 0.15)',
      fill: true
    }];
    const spaarData = Object.entries(this.spaarrekeningSaldoData).map(([accountId, data], idx) => {
      const acc = this.accounts.find(a => a.accountId === accountId);
      const color = saldoColors[idx + 1] || '#43a047';
      return {
        data: (data as Array<{ saldo: number | null }>).map((d: { saldo: number | null }) => d.saldo ?? 0),
        label: acc ? (acc.name || acc.accountIdentifier || 'Spaarrekening') : 'Spaarrekening',
        borderColor: color,
        backgroundColor: color + '33',
        fill: false
      };
    });
    this.saldoChartData = hoofdData.concat(spaarData);
  }

  updateStackedBarData() {
    this.stackedBarData = {
      datasets: this.monthlyExpenses.cats.map((cat, i) => {
        const catObj = this.categories.find(c => c.name === cat);
        const color = catObj?.colorHex || '#9E9E9E';
        const isOnbekend = cat === 'Onbekend';
        return {
          label: cat,
          data: this.monthlyExpenses.data[i],
          backgroundColor: color,
          stack: 'Stack 0',
          hidden: isOnbekend
        };
      })
    };
  }

  private transactionStore = inject(TransactionStore);

  private async loadChartJs() {
    const datalabels = (await import('chartjs-plugin-datalabels')).default;
    Chart.register(
      LineController, LineElement, PointElement, LinearScale, CategoryScale,
      BarController, BarElement,
      DoughnutController, ArcElement,
      Legend, Tooltip,
      datalabels,
      Filler
    );
    this.ChartDataLabels = datalabels;
    this.chartReady = true;
  }

  async ngOnInit(): Promise<void> {
    this.accountYearStore.ensureSelection();
    await this.loadChartJs();
    this.loading = true;
    this.accountStore.load();
    this.categoryStore.load();
  }

  getCategories() {
    return this.categoryStore.categories();
  }

  getAccounts() {
    return this.accountStore.accounts();
  }

  // KPI's
  get kpiTransactions() {
    if (!this.selectedPeriod || !this.selectedPeriod.from || !this.selectedPeriod.to) return [];
    const from = this.selectedPeriod.from;
    const to = this.selectedPeriod.to;
    return this.transactions
    .filter(t => from && to && t.date >= from && t.date <= to)
    .sort((a, b) => a.date.localeCompare(b.date));
  }

  get income() {
    return this.kpiTransactions
      .filter(t => t.amount > 0 && t.linkedTransactionId == null)
      .reduce((sum, t) => sum + t.amount, 0);
  }

  get expenses() {
    return this.kpiTransactions
      .filter(t => t.amount < 0 && t.accountId && t.linkedTransactionId == null)
      .reduce((sum, t) => sum + t.amount, 0);
  }

  get balance() {
    return this.income + this.expenses;
  }

  // Dynamisch genereren van dagen binnen geselecteerde periode
  get daysOfChart() {
    if (!this.selectedPeriod) return [];
    const days: string[] = [];
    const from = new Date(this.selectedPeriod.from);
    for (let d = new Date(from); d <= new Date(); d.setDate(d.getDate() + 1)) {
      days.push(d.toISOString().slice(0, 10));
    }
    return days;
  }

  // Cumulatief saldo per dag op basis van resultingBalance binnen periode
  get saldoData() {
    const sorted = [...this.transactions].sort((a, b) => a.date.localeCompare(b.date));
    const saldoPerDag: Array<{ date: string; saldo: number | null }> = [];
    for (const date of this.daysOfChart) {
      const dagTrans = sorted.filter(t => t.date <= date && t.resultingBalance != null);
      if (dagTrans.length > 0) {
        const last = dagTrans[dagTrans.length - 1];
        saldoPerDag.push({date, saldo: last.resultingBalance ?? null});
      } else {
        saldoPerDag.push({date, saldo: null});
      }
    }
    return saldoPerDag;
  }

  // Dynamische labels voor saldoChartLabels binnen periode
  get saldoChartLabels() {
    if (!this.selectedPeriod) return [];
    const from = new Date(this.selectedPeriod.from);
    const maandnaam = from.toLocaleString('nl-NL', {month: 'short'});
    return this.daysOfChart.map(date => {
      const d = new Date(date);
      // Toon label op elke maandag en laatste dag
      if (d.getDay() === 1 || (this.selectedPeriod && date === this.selectedPeriod.to)) {
        return `${d.getDate()} ${maandnaam}`;
      }
      return '';
    });
  }
  
  saldoChartData: any[] = [];
  saldoChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    elements: {
      line: {
        tension: 0.4,
        cubicInterpolationMode: 'monotone',
        fill: true,
        backgroundColor: '#1976d2', // vaste blauwe kleur, geen fade/gradient
      },
      point: {
        radius: (ctx: any) => this.fixedIndexes.has(ctx.dataIndex) ? 3 : 0,
        hoverRadius: (ctx: any) => this.fixedIndexes.has(ctx.dataIndex) ? 6 : 3
      }
    },
    scales: {
      x: {
        type: 'category',
        offset: true,             
        ticks: {
          autoSkip: false,   
          maxRotation: 0,
          padding: 12    
        },
        grid: {display: false}
      },
      y: {
        grid: {display: true}
      }
    },
    plugins: {
      legend: {display: true, position: 'top'}, 
      datalabels: {
        display: false 
      }
    }
  };
  saldoChartType: 'line' = 'line';


  doughnutLabels: string[] = [];
  doughnutData: any = {datasets: []};
  doughnutChartType: ChartType = 'doughnut';
  doughnutChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    plugins: {
      legend: {display: true, position: 'top'},
      datalabels: {
        color: '#fff',
        font: {weight: 'bold'},
        formatter: (value: number) => {
          return value.toLocaleString('nl-NL', {style: 'currency', currency: 'EUR', minimumFractionDigits: 2});
        },
        display: true,
      },
      tooltip: {
        callbacks: {
          label: function (context: any) {
            const value = context.parsed;
            return value.toLocaleString('nl-NL', {style: 'currency', currency: 'EUR', minimumFractionDigits: 2});
          }
        }
      }
    }
  };


  get monthlyExpenses() {
    if (!this.selectedPeriod) return {months: [], cats: [], data: []};

    const fromDate = new Date(this.selectedPeriod.from);
    const months: { label: string, from: string, to: string }[] = [];
    for (let offset = -2; offset <= 1; offset++) {
      const d = new Date(fromDate);
      d.setMonth(d.getMonth() + offset);
      const year = d.getFullYear();
      const month = (d.getMonth() + 1).toString().padStart(2, '0');
      const from = `${year}-${month}-01`;
      const to = new Date(year, d.getMonth() + 1, 0); // laatste dag van maand
      const toStr = `${year}-${month}-${to.getDate().toString().padStart(2, '0')}`;
      months.push({
        label: d.toLocaleString('nl-NL', {month: 'short'}),
        from,
        to: toStr
      });
    }

    const cats: string[] = Array.from(new Set(this.transactions.map(t => t.categoryId || 'onbekend')));
    const catNames = cats.map(catId => {
      const cat = this.categories.find(c => c.categoryId === catId);
      return cat ? cat.name : 'Onbekend';
    });

    const data = cats.map(catId => months.map(m => {
      return this.transactions.filter(t => t.date >= m.from && t.date <= m.to && (t.categoryId || 'onbekend') === catId && t.amount < 0)
          .reduce((sum, t) => sum + Math.abs(t.amount), 0);
    }));
    return {months: months.map(m => m.label), cats: catNames, data};
  }

  get stackedBarLabels() {
    return this.monthlyExpenses.months;
  }

  stackedBarData: any = {datasets: []};
  stackedBarChartType: ChartType = 'bar';
  stackedBarChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    plugins: {
      legend: {position: 'top'},
      datalabels: {
        display: false
      },
      tooltip: {
        enabled: true,
        callbacks: {
          label: function (context: any) {
    
            const value = context.raw;
            if (typeof value === 'number') {
              return value.toLocaleString('nl-NL', {style: 'currency', currency: 'EUR', minimumFractionDigits: 2});
            }
            return '';
          }
        }
      }
    },
    scales: {x: {}, y: {stacked: true}}
  };

  ChartDataLabels: any = null;
  chartReady = false;
}
