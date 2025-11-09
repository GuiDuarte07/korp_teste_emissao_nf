using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InvoiceService.Domain.Entities;

namespace InvoiceService.Services;

public class InvoicePdfGenerator
{
    public byte[] GenerateInvoicePdf(Invoice invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text($"NOTA FISCAL Nº {invoice.InvoiceNumber}")
                    .SemiBold()
                    .FontSize(20)
                    .FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Informações da Nota Fiscal
                        column.Item().Element(ComposeInvoiceInfo);

                        // Tabela de Itens
                        column.Item().Element(ComposeTable);

                        // Rodapé com totais
                        column.Item().Element(ComposeFooter);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Emitido em: ");
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold();
                    });
            });
        });

        return document.GeneratePdf();

        void ComposeInvoiceInfo(IContainer container)
        {
            container.Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
            {
                column.Spacing(5);
                column.Item().Text($"Número da Nota: {invoice.InvoiceNumber}").FontSize(12).SemiBold();
                column.Item().Text($"Status: {GetStatusText(invoice)}").FontSize(10);
                column.Item().Text($"Data de Criação: {invoice.CreatedAt:dd/MM/yyyy HH:mm}").FontSize(10);
                
                if (invoice.PrintedAt.HasValue)
                {
                    column.Item().Text($"Data de Impressão: {invoice.PrintedAt:dd/MM/yyyy HH:mm}").FontSize(10);
                }

                if (invoice.Cancelled)
                {
                    column.Item().Text($"Data de Cancelamento: {invoice.CancelledAt:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Red.Medium);
                }
            });
        }

        void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                // Definir colunas
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);  // Item
                    columns.RelativeColumn(2);   // Código
                    columns.RelativeColumn(4);   // Descrição
                    columns.ConstantColumn(80);  // Quantidade
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Item").SemiBold();
                    header.Cell().Element(CellStyle).Text("Código").SemiBold();
                    header.Cell().Element(CellStyle).Text("Descrição").SemiBold();
                    header.Cell().Element(CellStyle).Text("Quantidade").SemiBold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold())
                            .PaddingVertical(5)
                            .BorderBottom(1)
                            .BorderColor(Colors.Black);
                    }
                });

                // Rows
                int itemNumber = 1;
                foreach (var item in invoice.Items)
                {
                    table.Cell().Element(CellStyle).Text(itemNumber.ToString());
                    table.Cell().Element(CellStyle).Text(item.ProductCode);
                    table.Cell().Element(CellStyle).Text(item.ProductDescription);
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                    itemNumber++;
                }

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(5);
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignRight().Column(column =>
            {
                column.Spacing(5);
                column.Item().Text($"Total de Itens: {invoice.Items.Count}").FontSize(12).SemiBold();
                column.Item().Text($"Quantidade Total: {invoice.Items.Sum(i => i.Quantity)}").FontSize(12).SemiBold();
            });
        }
    }

    private string GetStatusText(Invoice invoice)
    {
        if (invoice.Cancelled)
            return "CANCELADA";
        
        return invoice.Status == InvoiceStatus.Open ? "ABERTA" : "FECHADA";
    }
}
