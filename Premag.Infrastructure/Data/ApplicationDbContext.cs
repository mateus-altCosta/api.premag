using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Premag.Core;
using Premag.Core.Entities;
using Premag.Core.Interfaces;

namespace Premag.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public Guid TenantId => _tenant.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Users => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<InscricaoPush> InscricoesPush => Set<InscricaoPush>();
    public DbSet<Equipe> Equipes => Set<Equipe>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<Frente> Frentes => Set<Frente>();
    public DbSet<MotivoParada> MotivosParada => Set<MotivoParada>();
    public DbSet<JornadaDia> JornadasDia => Set<JornadaDia>();
    public DbSet<Apontamento> Apontamentos => Set<Apontamento>();
    public DbSet<Producao> Producoes => Set<Producao>();
    public DbSet<Foto> Fotos => Set<Foto>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FechamentoDia> FechamentosDia => Set<FechamentoDia>();
    public DbSet<LoteSincronizacao> LotesSincronizacao => Set<LoteSincronizacao>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AplicarIds();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AplicarIds();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var id = entity.FindProperty("Id");
            if (id?.ClrType == typeof(Guid))
            {
                id.ValueGenerated = ValueGenerated.OnAdd;
                id.SetValueGeneratorFactory((_, _) => new GuidVersion7ValueGenerator());
            }
        }

        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("tenants");
            b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(40).IsRequired();
            b.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("usuarios");
            b.Property(x => x.UserName).HasMaxLength(64).IsRequired();
            b.Property(x => x.Email).HasMaxLength(160);
            b.Property(x => x.NomeExibicao).HasMaxLength(120).IsRequired();
            b.Property(x => x.SenhaHash).HasMaxLength(200).IsRequired();
            b.Property(x => x.Perfil).HasMaxLength(40).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.UserName }).IsUnique();
            b.HasOne(x => x.Equipe).WithMany().HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.Property(x => x.Token).HasMaxLength(200).IsRequired();
            b.Property(x => x.DispositivoId).HasMaxLength(64);
            b.HasIndex(x => x.Token).IsUnique();
            b.HasIndex(x => x.TenantId);
            b.HasOne(x => x.Usuario).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<Configuracao>(b =>
        {
            b.ToTable("configuracoes");
            b.HasIndex(x => x.TenantId).IsUnique();
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<InscricaoPush>(b =>
        {
            b.ToTable("inscricoes_push");
            b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<Equipe>(b =>
        {
            b.ToTable("equipes");
            b.Property(x => x.Nome).HasMaxLength(60).IsRequired();
            b.Property(x => x.Cor).HasMaxLength(7);
            b.HasIndex(x => new { x.TenantId, x.Nome }).IsUnique();
            b.HasOne(x => x.Encarregado).WithMany().HasForeignKey(x => x.EncarregadoId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<Colaborador>(b =>
        {
            b.ToTable("colaboradores");
            b.Property(x => x.Matricula).HasMaxLength(20).IsRequired();
            b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            b.Property(x => x.Funcao).HasMaxLength(60).IsRequired();
            b.Property(x => x.CustoHora).HasPrecision(10, 2);
            b.Property(x => x.CodigoExterno).HasMaxLength(40);
            b.HasIndex(x => new { x.TenantId, x.Matricula }).IsUnique();
            b.HasOne(x => x.Equipe).WithMany(e => e.Colaboradores).HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<Obra>(b =>
        {
            b.ToTable("obras");
            b.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            b.Property(x => x.Cliente).HasMaxLength(160).IsRequired();
            b.Property(x => x.Tipo).HasMaxLength(80);
            b.Property(x => x.Local).HasMaxLength(160);
            b.Property(x => x.CodigoSienge).HasMaxLength(40);
            b.Property(x => x.CentroCusto).HasMaxLength(40);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<Etapa>(b =>
        {
            b.ToTable("etapas");
            b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Nome }).IsUnique();
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<Frente>(b =>
        {
            b.ToTable("frentes");
            b.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            b.Property(x => x.Unidade).HasMaxLength(10).IsRequired();
            b.Property(x => x.QuantidadePrevista).HasPrecision(14, 3);
            b.Property(x => x.QuantidadeConcluida).HasPrecision(14, 3);
            b.Property(x => x.TaxaAcoKgPorUnidade).HasPrecision(12, 3);
            b.Property(x => x.HhOrcadoPorUnidade).HasPrecision(12, 4);
            b.Property(x => x.ItemOrcamentoSienge).HasMaxLength(40);
            b.Property(x => x.Cor).HasMaxLength(7);
            b.HasIndex(x => new { x.TenantId, x.ObraId, x.Nome }).IsUnique();
            b.HasOne(x => x.Obra).WithMany(o => o.Frentes).HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Etapa).WithMany().HasForeignKey(x => x.EtapaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Equipe).WithMany().HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<MotivoParada>(b =>
        {
            b.ToTable("motivos_parada");
            b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Nome }).IsUnique();
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<JornadaDia>(b =>
        {
            b.ToTable("jornadas_dia");
            b.HasIndex(x => new { x.TenantId, x.ColaboradorId, x.Data }).IsUnique();
            b.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<Apontamento>(b =>
        {
            b.ToTable("apontamentos");
            b.Property(x => x.Observacao).HasMaxLength(300);
            b.Property(x => x.DispositivoId).HasMaxLength(64);
            b.HasIndex(x => new { x.TenantId, x.ClienteUuid }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.ColaboradorId, x.Data });
            b.HasIndex(x => new { x.TenantId, x.FrenteId, x.Data });
            b.HasIndex(x => new { x.TenantId, x.ColaboradorId })
                .IsUnique()
                .HasFilter("hora_fim IS NULL AND excluido = FALSE")
                .HasDatabaseName("ix_apontamentos_aberto_por_colaborador");
            b.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Frente).WithMany().HasForeignKey(x => x.FrenteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.MotivoParada).WithMany().HasForeignKey(x => x.MotivoParadaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.CriadoPor).WithMany().HasForeignKey(x => x.CriadoPorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.AlteradoPor).WithMany().HasForeignKey(x => x.AlteradoPorId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId && !x.Excluido);
        });

        modelBuilder.Entity<Producao>(b =>
        {
            b.ToTable("producoes");
            b.Property(x => x.Quantidade).HasPrecision(14, 3);
            b.HasIndex(x => new { x.TenantId, x.ClienteUuid }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.FrenteId, x.Data });
            b.HasOne(x => x.Frente).WithMany().HasForeignKey(x => x.FrenteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Foto).WithMany().HasForeignKey(x => x.FotoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Apontamento).WithMany().HasForeignKey(x => x.ApontamentoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.RegistradoPor).WithMany().HasForeignKey(x => x.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId && !x.Excluido);
        });

        modelBuilder.Entity<Foto>(b =>
        {
            b.ToTable("fotos");
            b.Property(x => x.Observacao).HasMaxLength(300);
            b.Property(x => x.ObjectKey).HasMaxLength(200).IsRequired();
            b.Property(x => x.ThumbKey).HasMaxLength(200).IsRequired();
            b.Property(x => x.HashSha256).HasMaxLength(64).IsRequired();
            b.Property(x => x.Quantidade).HasPrecision(14, 3);
            b.Property(x => x.Latitude).HasPrecision(9, 6);
            b.Property(x => x.Longitude).HasPrecision(9, 6);
            b.HasIndex(x => new { x.TenantId, x.ClienteUuid }).IsUnique();
            b.HasOne(x => x.Frente).WithMany().HasForeignKey(x => x.FrenteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Apontamento).WithMany().HasForeignKey(x => x.ApontamentoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.EnviadaPor).WithMany().HasForeignKey(x => x.EnviadaPorId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId && !x.Excluido);
        });

        modelBuilder.Entity<Ocorrencia>(b =>
        {
            b.ToTable("ocorrencias");
            b.Property(x => x.Justificativa).HasMaxLength(300);
            b.HasIndex(x => new { x.TenantId, x.Tipo, x.ColaboradorId, x.Data, x.JanelaInicio })
                .IsUnique()
                .HasDatabaseName("ix_ocorrencias_dedup");
            b.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Frente).WithMany().HasForeignKey(x => x.FrenteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Equipe).WithMany().HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReconhecidaPor).WithMany().HasForeignKey(x => x.ReconhecidaPorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ApontamentoGerado).WithMany().HasForeignKey(x => x.ApontamentoGeradoId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.Property(x => x.Entidade).HasMaxLength(60).IsRequired();
            b.Property(x => x.Acao).HasMaxLength(20).IsRequired();
            b.Property(x => x.Antes).HasColumnType("jsonb");
            b.Property(x => x.Depois).HasColumnType("jsonb");
            b.Property(x => x.Ip).HasMaxLength(45);
            b.HasIndex(x => new { x.TenantId, x.Entidade, x.EntidadeId });
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<FechamentoDia>(b =>
        {
            b.ToTable("fechamentos_dia");
            b.Property(x => x.MotivoReabertura).HasMaxLength(300);
            b.HasIndex(x => new { x.TenantId, x.Data, x.EquipeId }).IsUnique();
            b.HasOne(x => x.Equipe).WithMany().HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.FechadoPor).WithMany().HasForeignKey(x => x.FechadoPorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ReabertoPor).WithMany().HasForeignKey(x => x.ReabertoPorId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });

        modelBuilder.Entity<LoteSincronizacao>(b =>
        {
            b.ToTable("lotes_sincronizacao");
            b.Property(x => x.DispositivoId).HasMaxLength(64).IsRequired();
            b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => x.TenantId == TenantId);
        });
    }

    private void AplicarIds()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;
            var prop = entry.Metadata.FindProperty("Id");
            if (prop?.ClrType != typeof(Guid)) continue;
            var atual = (Guid?)entry.Property("Id").CurrentValue;
            if (atual is null || atual == Guid.Empty)
                entry.Property("Id").CurrentValue = GeradorId.Novo();
        }
    }
}
