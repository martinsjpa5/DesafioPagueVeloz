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

            builder.Property(x => x.DataCriacao).IsRequired();

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
                   .HasColumnType("nvarchar(max)")
                   .IsRequired(false);

            builder.Property(x => x.MensagemErro)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            // ContaOrigem (sem navegação inversa pra evitar ambiguidade)
            builder.HasOne(x => x.ContaOrigem)
                   .WithMany()
                   .HasForeignKey(x => x.ContaOrigemId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ContaOrigemId);

            // ContaDestino (opcional)
            builder.HasOne(x => x.ContaDestino)
                   .WithMany()
                   .HasForeignKey(x => x.ContaDestinoId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.HasIndex(x => x.ContaDestinoId);

            // TransacaoRevertida (opcional) - FK correta
            builder.HasOne(x => x.TransacaoRevertida)
                   .WithMany()
                   .HasForeignKey(x => x.TransacaoRevertidaId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.HasIndex(x => x.TransacaoRevertidaId);
        }

    }
}
