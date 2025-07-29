using SignalR.Models;
using Microsoft.EntityFrameworkCore;

namespace SignalR.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }

        public DbSet<UsuarioGrupo> UsuariosGrupos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<LlamadaGrupal> LlamadasGrupales { get; set; }
        public DbSet<ParticipanteLlamada> ParticipantesLlamada { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioGrupo>(entity =>
            {
                entity.ToTable("grupo_usuarios");

                // Mapear columnas con nombres exactos de la base de datos
                entity.Property(e => e.GrupoId).HasColumnName("grupo_id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            });

            modelBuilder.Entity<LlamadaGrupal>(entity =>
            {
                entity.ToTable("llamadas_grupales");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GrupoId).HasColumnName("grupo_id");
                entity.Property(e => e.IniciadorId).HasColumnName("iniciador_id");
                entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
                entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
                entity.Property(e => e.Activa).HasColumnName("activa");
                entity.Property(e => e.MaxParticipantes).HasColumnName("max_participantes");
            });

            modelBuilder.Entity<ParticipanteLlamada>(entity =>
            {
                entity.ToTable("participantes_llamada");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LlamadaGrupalId).HasColumnName("llamada_grupal_id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                entity.Property(e => e.FechaUnion).HasColumnName("fecha_union");
                entity.Property(e => e.FechaSalida).HasColumnName("fecha_salida");
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.CamaraActiva).HasColumnName("camara_activa");
                entity.Property(e => e.MicrofonoActivo).HasColumnName("microfono_activo");
                entity.Property(e => e.CompartiendoPantalla).HasColumnName("compartiendo_pantalla");

                // Relación con LlamadaGrupal
                entity.HasOne(p => p.LlamadaGrupal)
                      .WithMany(l => l.Participantes)
                      .HasForeignKey(p => p.LlamadaGrupalId);
            });
        }
    }
}
