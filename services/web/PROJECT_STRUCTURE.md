# Estrutura do Projeto Angular

Este projeto foi estruturado seguindo as melhores práticas do Angular com **Standalone Components**.

## 📁 Estrutura de Pastas

```
src/app/
├── core/                          # Módulo central (singleton services e models)
│   ├── models/                    # Interfaces TypeScript baseadas nos contratos C# da API
│   │   ├── invoice.model.ts       # Invoice, InvoiceItem, CreateInvoiceRequest, GetAllInvoicesRequest
│   │   ├── product.model.ts       # Product, CreateProductRequest, UpdateProductRequest, StockStatus
│   │   ├── api-response.model.ts  # ApiResponse<T>, ErrorCode enum
│   │   └── index.ts               # Barrel export
│   └── services/                  # Serviços globais
│       └── api.service.ts         # Serviço HTTP base para comunicação com API Gateway
│
├── features/                      # Módulos de funcionalidades (lazy-loaded)
│   ├── invoices/                  # Feature de Notas Fiscais
│   │   ├── pages/                 # Páginas/Rotas da feature
│   │   │   ├── invoice-list/      # GET /api/invoices (com filtros)
│   │   │   ├── invoice-detail/    # GET /api/invoices/:id
│   │   │   └── invoice-create/    # POST /api/invoices
│   │   ├── components/            # Componentes reutilizáveis dentro da feature
│   │   │   ├── invoice-item/      # Componente para exibir item da nota
│   │   │   └── invoice-status/    # Badge de status (Open/Closed/Cancelled)
│   │   └── services/
│   │       └── invoice.service.ts # Métodos: getAllInvoices, getById, create, delete, print
│   │
│   └── inventory/                 # Feature de Inventário
│       ├── pages/
│       │   ├── product-list/      # GET /api/inventory/products
│       │   ├── product-detail/    # GET /api/inventory/products/:id
│       │   └── product-create/    # POST/PUT /api/inventory/products
│       ├── components/
│       │   ├── stock-status/      # Dashboard com StockStatus (totais)
│       │   └── product-card/      # Card de produto com estoque disponível
│       └── services/
│           └── inventory.service.ts # Métodos: getAllProducts, getById, create, update, delete, getStockStatus
│
├── shared/                        # Componentes compartilhados entre features
│   └── components/
│       ├── header/                # Toolbar/Header da aplicação
│       ├── footer/                # Footer
│       ├── loading-spinner/       # Spinner de carregamento
│       └── confirm-dialog/        # Dialog de confirmação (ex: deletar produto)
│
└── layout/                        # Layouts da aplicação
    └── main-layout/               # Layout principal com header + router-outlet + footer
```

## 🔌 API Endpoints Implementados

### Invoices Service
- `GET /api/invoices` - Lista todas as notas (com filtros: status, includeCancelled, createdFrom, createdTo)
- `GET /api/invoices/:id` - Busca nota por ID
- `POST /api/invoices` - Cria nova nota fiscal
- `DELETE /api/invoices/:id` - Cancela nota fiscal (soft delete)
- `POST /api/invoices/:id/print` - Imprime nota (confirma reserva e debita estoque)

### Inventory Service
- `GET /api/inventory/products` - Lista todos os produtos
- `GET /api/inventory/products/:id` - Busca produto por ID
- `POST /api/inventory/products` - Cria novo produto
- `PUT /api/inventory/products/:id` - Atualiza produto
- `DELETE /api/inventory/products/:id` - Deleta produto
- `GET /api/inventory/stock-status` - Status consolidado do estoque

## 🎨 Models TypeScript

Os models foram criados baseados nos **contratos C#** da pasta `shared/Shared/Contracts/`:

### Invoice Models
```typescript
interface Invoice {
  id: string;
  invoiceNumber: number;
  status: 'Open' | 'Closed';
  createdAt: Date;
  printedAt?: Date;
  cancelled: boolean;
  cancelledAt?: Date;
  items: InvoiceItem[];
}
```

### Product Models
```typescript
interface Product {
  id: string;
  code: string;
  description: string;
  stock: number;
  reservedStock: number;
  availableStock: number; // Calculado: stock - reservedStock
}
```

### API Response
```typescript
interface ApiResponse<T> {
  data?: T;
  isSuccess: boolean;
  errorCode?: string;
  errorMessage?: string;
}
```

## 🚀 Próximos Passos

Para criar componentes, use os comandos Angular CLI:

```bash
# Criar página de listagem de invoices
ng generate component features/invoices/pages/invoice-list --standalone

# Criar componente de status badge
ng generate component features/invoices/components/invoice-status --standalone

# Criar página de listagem de produtos
ng generate component features/inventory/pages/product-list --standalone

# Criar componente de card de produto
ng generate component features/inventory/components/product-card --standalone

# Criar layout principal
ng generate component layout/main-layout --standalone

# Criar header compartilhado
ng generate component shared/components/header --standalone
```

## ⚙️ Configuração

O arquivo `src/environments/environment.ts` contém a URL base da API:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5263/api'
};
```

## 📦 Dependências Instaladas

- **@angular/material** ^18.0.0 - Material Design components
- **@angular/cdk** ^18.0.0 - Component Dev Kit
- **@angular/animations** - Animações para Material

## 🎯 Padrões Utilizados

1. **Standalone Components** - Angular 18+ sem NgModules
2. **Services com `providedIn: 'root'`** - Singleton services
3. **Observable pattern** - RxJS para operações assíncronas
4. **Type-safe models** - Interfaces TypeScript para garantir type-safety
5. **Barrel exports** - `index.ts` para facilitar imports
6. **Feature-based structure** - Organização por funcionalidade, não por tipo de arquivo

## 🛠️ Como Usar os Serviços

```typescript
// Exemplo de uso no componente
import { Component, OnInit } from '@angular/core';
import { InvoiceService } from './features/invoices/services/invoice.service';
import { Invoice } from './core/models';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  template: `...`
})
export class InvoiceListComponent implements OnInit {
  invoices: Invoice[] = [];

  constructor(private invoiceService: InvoiceService) {}

  ngOnInit() {
    this.invoiceService.getAllInvoices().subscribe({
      next: (response) => {
        if (response.isSuccess && response.data) {
          this.invoices = response.data;
        }
      },
      error: (error) => console.error('Error loading invoices', error)
    });
  }
}
```

## 📝 Notas

- Todos os models TypeScript refletem exatamente os contratos C# do backend
- A estrutura suporta lazy loading para melhor performance
- HttpClient já configurado no `app.config.ts` (standalone apps)
- Material Design theme já configurado em `styles.scss`
