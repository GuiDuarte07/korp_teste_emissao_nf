import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { InvoiceService } from '../../services/invoice.service';
import { Invoice } from '../../../../core/models';
import { LoadingService } from '../../../../core/services/loading.service';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatChipsModule,
  ],
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.scss',
})
export class InvoiceDetailComponent implements OnInit {
  invoice?: Invoice;
  loading = false;
  displayedColumns: string[] = ['item', 'code', 'description', 'quantity'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private invoiceService: InvoiceService,
    private snackBar: MatSnackBar,
    private loadingService: LoadingService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadInvoice(id);
    }
  }

  loadInvoice(id: string): void {
    this.loading = true;
    this.invoiceService.getInvoiceById(id).subscribe({
      next: (invoice: Invoice) => {
        this.invoice = invoice;
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Erro ao carregar nota fiscal', error);
        const errorMessage =
          error?.error?.errorMessage || 'Erro ao carregar nota fiscal';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000,
        });
        this.router.navigate(['/invoices']);
        this.loading = false;
      },
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Cancelled':
        return 'cancelled-chip';
      case 'Open':
        return 'open-chip';
      case 'Closed':
        return 'closed-chip';
      default:
        return '';
    }
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'Cancelled':
        return 'cancel';
      case 'Open':
        return 'schedule';
      case 'Closed':
        return 'check_circle';
      default:
        return 'help';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Cancelled':
        return 'Cancelada';
      case 'Open':
        return 'Aberta';
      case 'Closed':
        return 'Fechada';
      default:
        return status;
    }
  }

  onBack(): void {
    this.router.navigate(['/invoices']);
  }

  onPrint(): void {
    if (!this.invoice) return;

    this.loadingService.show('Imprimindo nota fiscal...');
    this.invoiceService.printInvoice(this.invoice.id).subscribe({
      next: () => {
        this.snackBar.open('Nota fiscal impressa com sucesso', 'Fechar', {
          duration: 3000,
        });
        this.loadingService.hide();
        this.loadInvoice(this.invoice!.id);
      },
      error: (error: any) => {
        console.error('Erro ao imprimir nota fiscal', error);
        const errorMessage =
          error?.error?.errorMessage || 'Erro ao imprimir nota fiscal';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000,
        });
        this.loadingService.hide();
      },
    });
  }

  onDownloadPdf(): void {
    if (!this.invoice) return;

    this.loadingService.show('Gerando PDF da nota fiscal...');
    this.invoiceService.downloadInvoicePdf(this.invoice.id).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `NotaFiscal_${this.invoice!.invoiceNumber}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.snackBar.open('PDF baixado com sucesso', 'Fechar', {
          duration: 3000,
        });
        this.loadingService.hide();
      },
      error: (error: any) => {
        console.error('Erro ao baixar PDF', error);
        const errorMessage =
          error?.error?.errorMessage || 'Erro ao baixar PDF da nota fiscal';
        this.snackBar.open(errorMessage, 'Fechar', {
          duration: 5000,
        });
        this.loadingService.hide();
      },
    });
  }
}
