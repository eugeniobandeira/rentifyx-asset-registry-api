using Microsoft.EntityFrameworkCore;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Infrastructure.Context.Configurations;

namespace RentifyxAssetRegistry.Infrastructure.Context;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ExampleEntity> Examples => Set<ExampleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ExampleConfiguration());
    }
}
