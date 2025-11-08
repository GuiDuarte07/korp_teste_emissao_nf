export interface Product {
  id: string;
  code: string;
  description: string;
  stock: number;
  reservedStock: number;
  availableStock: number;
}

export interface CreateProductRequest {
  code: string;
  description: string;
  stock: number;
}

export interface UpdateProductRequest {
  id: string;
  code: string;
  description: string;
  stock: number;
}

export interface StockStatus {
  totalProducts: number;
  totalStock: number;
  totalReserved: number;
  totalAvailable: number;
}
