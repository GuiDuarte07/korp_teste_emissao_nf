import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { InventoryService } from '../../services/inventory.service';
import {
  CreateProductRequest,
  UpdateProductRequest,
} from '../../../../core/models';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
})
export class ProductFormComponent implements OnInit {
  productForm: FormGroup;
  isEditMode = false;
  productId?: string;
  loading = false;
  submitting = false;

  constructor(
    private fb: FormBuilder,
    private inventoryService: InventoryService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.productForm = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(50)]],
      description: ['', [Validators.required, Validators.maxLength(200)]],
      stock: [0, [Validators.required, Validators.min(0)]],
    });
  }

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id') || undefined;
    this.isEditMode = !!this.productId;

    if (this.isEditMode && this.productId) {
      this.loadProduct(this.productId);
    }
  }

  loadProduct(id: string): void {
    this.loading = true;
    this.inventoryService.getProductById(id).subscribe({
      next: (product) => {
        this.productForm.patchValue({
          code: product.code,
          description: product.description,
          stock: product.stock,
        });
        this.loading = false;
      },
      error: (error) => {
        console.error('Erro ao carregar produto', error);
        const errorMessage =
          error?.error?.errorMessage || 'Erro ao carregar produto';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000,
        });
        this.router.navigate(['/products']);
        this.loading = false;
      },
    });
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const formValue = this.productForm.value;

    if (this.isEditMode && this.productId) {
      const request: UpdateProductRequest = {
        id: this.productId,
        ...formValue,
      };

      this.inventoryService.updateProduct(request).subscribe({
        next: () => {
          this.snackBar.open('Produto atualizado com sucesso', 'Fechar', {
            duration: 3000,
          });
          this.router.navigate(['/products']);
        },
        error: (error) => {
          console.error('Erro ao atualizar produto', error);
          const errorMessage =
            error?.error?.errorMessage || 'Erro ao atualizar produto';
          this.snackBar.open(errorMessage, 'Fechar', {
            duration: 5000,
          });
          this.submitting = false;
        },
      });
    } else {
      const request: CreateProductRequest = formValue;

      this.inventoryService.createProduct(request).subscribe({
        next: () => {
          this.snackBar.open('Produto criado com sucesso', 'Fechar', {
            duration: 3000,
          });
          this.router.navigate(['/products']);
        },
        error: (error) => {
          console.error('Erro ao criar produto', error);
          const errorMessage =
            error?.error?.errorMessage || 'Erro ao criar produto';
          this.snackBar.open(errorMessage, 'Fechar', {
            duration: 5000,
          });
          this.submitting = false;
        },
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/products']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.productForm.get(fieldName);
    if (field?.hasError('required')) {
      return 'Campo obrigatório';
    }
    if (field?.hasError('min')) {
      return 'Valor deve ser maior ou igual a 0';
    }
    if (field?.hasError('maxLength')) {
      return 'Tamanho máximo excedido';
    }
    return '';
  }
}
