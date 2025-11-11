import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { ApiService } from '../../../core/services/api.service';
import {
  Invoice,
  CreateInvoiceRequest,
  GetAllInvoicesRequest,
} from '../../../core/models';

@Injectable({
  providedIn: 'root',
})
export class InvoiceService {
  private readonly endpoint = 'invoices';
  // Cache reativo de notas (lista geral sem filtros)
  private invoicesSubject = new BehaviorSubject<Invoice[] | null>(null);
  invoices$ = this.invoicesSubject.asObservable();

  constructor(private apiService: ApiService) {}

  getAllInvoices(filters?: GetAllInvoicesRequest): Observable<Invoice[]> {
    let params = new HttpParams();

    if (filters) {
      if (filters.status) {
        params = params.set('status', filters.status);
      }
      if (filters.includeCancelled !== undefined) {
        params = params.set(
          'includeCancelled',
          filters.includeCancelled.toString()
        );
      }
      if (filters.createdFrom) {
        params = params.set('createdFrom', filters.createdFrom.toISOString());
      }
      if (filters.createdTo) {
        params = params.set('createdTo', filters.createdTo.toISOString());
      }
    }

    return this.apiService.get<Invoice[]>(this.endpoint, params).pipe(
      tap((list) => {
        // Atualiza cache apenas quando sem filtros (lista "geral")
        const hasFilters = !!(
          filters?.status ||
          filters?.includeCancelled !== undefined ||
          filters?.createdFrom ||
          filters?.createdTo
        );
        if (!hasFilters) {
          this.invoicesSubject.next(list);
        }
      })
    );
  }

  // Força refresh da lista sem filtros
  refreshAllInvoices(): Observable<Invoice[]> {
    return this.getAllInvoices({});
  }

  getInvoiceById(id: string): Observable<Invoice> {
    return this.apiService.get<Invoice>(`${this.endpoint}/${id}`);
  }

  createInvoice(request: CreateInvoiceRequest): Observable<Invoice> {
    return this.apiService
      .post<Invoice>(this.endpoint, request)
      .pipe(tap(() => this.refreshAllInvoices().subscribe()));
  }

  deleteInvoice(id: string): Observable<void> {
    return this.apiService
      .delete<void>(`${this.endpoint}/${id}`)
      .pipe(tap(() => this.refreshAllInvoices().subscribe()));
  }

  printInvoice(id: string): Observable<Invoice> {
    return this.apiService
      .post<Invoice>(`${this.endpoint}/${id}/print`, {})
      .pipe(tap(() => this.refreshAllInvoices().subscribe()));
  }

  downloadInvoicePdf(id: string): Observable<Blob> {
    return this.apiService.getBlob(`${this.endpoint}/${id}/pdf`);
  }
}
