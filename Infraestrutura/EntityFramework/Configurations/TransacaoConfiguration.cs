using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestrutura.EntityFramework.Configurations
{
    public class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
    {
        public void Configure(EntityTypeBuilder<Transacao> builder)
        {
            builder.ToTable("Transacoes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DataCriacao)
                   .IsRequired();

            builder.Property(x => x.Tipo)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(x => x.Quantia)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.Moeda)
                   .IsRequired()
                   .HasMaxLength(10);

            builder.Property(x => x.Status)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(x => x.MetadataJson)
                   .HasColumnType("nvarchar(max)").IsRequired(false);

            builder.Property(x => x.MensagemErro)
                   .HasMaxLength(2000).IsRequired(false);

            builder.HasOne(x => x.Conta)
                   .WithMany(x => x.Transacoes)
                   .HasForeignKey(x => x.ContaId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ContaId);
        }
    }
}
