using InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Infrastructure.Data;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da entidade Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.InvoiceNumber)
                .HasColumnName("invoice_number")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.PrintedAt)
                .HasColumnName("printed_at");

            // Índice único para numeração sequencial
            entity.HasIndex(e => e.InvoiceNumber)
                .IsUnique();

            // Relacionamento com InvoiceItems
            entity.HasMany(e => e.Items)
                .WithOne(i => i.Invoice)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração da entidade InvoiceItem
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("invoice_items");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.InvoiceId)
                .HasColumnName("invoice_id")
                .IsRequired();

            entity.Property(e => e.ProductId)
                .HasColumnName("product_id")
                .IsRequired();

            entity.Property(e => e.ProductCode)
                .HasColumnName("product_code")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ProductDescription)
                .HasColumnName("product_description")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            entity.Property(e => e.ReservationId)
                .HasColumnName("reservation_id");
        });
    }
}
