using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestrutura.EntityFramework.Configurations
{
    public class ContaConfiguration : IEntityTypeConfiguration<Conta>
    {
        public void Configure(EntityTypeBuilder<Conta> builder)
        {
            builder.ToTable("Contas");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DataCriacao)
                   .IsRequired();


            builder.Property(x => x.SaldoDisponivel)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.SaldoReservado)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.LimiteDeCredito)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(x => x.DataAtualizacao)
                   .IsRequired();

            builder.Property(x => x.RowVersion)
                   .IsRowVersion()
                   .IsConcurrencyToken();

            builder.HasOne(x => x.Cliente)
                   .WithMany(x => x.Contas)
                   .HasForeignKey(x => x.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Transacoes)
                   .WithOne(x => x.Conta)
                   .HasForeignKey(x => x.ContaId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
