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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioGrupo>(entity =>
            {
                entity.ToTable("grupo_usuarios");

                // Mapear columnas con nombres exactos de la base de datos
                entity.Property(e => e.GrupoId).HasColumnName("grupo_id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            });
        }

    }
}
