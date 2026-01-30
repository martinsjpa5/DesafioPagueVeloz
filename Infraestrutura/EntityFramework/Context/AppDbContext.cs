
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura.EntityFramework.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<Conta> Contas { get; set; }
        public DbSet<Transacao> Transacoes { get; set; }
        public DbSet<Cliente> Clientes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override int SaveChanges()
        {
            AplicarAuditoria();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AplicarAuditoria();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AplicarAuditoria()
        {
            var agoraUtc = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<EntidadeBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DataCriacao = agoraUtc;
                    entry.Entity.DataAtualizacao = agoraUtc;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(x => x.DataCriacao).IsModified = false;

                    entry.Entity.DataAtualizacao = agoraUtc;
                }
            }
        }
    }
}
