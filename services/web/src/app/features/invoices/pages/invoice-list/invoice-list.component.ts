import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { InvoiceService } from '../../services/invoice.service';
import { Invoice, InvoiceStatus } from '../../../../core/models';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatChipsModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './invoice-list.component.html',
  styleUrl: './invoice-list.component.scss',
})
export class InvoiceListComponent implements OnInit {
  invoices: Invoice[] = [];
  displayedColumns: string[] = [
    'invoiceNumber',
    'status',
    'createdAt',
    'printedAt',
    'itemCount',
    'actions',
  ];
  loading = false;
  filterForm: FormGroup;
  InvoiceStatus = InvoiceStatus;

  constructor(
    private fb: FormBuilder,
    private invoiceService: InvoiceService,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {
    this.filterForm = this.fb.group({
      status: [null],
      includeCancelled: [false],
      createdFrom: [null],
      createdTo: [null],
    });
  }

  ngOnInit(): void {
    this.loadInvoices();
    this.filterForm.valueChanges.subscribe(() => this.loadInvoices());
  }

  loadInvoices(): void {
    this.loading = true;
    const filters = this.filterForm.value;

    const request = {
      status: filters.status,
      includeCancelled: filters.includeCancelled,
      createdFrom: filters.createdFrom?.toISOString(),
      createdTo: filters.createdTo?.toISOString(),
    };

    this.invoiceService.getAllInvoices(request).subscribe({
      next: (invoices) => {
        this.invoices = invoices;
        this.loading = false;
      },
      error: (error) => {
        console.error('Erro ao carregar notas fiscais', error);
        const errorMessage =
          error?.error?.errorMessage || 'Erro ao carregar notas fiscais';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000,
        });
        this.loading = false;
      },
    });
  }

  navigateToCreate(): void {
    this.router.navigate(['/invoices/new']);
  }

  printInvoice(invoice: Invoice): void {
    if (invoice.printedAt) {
      this.snackBar.open('Esta nota fiscal já foi impressa', 'Fechar', {
        duration: 3000,
      });
      return;
    }

    if (invoice.cancelled) {
      this.snackBar.open(
        'Não é possível imprimir uma nota fiscal cancelada',
        'Fechar',
        { duration: 3000 }
      );
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Confirmar Impressão',
        message: `Deseja confirmar a impressão da nota fiscal ${invoice.invoiceNumber}? Esta ação não pode ser desfeita.`,
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.invoiceService.printInvoice(invoice.id).subscribe({
          next: () => {
            this.snackBar.open('Nota fiscal impressa com sucesso', 'Fechar', {
              duration: 3000,
            });
            this.loadInvoices();
          },
          error: (error) => {
            console.error('Erro ao imprimir nota fiscal', error);
            const errorMessage =
              error?.error?.errorMessage || 'Erro ao imprimir nota fiscal';
            this.snackBar.open(errorMessage, 'Fechar', {
              duration: 5000,
            });
          },
        });
      }
    });
  }

  deleteInvoice(invoice: Invoice): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Confirmar Cancelamento',
        message: `Deseja cancelar a nota fiscal ${invoice.invoiceNumber}? Esta ação cancelará as reservas de estoque associadas.`,
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.invoiceService.deleteInvoice(invoice.id).subscribe({
          next: () => {
            this.snackBar.open('Nota fiscal cancelada com sucesso', 'Fechar', {
              duration: 3000,
            });
            this.loadInvoices();
          },
          error: (error) => {
            console.error('Erro ao cancelar nota fiscal', error);
            const errorMessage =
              error?.error?.errorMessage || 'Erro ao cancelar nota fiscal';
            this.snackBar.open(errorMessage, 'Fechar', {
              duration: 5000,
            });
          },
        });
      }
    });
  }

  clearFilters(): void {
    this.filterForm.reset({
      status: null,
      includeCancelled: false,
      createdFrom: null,
      createdTo: null,
    });
  }

  getStatusLabel(status: InvoiceStatus): string {
    return status === InvoiceStatus.Open ? 'Aberta' : 'Fechada';
  }

  formatDate(date: Date | string): string {
    return new Date(date).toLocaleString('pt-BR');
  }
}
