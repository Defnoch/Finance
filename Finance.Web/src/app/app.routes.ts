import { Routes } from '@angular/router';

export const routes: Routes = [
  // Uitloggen route NIET beveiligen!
  {
    path: 'logged-out',
    loadComponent: () => import('./features/logout/logout.component').then(m => m.LogoutComponent)
  },
  // Beveiligde routes
  {
    path: '',
    loadComponent: () => import('./features/start/start.component').then(m => m.StartComponent)
  },
  {
    path: 'import',
    loadComponent: () => import('./features/import/import.component').then(m => m.ImportComponent)
  },
  {
    path: 'transactions',
    loadComponent: () => import('./features/transactions/transactions.component').then(m => m.TransactionsComponent)
  },
  {
    path: 'reports',
    loadComponent: () => import('./features/reports/rapportage.component').then(m => m.RapportageComponent)
  },
  {
    path: 'rules',
    loadComponent: () => import('./features/rules/rules.component').then(m => m.RulesComponent)
  },
  { path: '**', redirectTo: '' }
];
