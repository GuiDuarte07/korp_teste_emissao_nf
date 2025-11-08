import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
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

    return this.apiService.get<Invoice[]>(this.endpoint, params);
  }

  getInvoiceById(id: string): Observable<Invoice> {
    return this.apiService.get<Invoice>(`${this.endpoint}/${id}`);
  }

  createInvoice(request: CreateInvoiceRequest): Observable<Invoice> {
    return this.apiService.post<Invoice>(this.endpoint, request);
  }

  deleteInvoice(id: string): Observable<void> {
    return this.apiService.delete<void>(`${this.endpoint}/${id}`);
  }

  printInvoice(id: string): Observable<Invoice> {
    return this.apiService.post<Invoice>(`${this.endpoint}/${id}/print`, {});
  }
}
