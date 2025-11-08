import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { InventoryService } from '../../services/inventory.service';
import { Product } from '../../../../core/models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss',
})
export class ProductListComponent implements OnInit {
  products: Product[] = [];
  displayedColumns: string[] = [
    'code',
    'description',
    'stock',
    'reservedStock',
    'availableStock',
    'actions',
  ];
  loading = false;

  constructor(
    private inventoryService: InventoryService,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.inventoryService.getAllProducts().subscribe({
      next: (products) => {
        this.products = products;
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

  navigateToCreate(): void {
    this.router.navigate(['/products/new']);
  }

  navigateToEdit(product: Product): void {
    this.router.navigate(['/products/edit', product.id]);
  }

  deleteProduct(product: Product): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Confirmar Exclusão',
        message: `Tem certeza que deseja excluir o produto "${product.description}"?`,
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.inventoryService.deleteProduct(product.id).subscribe({
          next: () => {
            this.snackBar.open('Produto excluído com sucesso', 'Fechar', {
              duration: 3000,
            });
            this.loadProducts();
          },
          error: (error) => {
            console.error('Erro ao excluir produto', error);
            const errorMessage =
              error?.error?.errorMessage || 'Erro ao excluir produto';
            this.snackBar.open(errorMessage, 'Fechar', {
              duration: 5000,
            });
          },
        });
      }
    });
  }
}
