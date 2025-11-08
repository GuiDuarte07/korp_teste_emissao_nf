import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormArray,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { InvoiceService } from '../../services/invoice.service';
import { InventoryService } from '../../../inventory/services/inventory.service';
import { Product, CreateInvoiceRequest } from '../../../../core/models';

@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatDividerModule,
  ],
  templateUrl: './invoice-form.component.html',
  styleUrl: './invoice-form.component.scss',
})
export class InvoiceFormComponent implements OnInit {
  invoiceForm: FormGroup;
  products: Product[] = [];
  loading = false;
  submitting = false;

  constructor(
    private fb: FormBuilder,
    private invoiceService: InvoiceService,
    private inventoryService: InventoryService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.invoiceForm = this.fb.group({
      items: this.fb.array([]),
    });
  }

  ngOnInit(): void {
    this.loadProducts();
    this.addItem();
  }

  get items(): FormArray {
    return this.invoiceForm.get('items') as FormArray;
  }

  loadProducts(): void {
    this.loading = true;
    this.inventoryService.getAllProducts().subscribe({
      next: (products) => {
        this.products = products.filter((p) => p.availableStock > 0);
        this.loading = false;
      },
      error: (error) => {
        console.error('Erro ao carregar produtos', error);
        const errorMessage =
          error?.error?.errorMessage || 'Erro ao carregar produtos';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000,
        });
        this.loading = false;
      },
    });
  }

  createItemFormGroup(): FormGroup {
    return this.fb.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      availableStock: [0],
    });
  }

  addItem(): void {
    this.items.push(this.createItemFormGroup());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    } else {
      this.snackBar.open(
        'A nota fiscal deve ter pelo menos um item',
        'Fechar',
        { duration: 3000 }
      );
    }
  }

  onProductChange(index: number): void {
    const item = this.items.at(index);
    const productId = item.get('productId')?.value;
    const product = this.products.find((p) => p.id === productId);

    if (product) {
      // Verificar se o produto já foi adicionado em outro item
      const isDuplicate = this.items.controls.some((control, i) => {
        return i !== index && control.get('productId')?.value === productId;
      });

      if (isDuplicate) {
        this.snackBar.open(
          'Este produto já foi adicionado na nota fiscal',
          'Fechar',
          { duration: 3000 }
        );
        item.patchValue({
          productId: '',
          quantity: 1,
          availableStock: 0,
        });
        return;
      }

      item.patchValue({
        availableStock: product.availableStock,
        quantity: 1,
      });

      // Update quantity validator with max available stock
      item
        .get('quantity')
        ?.setValidators([
          Validators.required,
          Validators.min(1),
          Validators.max(product.availableStock),
        ]);
      item.get('quantity')?.updateValueAndValidity();
    }
  }

  getProductName(productId: string): string {
    const product = this.products.find((p) => p.id === productId);
    return product ? `${product.code} - ${product.description}` : '';
  }

  getAvailableStock(index: number): number {
    return this.items.at(index).get('availableStock')?.value || 0;
  }

  isQuantityInvalid(index: number): boolean {
    const item = this.items.at(index);
    const quantityControl = item.get('quantity');
    return !!quantityControl?.invalid && !!quantityControl?.touched;
  }

  getQuantityErrorMessage(index: number): string {
    const item = this.items.at(index);
    const quantityControl = item.get('quantity');
    const availableStock = this.getAvailableStock(index);

    if (quantityControl?.hasError('required')) {
      return 'Quantidade é obrigatória';
    }
    if (quantityControl?.hasError('min')) {
      return 'Quantidade mínima é 1';
    }
    if (quantityControl?.hasError('max')) {
      return `Estoque disponível: ${availableStock} unidades`;
    }
    return '';
  }

  onSubmit(): void {
    if (this.invoiceForm.invalid) {
      this.invoiceForm.markAllAsTouched();
      this.snackBar.open(
        'Por favor, corrija os erros no formulário',
        'Fechar',
        { duration: 3000 }
      );
      return;
    }

    this.submitting = true;
    const request: CreateInvoiceRequest = {
      items: this.items.value.map((item: any) => ({
        productId: item.productId,
        quantity: item.quantity,
      })),
    };

    this.invoiceService.createInvoice(request).subscribe({
      next: () => {
        this.snackBar.open('Nota fiscal criada com sucesso', 'Fechar', {
          duration: 3000,
        });
        this.router.navigate(['/invoices']);
      },
      error: (error) => {
        console.error('Erro ao criar nota fiscal', error);
        const errorMessage =
          error.error?.errorMessage || 'Erro ao criar nota fiscal';
        this.snackBar.open(errorMessage, 'Fechar', { duration: 5000 });
        this.submitting = false;
      },
    });
  }

  onCancel(): void {
    this.router.navigate(['/invoices']);
  }
}
