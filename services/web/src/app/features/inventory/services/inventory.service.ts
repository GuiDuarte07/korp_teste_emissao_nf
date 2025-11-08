import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
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

  constructor(private apiService: ApiService) {}

  getAllProducts(): Observable<Product[]> {
    return this.apiService.get<Product[]>(`${this.endpoint}/products`);
  }

  getProductById(id: string): Observable<Product> {
    return this.apiService.get<Product>(`${this.endpoint}/products/${id}`);
  }

  createProduct(request: CreateProductRequest): Observable<Product> {
    return this.apiService.post<Product>(`${this.endpoint}/products`, request);
  }

  updateProduct(request: UpdateProductRequest): Observable<Product> {
    return this.apiService.put<Product>(
      `${this.endpoint}/products/${request.id}`,
      request
    );
  }

  deleteProduct(id: string): Observable<void> {
    return this.apiService.delete<void>(`${this.endpoint}/products/${id}`);
  }

  getStockStatus(): Observable<StockStatus> {
    return this.apiService.get<StockStatus>(`${this.endpoint}/stock-status`);
  }
}
