import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  Product,
  CreateProductRequest,
  UpdateProductRequest,
  StockStatus,
} from '../../../core/models';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  private readonly endpoint = 'inventory';
  // Cache reativo de produtos
  private productsSubject = new BehaviorSubject<Product[] | null>(null);
  products$ = this.productsSubject.asObservable();

  constructor(private apiService: ApiService) {}

  // Busca produtos e atualiza cache interno
  getAllProducts(): Observable<Product[]> {
    return this.apiService
      .get<Product[]>(`${this.endpoint}/products`)
      .pipe(tap((list) => this.productsSubject.next(list)));
  }

  // Força um refresh da lista interna
  refreshAllProducts(): Observable<Product[]> {
    return this.getAllProducts();
  }

  getProductById(id: string): Observable<Product> {
    return this.apiService.get<Product>(`${this.endpoint}/products/${id}`);
  }

  createProduct(request: CreateProductRequest): Observable<Product> {
    return this.apiService
      .post<Product>(`${this.endpoint}/products`, request)
      .pipe(tap(() => this.refreshAllProducts().subscribe()));
  }

  updateProduct(request: UpdateProductRequest): Observable<Product> {
    return this.apiService
      .put<Product>(`${this.endpoint}/products/${request.id}`, request)
      .pipe(tap(() => this.refreshAllProducts().subscribe()));
  }

  deleteProduct(id: string): Observable<void> {
    return this.apiService
      .delete<void>(`${this.endpoint}/products/${id}`)
      .pipe(tap(() => this.refreshAllProducts().subscribe()));
  }

  getStockStatus(): Observable<StockStatus> {
    return this.apiService.get<StockStatus>(`${this.endpoint}/stock-status`);
  }
}
