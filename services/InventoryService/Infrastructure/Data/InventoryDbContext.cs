using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;

namespace InventoryService.Infrastructure.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<StockReservationItem> StockReservationItems => Set<StockReservationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da entidade Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.HasIndex(p => p.Code)
                .IsUnique();
            
            entity.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(150);
            
            entity.Property(p => p.Stock)
                .IsRequired();
            
            entity.Property(p => p.CreatedAt)
                .IsRequired();
        });

        // Configuração da entidade StockReservation
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("stock_reservations");
            entity.HasKey(r => r.Id);
            
            entity.Property(r => r.InvoiceId)
                .IsRequired();
            
            entity.HasIndex(r => r.InvoiceId);
            
            entity.Property(r => r.CreatedAt)
                .IsRequired();
            
            entity.Property(r => r.Confirmed)
                .IsRequired();
            
            entity.Property(r => r.Cancelled)
                .IsRequired();
        });

        // Configuração da entidade StockReservationItem
        modelBuilder.Entity<StockReservationItem>(entity =>
        {
            entity.ToTable("stock_reservation_items");
            entity.HasKey(i => i.Id);
            
            entity.Property(i => i.Quantity)
                .IsRequired();
            
            // Relacionamento com StockReservation
            entity.HasOne(i => i.Reservation)
                .WithMany(r => r.Items)
                .HasForeignKey(i => i.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relacionamento com Product
            entity.HasOne(i => i.Product)
                .WithMany(p => p.ReservationItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
