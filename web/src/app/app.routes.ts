import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./products/product-list').then((m) => m.ProductList), title: 'Inventory' },
  {
    path: 'products/new',
    loadComponent: () => import('./products/register-product').then((m) => m.RegisterProduct),
    title: 'New product · Inventory',
  },
  {
    path: 'products/:id',
    loadComponent: () => import('./products/product-detail').then((m) => m.ProductDetail),
    title: 'Product · Inventory',
  },
  { path: '**', redirectTo: '' },
];
