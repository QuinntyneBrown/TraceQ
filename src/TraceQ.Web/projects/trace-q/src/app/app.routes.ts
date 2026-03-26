import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('features').then((m) => m.DashboardComponent),
  },
  {
    path: 'search',
    loadComponent: () => import('features').then((m) => m.SearchComponent),
  },
  {
    path: 'import',
    loadComponent: () => import('features').then((m) => m.ImportComponent),
  },
  {
    path: 'requirements',
    loadComponent: () => import('features').then((m) => m.RequirementsComponent),
  },
  { path: '**', redirectTo: 'dashboard' },
];
