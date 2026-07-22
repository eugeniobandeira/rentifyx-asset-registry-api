using rentifyx_asset_registry_api.Domain.Constants;
using rentifyx_asset_registry_api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace rentifyx_asset_registry_api.Infrastructure.Context.Configurations;

internal sealed class ExampleConfiguration : IEntityTypeConfiguration<ExampleEntity>
{
    public void Configure(EntityTypeBuilder<ExampleEntity> builder)
    {
        builder.ToTable("Examples");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(ValidationConstants.ExampleRules.NameMaxLength);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(ValidationConstants.ExampleRules.DescriptionMaxLength);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);
    }
}
