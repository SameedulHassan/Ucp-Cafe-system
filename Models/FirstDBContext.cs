using Microsoft.EntityFrameworkCore;

namespace UCPFoodCorner.Models;

public class FirstDBContext : DbContext
{
    public FirstDBContext(DbContextOptions<FirstDBContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CafeItem> CafeItems => Set<CafeItem>();
    public DbSet<ItemAvailability> ItemAvailabilities => Set<ItemAvailability>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealItem> DealItems => Set<DealItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<CafeItem>().ToTable("CafeItems");
        modelBuilder.Entity<ItemAvailability>().ToTable("ItemAvailabilities");
        modelBuilder.Entity<Review>().ToTable("Reviews");
        modelBuilder.Entity<Order>().ToTable("Orders");
        modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
        modelBuilder.Entity<Deal>().ToTable("Deals");
        modelBuilder.Entity<DealItem>().ToTable("DealItems");

        modelBuilder.Entity<CafeItem>().Property(x => x.Price).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPrice).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<Review>().Property(x => x.Rating).HasDefaultValue(5);
        modelBuilder.Entity<Deal>().Property(x => x.DealPrice).HasColumnType("decimal(10,2)");
    }
}