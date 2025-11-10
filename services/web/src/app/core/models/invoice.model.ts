export enum InvoiceStatus {
  Open = 'Open',
  Closed = 'Closed',
}

export interface Invoice {
  id: string;
  invoiceNumber: number;
  status: 'Open' | 'Closed';
  createdAt: Date;
  printedAt?: Date;
  cancelled: boolean;
  cancelledAt?: Date;
  items: InvoiceItem[];
}

export interface InvoiceItem {
  id: string;
  productId: string;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface CreateInvoiceRequest {
  items: CreateInvoiceItemRequest[];
  idempotencyKey?: string; // Chave única para garantir idempotência
}

export interface CreateInvoiceItemRequest {
  productId: string;
  quantity: number;
}

export interface GetAllInvoicesRequest {
  status?: string;
  includeCancelled?: boolean;
  createdFrom?: Date;
  createdTo?: Date;
}
