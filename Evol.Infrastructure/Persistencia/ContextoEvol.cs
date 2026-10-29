using Evol.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Evol.Infrastructure.Persistencia;

public sealed class ContextoEvol(DbContextOptions<ContextoEvol> options) : DbContext(options)
{
    public DbSet<ServicioReparacion> ServiciosReparacion => Set<ServicioReparacion>();
    public DbSet<ModeloDispositivo> ModelosDispositivo => Set<ModeloDispositivo>();
    public DbSet<PrecioReparacionModelo> PreciosReparacionModelo => Set<PrecioReparacionModelo>();
    public DbSet<CotizacionReparacion> Cotizaciones => Set<CotizacionReparacion>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<BloqueHorario> BloqueosHorario => Set<BloqueHorario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServicioReparacion>(entity =>
        {
            entity.ToTable("repair_services");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
        });
        modelBuilder.Entity<ModeloDispositivo>(entity =>
        {
            entity.ToTable("device_models");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Brand).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => new { x.Brand, x.Model }).IsUnique();
            entity.HasMany(x => x.RepairPrices).WithOne(x => x.ModeloDispositivo).HasForeignKey(x => x.DeviceModelId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PrecioReparacionModelo>(entity =>
        {
            entity.ToTable("model_repair_prices");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DeviceModelId, x.RepairCode }).IsUnique();
            entity.Property(x => x.RepairCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Price).HasPrecision(12, 2);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.VerifiedOn).HasMaxLength(20);
            entity.HasOne(x => x.ServicioReparacion).WithMany().HasForeignKey(x => x.RepairCode)
                .HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CotizacionReparacion>(entity =>
        {
            entity.ToTable("quotes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerMessage).HasMaxLength(6000);
            entity.Property(x => x.MinPrice).HasPrecision(12, 2);
            entity.Property(x => x.MaxPrice).HasPrecision(12, 2);
            entity.HasMany(x => x.Appointments).WithOne(x => x.Quote).HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CustomerPhone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Brand).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Issue).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.ScheduledAt);
        });
        modelBuilder.Entity<BloqueHorario>(entity =>
        {
            entity.ToTable("availability_blocks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(240).IsRequired();
            entity.HasIndex(x => x.StartsAt);
        });
    }
}




