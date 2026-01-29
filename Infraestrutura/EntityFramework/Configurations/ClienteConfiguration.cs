
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestrutura.EntityFramework.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DataCriacao)
                   .IsRequired();

            builder.HasMany(x => x.Contas)
                   .WithOne(x => x.Cliente)
                   .HasForeignKey(x => x.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);


        }
    }
}
