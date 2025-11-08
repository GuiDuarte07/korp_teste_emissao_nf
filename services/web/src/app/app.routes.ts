import { Routes } from '@angular/router';
import { ProductListComponent } from './features/inventory/pages/product-list/product-list.component';
import { ProductFormComponent } from './features/inventory/pages/product-form/product-form.component';
import { InvoiceListComponent } from './features/invoices/pages/invoice-list/invoice-list.component';
import { InvoiceFormComponent } from './features/invoices/pages/invoice-form/invoice-form.component';
import { InvoiceDetailComponent } from './features/invoices/pages/invoice-detail/invoice-detail.component';

export const routes: Routes = [
  { path: '', redirectTo: '/products', pathMatch: 'full' },
  { path: 'products', component: ProductListComponent },
  { path: 'products/new', component: ProductFormComponent },
  { path: 'products/edit/:id', component: ProductFormComponent },
  { path: 'invoices', component: InvoiceListComponent },
  { path: 'invoices/new', component: InvoiceFormComponent },
  { path: 'invoices/:id', component: InvoiceDetailComponent },
  { path: '**', redirectTo: '/products' },
];
