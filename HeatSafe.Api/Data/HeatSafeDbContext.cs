using HeatSafe.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Data;

public sealed class HeatSafeDbContext(DbContextOptions<HeatSafeDbContext> options) : DbContext(options)
{
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<Idoso> Idosos => Set<Idoso>();
    public DbSet<Familiar> Familiares => Set<Familiar>();
    public DbSet<AgenteSaude> AgentesSaude => Set<AgenteSaude>();
    public DbSet<FamiliarIdoso> FamiliaresIdosos => Set<FamiliarIdoso>();
    public DbSet<Residencia> Residencias => Set<Residencia>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<Leitura> Leituras => Set<Leitura>();
    public DbSet<RegistroHidratacao> RegistrosHidratacao => Set<RegistroHidratacao>();
    public DbSet<EventoDispositivo> EventosDispositivo => Set<EventoDispositivo>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<Visita> Visitas => Set<Visita>();
    public DbSet<PrevisaoClimatica> PrevisoesClimaticas => Set<PrevisaoClimatica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
    }

    // Mantém a configuração do modelo em um único ponto para que o snapshot inicial
    // represente a mesma estrutura usada em runtime. Em novas alterações de domínio,
    // gere uma nova migration e atualize o snapshot normalmente.
    internal static void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pessoa>(entity =>
        {
            entity.ToTable("Pessoas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.DataCadastroUtc).IsRequired();
            entity.Property(x => x.Ativo).IsRequired();
            entity.HasDiscriminator<string>("TipoPessoa")
                .HasValue<Idoso>("IDOSO")
                .HasValue<Familiar>("FAMILIAR")
                .HasValue<AgenteSaude>("AGENTE_SAUDE");
        });

        modelBuilder.Entity<Idoso>(entity =>
        {
            entity.Property(x => x.Idade).IsRequired();
            entity.Property(x => x.PossuiVulnerabilidade).IsRequired();
            entity.Property(x => x.DescricaoVulnerabilidade).HasMaxLength(500);
        });
        modelBuilder.Entity<Familiar>(entity => entity.Property(x => x.ContatoMascarado).HasMaxLength(80));
        modelBuilder.Entity<AgenteSaude>(entity => entity.Property(x => x.UnidadeSaude).HasMaxLength(160).IsRequired());

        modelBuilder.Entity<FamiliarIdoso>(entity =>
        {
            entity.ToTable("FamiliaresIdosos");
            entity.HasKey(x => new { x.FamiliarId, x.IdosoId });
            entity.HasOne(x => x.Familiar).WithMany(x => x.IdososVinculados).HasForeignKey(x => x.FamiliarId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Idoso).WithMany(x => x.FamiliaresVinculados).HasForeignKey(x => x.IdosoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Residencia>(entity =>
        {
            entity.ToTable("Residencias");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Cidade).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Estado).HasMaxLength(2).IsRequired();
            entity.Property(x => x.IdentificacaoEndereco).HasMaxLength(180).IsRequired();
            entity.HasOne(x => x.Idoso).WithOne(x => x.Residencia).HasForeignKey<Residencia>(x => x.IdosoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.IdosoId).IsUnique();
        });

        modelBuilder.Entity<Dispositivo>(entity =>
        {
            entity.ToTable("Dispositivos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.HasIndex(x => x.ResidenciaId).IsUnique();
            entity.HasOne(x => x.Residencia).WithOne(x => x.Dispositivo).HasForeignKey<Dispositivo>(x => x.ResidenciaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Leitura>(entity =>
        {
            entity.ToTable("Leituras");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TemperaturaC).IsRequired();
            entity.Property(x => x.Umidade).IsRequired();
            entity.Property(x => x.IndiceCalorCalculadoC).IsRequired();
            entity.Property(x => x.NivelInformado).HasMaxLength(30);
            entity.Property(x => x.NivelRisco).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.DispositivoId, x.DataHoraUtc });
            entity.HasOne(x => x.Dispositivo).WithMany(x => x.Leituras).HasForeignKey(x => x.DispositivoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistroHidratacao>(entity =>
        {
            entity.ToTable("RegistrosHidratacao");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Idoso).WithMany(x => x.Hidratacoes).HasForeignKey(x => x.IdosoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Dispositivo).WithMany().HasForeignKey(x => x.DispositivoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.IdosoId, x.DataHoraUtc });
        });

        modelBuilder.Entity<EventoDispositivo>(entity =>
        {
            entity.ToTable("EventosDispositivo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ChaveUnica).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.ChaveUnica).IsUnique();
            entity.HasOne(x => x.Dispositivo).WithMany(x => x.Eventos).HasForeignKey(x => x.DispositivoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.ToTable("Alertas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NivelRisco).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Motivo).HasMaxLength(700).IsRequired();
            entity.HasOne(x => x.Idoso).WithMany(x => x.Alertas).HasForeignKey(x => x.IdosoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Residencia).WithMany().HasForeignKey(x => x.ResidenciaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FamiliarConfirmacao).WithMany(x => x.AlertasConfirmados).HasForeignKey(x => x.FamiliarConfirmacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.IdosoId, x.Status, x.Tipo });
        });

        modelBuilder.Entity<Visita>(entity =>
        {
            entity.ToTable("Visitas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.MotivoPrioridade).HasMaxLength(700).IsRequired();
            entity.HasIndex(x => x.AlertaId).IsUnique();
            entity.HasOne(x => x.Idoso).WithMany(x => x.Visitas).HasForeignKey(x => x.IdosoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Residencia).WithMany().HasForeignKey(x => x.ResidenciaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Alerta).WithMany(x => x.Visitas).HasForeignKey(x => x.AlertaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AgenteSaude).WithMany(x => x.Visitas).HasForeignKey(x => x.AgenteSaudeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrevisaoClimatica>(entity =>
        {
            entity.ToTable("PrevisoesClimaticas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Estacao).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.Estacao, x.DataPrevista }).IsUnique();
        });
    }
}
