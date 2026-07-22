using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Infrastructure.Context.Configurations;
using Microsoft.EntityFrameworkCore;

namespace rentifyx_asset_registry_api.Infrastructure.Context;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ExampleEntity> Examples => Set<ExampleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ExampleConfiguration());
    }
}
