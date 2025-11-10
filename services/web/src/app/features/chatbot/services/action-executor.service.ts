import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { InventoryService } from '../../inventory/services/inventory.service';
import { InvoiceService } from '../../invoices/services/invoice.service';
import { FunctionResult } from '../models';
import { CreateInvoiceRequest, Invoice, Product } from '../../../core/models';

@Injectable({
  providedIn: 'root',
})
export class ActionExecutorService {
  constructor(
    private inventoryService: InventoryService,
    private invoiceService: InvoiceService
  ) {}

  async execute(functionName: string, args: any): Promise<FunctionResult> {
    try {
      switch (functionName) {
        case 'create_product':
          return await this.createProduct(args);

        case 'create_invoice':
          return await this.createInvoice(args.products);

        case 'list_products':
          return await this.listProducts(args.searchTerm);

        case 'get_invoice_by_number':
          return await this.getInvoiceByNumber(args.invoiceNumber);

        case 'list_invoices':
          return await this.listInvoices(args);

        case 'cancel_invoice':
          return await this.cancelInvoice(args.invoiceNumber);

        case 'print_invoice':
          return await this.printInvoice(args.invoiceNumber);

        default:
          return {
            success: false,
            error: `Função desconhecida: ${functionName}`,
          };
      }
    } catch (error: any) {
      console.error(`Erro ao executar ${functionName}:`, error);
      return {
        success: false,
        error:
          error?.error?.errorMessage || error?.message || 'Erro desconhecido',
      };
    }
  }

  private async createProduct(args: {
    code: string;
    description: string;
    initialStock: number;
  }): Promise<FunctionResult> {
    try {
      const product = await firstValueFrom(
        this.inventoryService.createProduct({
          code: args.code,
          description: args.description,
          initialStock: args.initialStock,
        })
      );

      return {
        success: true,
        data: {
          id: product.id,
          code: product.code,
          description: product.description,
          stock: product.stock,
          reservedStock: product.reservedStock,
          availableStock: product.stock - product.reservedStock,
          message: `Produto "${product.code}" criado com sucesso!`,
        },
      };
    } catch (error: any) {
      return {
        success: false,
        error: error?.error?.errorMessage || 'Erro ao criar produto',
      };
    }
  }

  private async createInvoice(
    products: Array<{ productCode: string; quantity: number }>
  ): Promise<FunctionResult> {
    try {
      // 1. Buscar todos os produtos
      const allProducts = await firstValueFrom(
        this.inventoryService.getAllProducts()
      );

      // 2. Resolver códigos para IDs
      const invoiceProducts: Array<{ productId: string; quantity: number }> =
        [];
      const notFound: string[] = [];

      for (const item of products) {
        const product = allProducts.find(
          (p: Product) =>
            p.code.toLowerCase() === item.productCode.toLowerCase()
        );

        if (product) {
          invoiceProducts.push({
            productId: product.id,
            quantity: item.quantity,
          });
        } else {
          notFound.push(item.productCode);
        }
      }

      if (notFound.length > 0) {
        return {
          success: false,
          error: `Produtos não encontrados: ${notFound.join(
            ', '
          )}. Use a função list_products para ver os códigos disponíveis.`,
        };
      }

      // 3. Criar nota fiscal
      const request: CreateInvoiceRequest = {
        items: invoiceProducts,
      };

      const invoice = await firstValueFrom(
        this.invoiceService.createInvoice(request)
      );

      return {
        success: true,
        data: {
          invoiceNumber: invoice.invoiceNumber,
          id: invoice.id,
          status: invoice.status,
          itemCount: invoice.items.length,
          items: invoice.items.map((item) => ({
            code: item.productCode,
            description: item.productDescription,
            quantity: item.quantity,
          })),
        },
      };
    } catch (error: any) {
      return {
        success: false,
        error: error?.error?.errorMessage || 'Erro ao criar nota fiscal',
      };
    }
  }

  private async listProducts(searchTerm?: string): Promise<FunctionResult> {
    try {
      const products = await firstValueFrom(
        this.inventoryService.getAllProducts()
      );

      let filteredProducts = products;

      if (searchTerm) {
        const term = searchTerm.toLowerCase();
        filteredProducts = products.filter(
          (p: Product) =>
            p.code.toLowerCase().includes(term) ||
            p.description.toLowerCase().includes(term)
        );
      }

      return {
        success: true,
        data: filteredProducts.map((p: Product) => ({
          code: p.code,
          description: p.description,
          stock: p.stock,
          reservedStock: p.reservedStock,
          availableStock: p.stock - p.reservedStock,
        })),
      };
    } catch (error: any) {
      return {
        success: false,
        error: error?.error?.errorMessage || 'Erro ao listar produtos',
      };
    }
  }

  private async getInvoiceByNumber(
    invoiceNumber: string
  ): Promise<FunctionResult> {
    try {
      const invoices = await firstValueFrom(
        this.invoiceService.getAllInvoices({})
      );

      const invoice = invoices.find(
        (inv: Invoice) =>
          inv.invoiceNumber.toString() === invoiceNumber ||
          `NF-${inv.invoiceNumber.toString().padStart(6, '0')}` ===
            invoiceNumber.toUpperCase()
      );

      if (!invoice) {
        return {
          success: false,
          error: `Nota fiscal ${invoiceNumber} não encontrada`,
        };
      }

      // Buscar detalhes completos
      const fullInvoice = await firstValueFrom(
        this.invoiceService.getInvoiceById(invoice.id)
      );

      return {
        success: true,
        data: {
          invoiceNumber: `NF-${fullInvoice.invoiceNumber
            .toString()
            .padStart(6, '0')}`,
          status: fullInvoice.status,
          cancelled: fullInvoice.cancelled,
          createdAt: fullInvoice.createdAt,
          printedAt: fullInvoice.printedAt,
          items: fullInvoice.items.map((item) => ({
            code: item.productCode,
            description: item.productDescription,
            quantity: item.quantity,
          })),
          totalItems: fullInvoice.items.length,
          totalQuantity: fullInvoice.items.reduce(
            (sum, item) => sum + item.quantity,
            0
          ),
        },
      };
    } catch (error: any) {
      return {
        success: false,
        error: error?.error?.errorMessage || 'Erro ao buscar nota fiscal',
      };
    }
  }

  private async listInvoices(filters: any): Promise<FunctionResult> {
    try {
      const invoices = await firstValueFrom(
        this.invoiceService.getAllInvoices({
          status: filters.status,
          includeCancelled: filters.includeCancelled || false,
        })
      );

      return {
        success: true,
        data: invoices.map((inv) => ({
          invoiceNumber: inv.invoiceNumber,
          status: inv.status,
          cancelled: inv.cancelled,
          createdAt: inv.createdAt,
          printedAt: inv.printedAt,
          itemCount: inv.items?.length || 0,
        })),
      };
    } catch (error: any) {
      return {
        success: false,
        error: error?.error?.errorMessage || 'Erro ao listar notas fiscais',
      };
    }
  }

  private async cancelInvoice(invoiceNumber: string): Promise<FunctionResult> {
    try {
      // Buscar invoice por número
      const invoices = await firstValueFrom(
        this.invoiceService.getAllInvoices({})
      );

      const invoice = invoices.find(
        (inv: Invoice) =>
          inv.invoiceNumber.toString() === invoiceNumber ||
          `NF-${inv.invoiceNumber.toString().padStart(6, '0')}` ===
            invoiceNumber.toUpperCase()
      );

      if (!invoice) {
        return {
          success: false,
          error: `Nota fiscal ${invoiceNumber} não encontrada`,
        };
      }

      if (invoice.cancelled) {
        return {
          success: false,
          error: 'Esta nota fiscal já está cancelada',
        };
      }

      await firstValueFrom(this.invoiceService.deleteInvoice(invoice.id));

      return {
        success: true,
        data: {
          message: `Nota fiscal ${invoiceNumber} cancelada com sucesso`,
          invoiceNumber: invoiceNumber,
        },
      };
    } catch (error: any) {
      return {
        success: false,
        error: error?.error?.errorMessage || 'Erro ao cancelar nota fiscal',
      };
    }
  }

  private async printInvoice(invoiceNumber: string): Promise<FunctionResult> {
    try {
      // Buscar invoice por número
      const invoices = await firstValueFrom(
        this.invoiceService.getAllInvoices({})
      );

      const invoice = invoices.find(
        (inv: Invoice) =>
          inv.invoiceNumber.toString() === invoiceNumber ||
          `NF-${inv.invoiceNumber.toString().padStart(6, '0')}` ===
            invoiceNumber.toUpperCase()
      );

      if (!invoice) {
        return {
          success: false,
          error: `Nota fiscal ${invoiceNumber} não encontrada`,
        };
      }

      if (invoice.cancelled) {
        return {
          success: false,
          error: 'Não é possível imprimir uma nota fiscal cancelada',
        };
      }

      if (invoice.printedAt) {
        return {
          success: false,
          error: 'Esta nota fiscal já foi impressa',
        };
      }

      await firstValueFrom(this.invoiceService.printInvoice(invoice.id));

      return {
        success: true,
        data: {
          message: `Nota fiscal ${invoiceNumber} impressa com sucesso`,
          invoiceNumber: invoiceNumber,
        },
      };
    } catch (error: any) {
      return {
        success: false,
        error: error?.error?.errorMessage || 'Erro ao imprimir nota fiscal',
      };
    }
  }
}
