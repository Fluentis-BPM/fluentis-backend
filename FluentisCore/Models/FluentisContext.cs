using Microsoft.EntityFrameworkCore;
using FluentisCore.Models.UserManagement;
using FluentisCore.Models.WorkflowManagement;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.CommentAndNotificationManagement;
using FluentisCore.Models.MetricsAndReportsManagement;
using FluentisCore.Models.ProposalAndVotingManagement;
using FluentisCore.Models.BackupAndIncidentManagement;

namespace FluentisCore.Models
{
    public class FluentisContext : DbContext
    {
        public FluentisContext(DbContextOptions<FluentisContext> options) : base(options)
        {
        }

        // Tablas de UserManagement
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Rol> Roles { get; set; }

        public DbSet<Cargo> Cargos { get; set; }

        // Tablas de WorkflowManagement
        public DbSet<FlujoAprobacion> FlujosAprobacion { get; set; }
        public DbSet<PasoFlujo> PasosFlujo { get; set; }
        public DbSet<CaminoParalelo> CaminosParalelos { get; set; }
        public DbSet<Solicitud> Solicitudes { get; set; }
        public DbSet<FlujoActivo> FlujosActivos { get; set; }
        public DbSet<PasoSolicitud> PasosSolicitud { get; set; }

        // Tablas de InputAndApprovalManagement
        public DbSet<Inputs> Inputs { get; set; }
        public DbSet<RelacionInput> RelacionesInput { get; set; }
        public DbSet<GrupoAprobacion> GruposAprobacion { get; set; }
        public DbSet<RelacionGrupoAprobacion> RelacionesGrupoAprobacion { get; set; }
        public DbSet<RelacionDecisionUsuario> DecisionesUsuario { get; set; }
        public DbSet<Delegacion> Delegaciones { get; set; }
        public DbSet<RelacionUsuarioGrupo> RelacionesUsuarioGrupo { get; set; }

        // Tablas de CommentAndNotificationManagement
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

        // Tablas de MetricsAndReportsManagement
        public DbSet<Metrica> Metricas { get; set; }
        public DbSet<Informe> Informes { get; set; }
        public DbSet<Excepcion> Excepciones { get; set; }
        public DbSet<InformeMetrica> InformesMetricas { get; set; }
        public DbSet<InformeFlujo> InformesFlujo { get; set; }

        // Tablas de ProposalAndVotingManagement
        public DbSet<Propuesta> Propuestas { get; set; }
        public DbSet<Votacion> Votaciones { get; set; }
        public DbSet<Voto> Votos { get; set; }

        // Tablas de BackupAndIncidentManagement
        public DbSet<Backup> Backups { get; set; }
        public DbSet<Incidente> Incidentes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ----------------------------------------------------------
            // Fix for CaminosParalelos (PK error)
            // ----------------------------------------------------------
            modelBuilder.Entity<CaminoParalelo>(entity =>
            {
                entity.HasOne(c => c.PasoOrigen)
                    .WithMany()
                    .HasForeignKey(c => c.PasoOrigenId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade

                entity.HasOne(c => c.PasoDestino)
                    .WithMany()
                    .HasForeignKey(c => c.PasoDestinoId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade
            });
            // ----------------------------------------------------------
            // Fix for Delegaciones
            // ----------------------------------------------------------
            modelBuilder.Entity<Delegacion>(entity =>
            {
                entity.HasOne(d => d.Delegado)
                    .WithMany()
                    .HasForeignKey(d => d.DelegadoId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade

                entity.HasOne(d => d.Superior)
                    .WithMany()
                    .HasForeignKey(d => d.SuperiorId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade
            });

            // ----------------------------------------------------------
            // Fix for InformesFlujo
            // ----------------------------------------------------------
            modelBuilder.Entity<InformeFlujo>(entity =>
            {
                // Relationship with FlujoAprobacion
                entity.HasOne(e => e.FlujoAprobacion)
                    .WithMany()
                    .HasForeignKey(e => e.FlujoId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade

                // Relationship with Informe
                entity.HasOne(e => e.Informe)
                    .WithMany()
                    .HasForeignKey(e => e.InformeId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade
            });

            // ----------------------------------------------------------
            // Fix for InformesMetricas
            // ----------------------------------------------------------
            modelBuilder.Entity<InformeMetrica>(entity =>
            {
                entity.HasOne(im => im.Informe)
                    .WithMany()
                    .HasForeignKey(im => im.InformeId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade

                entity.HasOne(im => im.Metrica)
                    .WithMany()
                    .HasForeignKey(im => im.MetricaId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade
            });

            // ----------------------------------------------------------
            // Fix for RelacionesUsuarioGrupo
            // ----------------------------------------------------------
            modelBuilder.Entity<RelacionUsuarioGrupo>(entity =>
            {
                entity.HasOne(rug => rug.GrupoAprobacion)
                    .WithMany(g => g.RelacionesUsuarioGrupo)
                    .HasForeignKey(rug => rug.GrupoAprobacionId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(rug => rug.Usuario)
                    .WithMany()
                    .HasForeignKey(rug => rug.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ----------------------------------------------------------
            // Fix for RelacionDecisionUsuario
            // ----------------------------------------------------------
            modelBuilder.Entity<RelacionDecisionUsuario>()
                    .HasOne(rdu => rdu.RelacionGrupoAprobacion)
                    .WithMany(rga => rga.Decisiones)
                    .HasForeignKey(rdu => rdu.RelacionGrupoAprobacionId)
                    .OnDelete(DeleteBehavior.Restrict);

            // ----------------------------------------------------------
            // Fix for RelacionInput
            // ----------------------------------------------------------
            modelBuilder.Entity<RelacionInput>()
                .HasOne(ri => ri.Solicitud)
                .WithMany(s => s.Inputs)
                .HasForeignKey(ri => ri.SolicitudId)
                .OnDelete(DeleteBehavior.NoAction);

            // ----------------------------------------------------------
            // Fix for Votos
            // ----------------------------------------------------------
            modelBuilder.Entity<Voto>(entity =>
            {
                entity.HasOne(v => v.Votacion)
                    .WithMany()
                    .HasForeignKey(v => v.VotacionId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade
            });

            modelBuilder.Entity<FlujoActivo>(entity =>
            {
                // Relationship with Solicitud
                entity.HasOne(f => f.Solicitud)
                    .WithMany()
                    .HasForeignKey(f => f.SolicitudId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade

                // Relationship with FlujoEjecucion (optional: disable if causing cycles)
                entity.HasOne(f => f.FlujoEjecucion)
                    .WithMany()
                    .HasForeignKey(f => f.FlujoEjecucionId)
                    .OnDelete(DeleteBehavior.NoAction); // 🔴 Disable cascade
            });

            modelBuilder.Entity<Cargo>(entity =>
            {
                entity.HasKey(e => e.IdCargo);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(255);

                // Configuramos solo la relación JefeCargo → Cargo sin collection inversa
                entity.HasOne(e => e.JefeCargo)        // propiedad de navegación de referencia
                    .WithMany()                        // SIN parámetro: no hay colección inversa
                    .HasForeignKey(e => e.IdJefeCargo) // la FK real en IdJefeCargo
                    .IsRequired(false) //
                    .OnDelete(DeleteBehavior.NoAction);// deshabilita cascada :contentReference[oaicite:1]{index=1}
            });
            

            // Nuevas configuraciones para PasoSolicitud
            modelBuilder.Entity<PasoSolicitud>(entity =>
            {
                entity.HasMany(p => p.RelacionesInput)
                    .WithOne(ri => ri.PasoSolicitud)
                    .HasForeignKey(ri => ri.PasoSolicitudId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.RelacionesGrupoAprobacion)
                    .WithOne(rga => rga.PasoSolicitud)
                    .HasForeignKey<RelacionGrupoAprobacion>(rga => rga.PasoSolicitudId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Comentarios)
                    .WithOne(c => c.PasoSolicitud)
                    .HasForeignKey(c => c.PasoSolicitudId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Excepciones)
                    .WithOne(e => e.PasoSolicitud)
                    .HasForeignKey(e => e.PasoSolicitudId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
        public DbSet<FluentisCore.Models.UserManagement.Cargo> Cargo { get; set; } = default!;
    }
}