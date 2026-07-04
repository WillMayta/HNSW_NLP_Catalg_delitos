using Microsoft.EntityFrameworkCore;
using MinisterioPublico.Domain.Entities;


namespace MinisterioPublico.Infrastructure.Persistence;

/// <summary>
/// Contexto de Entity Framework Core. Proveedor: Npgsql (PostgreSQL).
/// Para producción, instalar el paquete `Pgvector.EntityFrameworkCore`
/// adicionalmente y mapear Embedding.Vector como tipo `vector(384)` para
/// aprovechar el índice HNSW nativo de la extensión pgvector:
///
///   modelBuilder.Entity&lt;Embedding&gt;()
///       .Property(e => e.Vector)
///       .HasColumnType("vector(384)");
///
///   // Migración SQL adicional:
///   CREATE EXTENSION IF NOT EXISTS vector;
///   CREATE INDEX idx_embeddings_hnsw ON embeddings
///       USING hnsw (vector vector_cosine_ops);
/// </summary>
public class MinisterioPublicoDbContext : DbContext
{
    public MinisterioPublicoDbContext(DbContextOptions<MinisterioPublicoDbContext> options)
        : base(options) { }

    public DbSet<DelitoOriginal> DelitosOriginales => Set<DelitoOriginal>();
    public DbSet<DelitoNormalizado> DelitosNormalizados => Set<DelitoNormalizado>();
    public DbSet<Embedding> Embeddings => Set<Embedding>();
    public DbSet<FamiliaDelictiva> FamiliasDelictivas => Set<FamiliaDelictiva>();
    public DbSet<DelitoCatalogo> DelitosCatalogo => Set<DelitoCatalogo>();
    public DbSet<Variante> Variantes => Set<Variante>();
    public DbSet<PropuestaAgrupamiento> PropuestasAgrupamiento => Set<PropuestaAgrupamiento>();
    public DbSet<ValidacionJuridica> ValidacionesJuridicas => Set<ValidacionJuridica>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<ParametroNormalizacion> ParametrosNormalizacion => Set<ParametroNormalizacion>();
    public DbSet<HistorialCambioCatalogo> HistorialCambiosCatalogo => Set<HistorialCambioCatalogo>();
    public DbSet<LoteCarga> LotesCarga => Set<LoteCarga>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Claves compuestas para tablas intermedias (3FN) ----
        modelBuilder.Entity<UsuarioRol>().HasKey(ur => new { ur.UsuarioId, ur.RolId });
        modelBuilder.Entity<RolPermiso>().HasKey(rp => new { rp.RolId, rp.PermisoId });

        // ---- Relaciones 1:1 ----
        modelBuilder.Entity<DelitoOriginal>()
            .HasOne(d => d.DelitoNormalizado)
            .WithOne(n => n.DelitoOriginal)
            .HasForeignKey<DelitoNormalizado>(n => n.DelitoOriginalId);

        modelBuilder.Entity<DelitoNormalizado>()
            .HasOne(n => n.Embedding)
            .WithOne(e => e.DelitoNormalizado)
            .HasForeignKey<Embedding>(e => e.DelitoNormalizadoId);

        // ---- Relaciones 1:N ----
        modelBuilder.Entity<FamiliaDelictiva>()
            .HasMany(f => f.Delitos)
            .WithOne(d => d.FamiliaDelictiva)
            .HasForeignKey(d => d.FamiliaDelictivaId);

        modelBuilder.Entity<DelitoCatalogo>()
            .HasMany(d => d.Variantes)
            .WithOne(v => v.DelitoCatalogo)
            .HasForeignKey(v => v.DelitoCatalogoId);

        modelBuilder.Entity<PropuestaAgrupamiento>()
            .HasMany(p => p.Validaciones)
            .WithOne(v => v.PropuestaAgrupamiento)
            .HasForeignKey(v => v.PropuestaAgrupamientoId);

        // ---- Índices únicos / de búsqueda frecuente ----
        modelBuilder.Entity<DelitoOriginal>()
            .HasIndex(d => d.IdUnicoCaso);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Correo)
            .IsUnique();

        modelBuilder.Entity<Rol>()
            .HasIndex(r => r.Nombre)
            .IsUnique();

        modelBuilder.Entity<Permiso>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        // ---- Tipo de columna explícito para el vector de embedding ----
        // En SQLite (modo demo) se serializa como JSON de floats.
        // En PostgreSQL + pgvector, sustituir por .HasColumnType("vector(384)")
        // y agregar el paquete Pgvector.EntityFrameworkCore.
        modelBuilder.Entity<Embedding>()
            .Property(e => e.Vector)
            .HasConversion(
                v => SerializarVector(v),
                v => DeserializarVector(v));
    }
    // Métodos auxiliares: HasConversion construye un árbol de expresión y no
    // admite llamadas a métodos con parámetros opcionales (como las
    // sobrecargas de JsonSerializer.Serialize/Deserialize), de ahí que se
    // aíslen aquí en métodos simples de un solo argumento (CS0854).
    private static string SerializarVector(float[] vector)
        => System.Text.Json.JsonSerializer.Serialize(vector);

    private static float[] DeserializarVector(string json)
        => System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
}
