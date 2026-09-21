using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Data;

public partial class ClubCreativoDbContext : DbContext
{
    public ClubCreativoDbContext(DbContextOptions<ClubCreativoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Auditorium> Auditoria { get; set; }

    public virtual DbSet<CanjesRecompensa> CanjesRecompensas { get; set; }

    public virtual DbSet<Carrito> Carritos { get; set; }

    public virtual DbSet<CarritoDetalle> CarritoDetalles { get; set; }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<CodigosQrusuario> CodigosQrusuarios { get; set; }

    public virtual DbSet<ComentariosResena> ComentariosResenas { get; set; }

    public virtual DbSet<Emprendimiento> Emprendimientos { get; set; }

    public virtual DbSet<EmprendimientoImagene> EmprendimientoImagenes { get; set; }

    public virtual DbSet<EmprendimientoRevisione> EmprendimientoRevisiones { get; set; }

    public virtual DbSet<Encuesta> Encuestas { get; set; }

    public virtual DbSet<Etiqueta> Etiquetas { get; set; }

    public virtual DbSet<Evento> Eventos { get; set; }

    public virtual DbSet<Favorito> Favoritos { get; set; }

    public virtual DbSet<Galeria> Galerias { get; set; }

    public virtual DbSet<GaleriaImagene> GaleriaImagenes { get; set; }

    public virtual DbSet<InscripcionesEvento> InscripcionesEventos { get; set; }

    public virtual DbSet<InscripcionesTaller> InscripcionesTallers { get; set; }

    public virtual DbSet<IntentosAcceso> IntentosAccesos { get; set; }

    public virtual DbSet<MensajesContacto> MensajesContactos { get; set; }

    public virtual DbSet<MotivosRechazo> MotivosRechazos { get; set; }

    public virtual DbSet<MovimientosInventario> MovimientosInventarios { get; set; }

    public virtual DbSet<MovimientosPunto> MovimientosPuntos { get; set; }

    public virtual DbSet<Noticia> Noticias { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<OpcionesPreguntum> OpcionesPregunta { get; set; }

    public virtual DbSet<ParticipacionesEvento> ParticipacionesEventos { get; set; }

    public virtual DbSet<PlanesSuscripcion> PlanesSuscripcions { get; set; }

    public virtual DbSet<PreferenciasNotificacion> PreferenciasNotificacions { get; set; }

    public virtual DbSet<PreguntasEncuestum> PreguntasEncuesta { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ProductoImagene> ProductoImagenes { get; set; }

    public virtual DbSet<Promocione> Promociones { get; set; }

    public virtual DbSet<Provincia> Provincias { get; set; }

    public virtual DbSet<Recompensa> Recompensas { get; set; }

    public virtual DbSet<RegistrosAsistencium> RegistrosAsistencia { get; set; }

    public virtual DbSet<RespuestaDetalle> RespuestaDetalles { get; set; }

    public virtual DbSet<RespuestasEncuestum> RespuestasEncuesta { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SuscripcionCancelacione> SuscripcionCancelaciones { get; set; }

    public virtual DbSet<Suscripcione> Suscripciones { get; set; }

    public virtual DbSet<Tallere> Talleres { get; set; }

    public virtual DbSet<TokensRecuperacionContrasena> TokensRecuperacionContrasenas { get; set; }

    public virtual DbSet<UsosPromocion> UsosPromocions { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<UsuarioRole> UsuarioRoles { get; set; }

    public virtual DbSet<Venta> Ventas { get; set; }

    public virtual DbSet<VentaDetalle> VentaDetalles { get; set; }

    public virtual DbSet<VentasCancelada> VentasCanceladas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auditorium>(entity =>
        {
            entity.HasKey(e => e.IdAuditoria);

            entity.HasIndex(e => e.FechaHora, "IX_Auditoria_Fecha").IsDescending();

            entity.HasIndex(e => new { e.IdUsuario, e.FechaHora }, "IX_Auditoria_Usuario").IsDescending(false, true);

            entity.Property(e => e.Accion).HasMaxLength(80);
            entity.Property(e => e.Descripcion).HasMaxLength(1500);
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(45)
                .HasColumnName("DireccionIP");
            entity.Property(e => e.Entidad).HasMaxLength(80);
            entity.Property(e => e.FechaHora)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IdRegistro).HasMaxLength(100);
            entity.Property(e => e.Modulo).HasMaxLength(80);
            entity.Property(e => e.Nivel)
                .HasMaxLength(20)
                .HasDefaultValue("Informativo");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Auditoria)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_Auditoria_Usuarios");
        });

        modelBuilder.Entity<CanjesRecompensa>(entity =>
        {
            entity.HasKey(e => e.IdCanje);

            entity.ToTable("CanjesRecompensa");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Canjeado");
            entity.Property(e => e.FechaCanje)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdRecompensaNavigation).WithMany(p => p.CanjesRecompensas)
                .HasForeignKey(d => d.IdRecompensa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CanjesRecompensa_Recompensas");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.CanjesRecompensas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CanjesRecompensa_Usuarios");
        });

        modelBuilder.Entity<Carrito>(entity =>
        {
            entity.HasKey(e => e.IdCarrito);

            entity.HasIndex(e => e.IdUsuario, "UX_Carritos_Usuario_Activo")
                .IsUnique()
                .HasFilter("([Estado]=N'Activo')");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Activo");
            entity.Property(e => e.FechaActualizacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Carrito)
                .HasForeignKey<Carrito>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Carritos_Usuarios");
        });

        modelBuilder.Entity<CarritoDetalle>(entity =>
        {
            entity.HasKey(e => e.IdCarritoDetalle);

            entity.HasIndex(e => new { e.IdCarrito, e.IdProducto }, "UQ_CarritoDetalles").IsUnique();

            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdCarritoNavigation).WithMany(p => p.CarritoDetalles)
                .HasForeignKey(d => d.IdCarrito)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarritoDetalles_Carritos");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.CarritoDetalles)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarritoDetalles_Productos");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria);

            entity.HasIndex(e => new { e.Nombre, e.Modulo }, "UQ_Categorias_Nombre_Modulo").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Modulo).HasMaxLength(30);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<CodigosQrusuario>(entity =>
        {
            entity.HasKey(e => e.IdCodigoQr);

            entity.ToTable("CodigosQRUsuario");

            entity.HasIndex(e => e.Codigo, "UQ_CodigosQRUsuario_Codigo").IsUnique();

            entity.HasIndex(e => e.IdUsuario, "UQ_CodigosQRUsuario_Usuario").IsUnique();

            entity.Property(e => e.IdCodigoQr).HasColumnName("IdCodigoQR");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Codigo).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.CodigosQrusuario)
                .HasForeignKey<CodigosQrusuario>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CodigosQRUsuario_Usuarios");
        });

        modelBuilder.Entity<ComentariosResena>(entity =>
        {
            entity.HasKey(e => e.IdComentario);

            entity.Property(e => e.Contenido).HasMaxLength(1500);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Visible");
            entity.Property(e => e.FechaEdicion).HasPrecision(0);
            entity.Property(e => e.FechaPublicacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithMany(p => p.ComentariosResenas)
                .HasForeignKey(d => d.IdEmprendimiento)
                .HasConstraintName("FK_ComentariosResenas_Emprendimientos");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.ComentariosResenas)
                .HasForeignKey(d => d.IdEvento)
                .HasConstraintName("FK_ComentariosResenas_Eventos");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ComentariosResenas)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK_ComentariosResenas_Productos");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ComentariosResenas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComentariosResenas_Usuarios");
        });

        modelBuilder.Entity<Emprendimiento>(entity =>
        {
            entity.HasKey(e => e.IdEmprendimiento);

            entity.HasIndex(e => e.EstadoAprobacion, "IX_Emprendimientos_Estado");

            entity.HasIndex(e => e.IdUsuarioPropietario, "IX_Emprendimientos_Propietario");

            entity.HasIndex(e => e.NombreComercial, "UQ_Emprendimientos_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CedulaJuridica).HasMaxLength(30);
            entity.Property(e => e.Correo).HasMaxLength(254);
            entity.Property(e => e.Descripcion).HasMaxLength(1500);
            entity.Property(e => e.EstadoAprobacion)
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.Facebook).HasMaxLength(150);
            entity.Property(e => e.Instagram).HasMaxLength(150);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.NombreComercial).HasMaxLength(150);
            entity.Property(e => e.ParticipaHechoEnCr).HasColumnName("ParticipaHechoEnCR");
            entity.Property(e => e.SitioWeb).HasMaxLength(300);
            entity.Property(e => e.Telefono).HasMaxLength(25);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Emprendimientos)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Emprendimientos_Categorias");

            entity.HasOne(d => d.IdUsuarioPropietarioNavigation).WithMany(p => p.Emprendimientos)
                .HasForeignKey(d => d.IdUsuarioPropietario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Emprendimientos_Usuarios");

            entity.HasMany(d => d.IdEtiqueta).WithMany(p => p.IdEmprendimientos)
                .UsingEntity<Dictionary<string, object>>(
                    "EmprendimientoEtiqueta",
                    r => r.HasOne<Etiqueta>().WithMany()
                        .HasForeignKey("IdEtiqueta")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EmprendimientoEtiquetas_Etiquetas"),
                    l => l.HasOne<Emprendimiento>().WithMany()
                        .HasForeignKey("IdEmprendimiento")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EmprendimientoEtiquetas_Emprendimientos"),
                    j =>
                    {
                        j.HasKey("IdEmprendimiento", "IdEtiqueta");
                        j.ToTable("EmprendimientoEtiquetas");
                    });
        });

        modelBuilder.Entity<EmprendimientoImagene>(entity =>
        {
            entity.HasKey(e => e.IdImagen);

            entity.HasIndex(e => e.IdEmprendimiento, "UX_EmprendimientoImagenes_Principal")
                .IsUnique()
                .HasFilter("([EsPrincipal]=(1))");

            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NombreArchivo).HasMaxLength(255);
            entity.Property(e => e.UrlImagen).HasMaxLength(500);

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithOne(p => p.EmprendimientoImagene)
                .HasForeignKey<EmprendimientoImagene>(d => d.IdEmprendimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmprendimientoImagenes_Emprendimientos");
        });

        modelBuilder.Entity<EmprendimientoRevisione>(entity =>
        {
            entity.HasKey(e => e.IdEmprendimiento);

            entity.Property(e => e.IdEmprendimiento).ValueGeneratedNever();
            entity.Property(e => e.FechaResolucion).HasPrecision(0);
            entity.Property(e => e.FechaSolicitud)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithOne(p => p.EmprendimientoRevisione)
                .HasForeignKey<EmprendimientoRevisione>(d => d.IdEmprendimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmprendimientoRevisiones_Emprendimientos");
        });

        modelBuilder.Entity<Encuesta>(entity =>
        {
            entity.HasKey(e => e.IdEncuesta);

            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Borrador");
            entity.Property(e => e.FechaCierre).HasPrecision(0);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaPublicacion).HasPrecision(0);
            entity.Property(e => e.Titulo).HasMaxLength(180);

            entity.HasOne(d => d.IdCreadorNavigation).WithMany(p => p.Encuesta)
                .HasForeignKey(d => d.IdCreador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Encuestas_Usuarios");

            entity.HasMany(d => d.IdEventos).WithMany(p => p.IdEncuesta)
                .UsingEntity<Dictionary<string, object>>(
                    "EncuestaEvento",
                    r => r.HasOne<Evento>().WithMany()
                        .HasForeignKey("IdEvento")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EncuestaEventos_Eventos"),
                    l => l.HasOne<Encuesta>().WithMany()
                        .HasForeignKey("IdEncuesta")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EncuestaEventos_Encuestas"),
                    j =>
                    {
                        j.HasKey("IdEncuesta", "IdEvento");
                        j.ToTable("EncuestaEventos");
                    });

            entity.HasMany(d => d.IdTallers).WithMany(p => p.IdEncuesta)
                .UsingEntity<Dictionary<string, object>>(
                    "EncuestaTallere",
                    r => r.HasOne<Tallere>().WithMany()
                        .HasForeignKey("IdTaller")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EncuestaTalleres_Talleres"),
                    l => l.HasOne<Encuesta>().WithMany()
                        .HasForeignKey("IdEncuesta")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EncuestaTalleres_Encuestas"),
                    j =>
                    {
                        j.HasKey("IdEncuesta", "IdTaller");
                        j.ToTable("EncuestaTalleres");
                    });
        });

        modelBuilder.Entity<Etiqueta>(entity =>
        {
            entity.HasKey(e => e.IdEtiqueta);

            entity.HasIndex(e => e.Nombre, "UQ_Etiquetas_Nombre").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Nombre).HasMaxLength(80);
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.IdEvento);

            entity.HasIndex(e => new { e.FechaInicio, e.Estado }, "IX_Eventos_Fecha_Estado");

            entity.Property(e => e.Descripcion).HasMaxLength(1500);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Programado");
            entity.Property(e => e.FechaFin).HasPrecision(0);
            entity.Property(e => e.FechaInicio).HasPrecision(0);
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ImagenUrl).HasMaxLength(500);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Ubicacion).HasMaxLength(250);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Eventos)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Eventos_Categorias");

            entity.HasMany(d => d.IdEtiqueta).WithMany(p => p.IdEventos)
                .UsingEntity<Dictionary<string, object>>(
                    "EventoEtiqueta",
                    r => r.HasOne<Etiqueta>().WithMany()
                        .HasForeignKey("IdEtiqueta")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EventoEtiquetas_Etiquetas"),
                    l => l.HasOne<Evento>().WithMany()
                        .HasForeignKey("IdEvento")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EventoEtiquetas_Eventos"),
                    j =>
                    {
                        j.HasKey("IdEvento", "IdEtiqueta");
                        j.ToTable("EventoEtiquetas");
                    });
        });

        modelBuilder.Entity<Favorito>(entity =>
        {
            entity.HasKey(e => e.IdFavorito);

            entity.HasIndex(e => new { e.IdUsuario, e.IdEmprendimiento }, "UX_Favoritos_Usuario_Emprendimiento")
                .IsUnique()
                .HasFilter("([IdEmprendimiento] IS NOT NULL)");

            entity.HasIndex(e => new { e.IdUsuario, e.IdEvento }, "UX_Favoritos_Usuario_Evento")
                .IsUnique()
                .HasFilter("([IdEvento] IS NOT NULL)");

            entity.HasIndex(e => new { e.IdUsuario, e.IdProducto }, "UX_Favoritos_Usuario_Producto")
                .IsUnique()
                .HasFilter("([IdProducto] IS NOT NULL)");

            entity.HasIndex(e => new { e.IdUsuario, e.IdTaller }, "UX_Favoritos_Usuario_Taller")
                .IsUnique()
                .HasFilter("([IdTaller] IS NOT NULL)");

            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.IdEmprendimiento)
                .HasConstraintName("FK_Favoritos_Emprendimientos");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.IdEvento)
                .HasConstraintName("FK_Favoritos_Eventos");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK_Favoritos_Productos");

            entity.HasOne(d => d.IdTallerNavigation).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.IdTaller)
                .HasConstraintName("FK_Favoritos_Talleres");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favoritos_Usuarios");
        });

        modelBuilder.Entity<Galeria>(entity =>
        {
            entity.HasKey(e => e.IdGaleria);

            entity.Property(e => e.Descripcion).HasMaxLength(800);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Borrador");
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Galeria)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Galerias_Categorias");

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithMany(p => p.Galeria)
                .HasForeignKey(d => d.IdEmprendimiento)
                .HasConstraintName("FK_Galerias_Emprendimientos");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Galeria)
                .HasForeignKey(d => d.IdEvento)
                .HasConstraintName("FK_Galerias_Eventos");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Galeria)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK_Galerias_Productos");
        });

        modelBuilder.Entity<GaleriaImagene>(entity =>
        {
            entity.HasKey(e => e.IdImagen);

            entity.HasIndex(e => e.IdGaleria, "UX_GaleriaImagenes_Portada")
                .IsUnique()
                .HasFilter("([EsPortada]=(1))");

            entity.Property(e => e.NombreArchivo).HasMaxLength(255);
            entity.Property(e => e.OrdenVisual).HasDefaultValue((byte)1);
            entity.Property(e => e.UrlImagen).HasMaxLength(500);

            entity.HasOne(d => d.IdGaleriaNavigation).WithOne(p => p.GaleriaImagene)
                .HasForeignKey<GaleriaImagene>(d => d.IdGaleria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GaleriaImagenes_Galerias");
        });

        modelBuilder.Entity<InscripcionesEvento>(entity =>
        {
            entity.HasKey(e => e.IdInscripcionEvento);

            entity.ToTable("InscripcionesEvento");

            entity.HasIndex(e => new { e.IdUsuario, e.IdEvento }, "UQ_InscripcionesEvento").IsUnique();

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Confirmada");
            entity.Property(e => e.FechaInscripcion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.InscripcionesEventos)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InscripcionesEvento_Eventos");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.InscripcionesEventos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InscripcionesEvento_Usuarios");
        });

        modelBuilder.Entity<InscripcionesTaller>(entity =>
        {
            entity.HasKey(e => e.IdInscripcionTaller);

            entity.ToTable("InscripcionesTaller");

            entity.HasIndex(e => new { e.IdUsuario, e.IdTaller }, "UQ_InscripcionesTaller").IsUnique();

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Confirmada");
            entity.Property(e => e.FechaInscripcion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdTallerNavigation).WithMany(p => p.InscripcionesTallers)
                .HasForeignKey(d => d.IdTaller)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InscripcionesTaller_Talleres");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.InscripcionesTallers)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InscripcionesTaller_Usuarios");
        });

        modelBuilder.Entity<IntentosAcceso>(entity =>
        {
            entity.HasKey(e => e.IdIntentoAcceso);

            entity.ToTable("IntentosAcceso");

            entity.Property(e => e.Correo).HasMaxLength(254);
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(45)
                .HasColumnName("DireccionIP");
            entity.Property(e => e.FechaHora)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.IntentosAccesos)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_IntentosAcceso_Usuarios");
        });

        modelBuilder.Entity<MensajesContacto>(entity =>
        {
            entity.HasKey(e => e.IdMensajeContacto);

            entity.ToTable("MensajesContacto");

            entity.Property(e => e.Asunto).HasMaxLength(180);
            entity.Property(e => e.Correo).HasMaxLength(254);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaEnvio)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Mensaje).HasMaxLength(2000);
            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.Telefono).HasMaxLength(25);
        });

        modelBuilder.Entity<MotivosRechazo>(entity =>
        {
            entity.HasKey(e => e.IdMotivoRechazo);

            entity.ToTable("MotivosRechazo");

            entity.Property(e => e.FechaRechazo)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Motivo).HasMaxLength(600);

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithMany(p => p.MotivosRechazos)
                .HasForeignKey(d => d.IdEmprendimiento)
                .HasConstraintName("FK_MotivosRechazo_Emprendimientos");

            entity.HasOne(d => d.IdParticipacionNavigation).WithMany(p => p.MotivosRechazos)
                .HasForeignKey(d => d.IdParticipacion)
                .HasConstraintName("FK_MotivosRechazo_ParticipacionesEvento");
        });

        modelBuilder.Entity<MovimientosInventario>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento);

            entity.ToTable("MovimientosInventario");

            entity.Property(e => e.FechaMovimiento)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Observacion).HasMaxLength(250);
            entity.Property(e => e.TipoMovimiento).HasMaxLength(20);

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.MovimientosInventarios)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosInventario_Productos");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.MovimientosInventarios)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosInventario_Usuarios");
        });

        modelBuilder.Entity<MovimientosPunto>(entity =>
        {
            entity.HasKey(e => e.IdMovimientoPuntos);

            entity.HasIndex(e => new { e.IdUsuario, e.FechaMovimiento }, "IX_MovimientosPuntos_Usuario_Fecha").IsDescending(false, true);

            entity.Property(e => e.FechaMovimiento)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Motivo).HasMaxLength(300);
            entity.Property(e => e.TipoMovimiento).HasMaxLength(20);

            entity.HasOne(d => d.IdAsistenciaNavigation).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.IdAsistencia)
                .HasConstraintName("FK_MovimientosPuntos_Asistencias");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosPuntos_Usuarios");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.IdVenta)
                .HasConstraintName("FK_MovimientosPuntos_Ventas");
        });

        modelBuilder.Entity<Noticia>(entity =>
        {
            entity.HasKey(e => e.IdNoticia);

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Borrador");
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaPublicacion).HasPrecision(0);
            entity.Property(e => e.ImagenUrl).HasMaxLength(500);
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasDefaultValue("Noticia");
            entity.Property(e => e.Titulo).HasMaxLength(180);

            entity.HasOne(d => d.IdAutorNavigation).WithMany(p => p.Noticia)
                .HasForeignKey(d => d.IdAutor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Noticias_Usuarios");

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Noticia)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Noticias_Categorias");
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion);

            entity.HasIndex(e => new { e.IdUsuario, e.Leida, e.FechaEnvio }, "IX_Notificaciones_Usuario_Leida").IsDescending(false, false, true);

            entity.Property(e => e.FechaEnvio)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaLectura).HasPrecision(0);
            entity.Property(e => e.Mensaje).HasMaxLength(1000);
            entity.Property(e => e.Tipo).HasMaxLength(50);
            entity.Property(e => e.Titulo).HasMaxLength(150);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notificaciones_Usuarios");
        });

        modelBuilder.Entity<OpcionesPreguntum>(entity =>
        {
            entity.HasKey(e => e.IdOpcion);

            entity.HasIndex(e => new { e.IdOpcion, e.IdPregunta }, "UQ_OpcionesPregunta_IdOpcion_IdPregunta").IsUnique();

            entity.Property(e => e.Texto).HasMaxLength(300);

            entity.HasOne(d => d.IdPreguntaNavigation).WithMany(p => p.OpcionesPregunta)
                .HasForeignKey(d => d.IdPregunta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OpcionesPregunta_Preguntas");
        });

        modelBuilder.Entity<ParticipacionesEvento>(entity =>
        {
            entity.HasKey(e => e.IdParticipacion);

            entity.ToTable("ParticipacionesEvento");

            entity.HasIndex(e => new { e.IdEvento, e.IdEmprendimiento }, "UQ_ParticipacionesEvento").IsUnique();

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaSolicitud)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NecesidadesEspeciales).HasMaxLength(800);
            entity.Property(e => e.ProductosPresentados).HasMaxLength(800);
            entity.Property(e => e.TipoEspacio).HasMaxLength(100);

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithMany(p => p.ParticipacionesEventos)
                .HasForeignKey(d => d.IdEmprendimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipacionesEvento_Emprendimientos");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.ParticipacionesEventos)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipacionesEvento_Eventos");
        });

        modelBuilder.Entity<PlanesSuscripcion>(entity =>
        {
            entity.HasKey(e => e.IdPlan);

            entity.ToTable("PlanesSuscripcion");

            entity.HasIndex(e => e.Nombre, "UQ_PlanesSuscripcion_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Beneficios).HasMaxLength(1500);
            entity.Property(e => e.Descripcion).HasMaxLength(600);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Periodicidad).HasMaxLength(20);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<PreferenciasNotificacion>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("PreferenciasNotificacion");

            entity.Property(e => e.IdUsuario).ValueGeneratedNever();
            entity.Property(e => e.CanalCorreo).HasDefaultValue(true);
            entity.Property(e => e.CanalPlataforma).HasDefaultValue(true);
            entity.Property(e => e.Compras).HasDefaultValue(true);
            entity.Property(e => e.Eventos).HasDefaultValue(true);
            entity.Property(e => e.FrecuenciaCorreo)
                .HasMaxLength(20)
                .HasDefaultValue("Inmediata");
            entity.Property(e => e.Promociones).HasDefaultValue(true);
            entity.Property(e => e.Recordatorios).HasDefaultValue(true);
            entity.Property(e => e.Talleres).HasDefaultValue(true);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.PreferenciasNotificacion)
                .HasForeignKey<PreferenciasNotificacion>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreferenciasNotificacion_Usuarios");
        });

        modelBuilder.Entity<PreguntasEncuestum>(entity =>
        {
            entity.HasKey(e => e.IdPregunta);

            entity.Property(e => e.Obligatoria).HasDefaultValue(true);
            entity.Property(e => e.Texto).HasMaxLength(800);
            entity.Property(e => e.TipoPregunta).HasMaxLength(20);

            entity.HasOne(d => d.IdEncuestaNavigation).WithMany(p => p.PreguntasEncuesta)
                .HasForeignKey(d => d.IdEncuesta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreguntasEncuesta_Encuestas");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto);

            entity.HasIndex(e => new { e.IdCategoria, e.Estado }, "IX_Productos_Categoria_Estado");

            entity.HasIndex(e => e.IdEmprendimiento, "IX_Productos_Emprendimiento");

            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Borrador");
            entity.Property(e => e.FechaPublicacion).HasPrecision(0);
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoPublicacion).HasMaxLength(20);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Productos_Categorias");

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdEmprendimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Emprendimientos");

            entity.HasMany(d => d.IdEtiqueta).WithMany(p => p.IdProductos)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductoEtiqueta",
                    r => r.HasOne<Etiqueta>().WithMany()
                        .HasForeignKey("IdEtiqueta")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ProductoEtiquetas_Etiquetas"),
                    l => l.HasOne<Producto>().WithMany()
                        .HasForeignKey("IdProducto")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ProductoEtiquetas_Productos"),
                    j =>
                    {
                        j.HasKey("IdProducto", "IdEtiqueta");
                        j.ToTable("ProductoEtiquetas");
                    });
        });

        modelBuilder.Entity<ProductoImagene>(entity =>
        {
            entity.HasKey(e => e.IdImagen);

            entity.HasIndex(e => e.IdProducto, "UX_ProductoImagenes_Principal")
                .IsUnique()
                .HasFilter("([EsPrincipal]=(1))");

            entity.Property(e => e.NombreArchivo).HasMaxLength(255);
            entity.Property(e => e.OrdenVisual).HasDefaultValue((byte)1);
            entity.Property(e => e.UrlImagen).HasMaxLength(500);

            entity.HasOne(d => d.IdProductoNavigation).WithOne(p => p.ProductoImagene)
                .HasForeignKey<ProductoImagene>(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoImagenes_Productos");
        });

        modelBuilder.Entity<Promocione>(entity =>
        {
            entity.HasKey(e => e.IdPromocion);

            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Programada");
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaFin).HasPrecision(0);
            entity.Property(e => e.FechaInicio).HasPrecision(0);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.TipoDescuento).HasMaxLength(20);
            entity.Property(e => e.ValorDescuento).HasColumnType("decimal(18, 2)");

            entity.HasMany(d => d.IdEmprendimientos).WithMany(p => p.IdPromocions)
                .UsingEntity<Dictionary<string, object>>(
                    "PromocionEmprendimiento",
                    r => r.HasOne<Emprendimiento>().WithMany()
                        .HasForeignKey("IdEmprendimiento")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PromocionEmprendimientos_Emprendimientos"),
                    l => l.HasOne<Promocione>().WithMany()
                        .HasForeignKey("IdPromocion")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PromocionEmprendimientos_Promociones"),
                    j =>
                    {
                        j.HasKey("IdPromocion", "IdEmprendimiento");
                        j.ToTable("PromocionEmprendimientos");
                    });

            entity.HasMany(d => d.IdEventos).WithMany(p => p.IdPromocions)
                .UsingEntity<Dictionary<string, object>>(
                    "PromocionEvento",
                    r => r.HasOne<Evento>().WithMany()
                        .HasForeignKey("IdEvento")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PromocionEventos_Eventos"),
                    l => l.HasOne<Promocione>().WithMany()
                        .HasForeignKey("IdPromocion")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PromocionEventos_Promociones"),
                    j =>
                    {
                        j.HasKey("IdPromocion", "IdEvento");
                        j.ToTable("PromocionEventos");
                    });

            entity.HasMany(d => d.IdProductos).WithMany(p => p.IdPromocions)
                .UsingEntity<Dictionary<string, object>>(
                    "PromocionProducto",
                    r => r.HasOne<Producto>().WithMany()
                        .HasForeignKey("IdProducto")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PromocionProductos_Productos"),
                    l => l.HasOne<Promocione>().WithMany()
                        .HasForeignKey("IdPromocion")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_PromocionProductos_Promociones"),
                    j =>
                    {
                        j.HasKey("IdPromocion", "IdProducto");
                        j.ToTable("PromocionProductos");
                    });
        });

        modelBuilder.Entity<Provincia>(entity =>
        {
            entity.HasKey(e => e.IdProvincia);

            entity.HasIndex(e => e.Nombre, "UQ_Provincias_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Recompensa>(entity =>
        {
            entity.HasKey(e => e.IdRecompensa);

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(600);
            entity.Property(e => e.Icono).HasMaxLength(100);
            entity.Property(e => e.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<RegistrosAsistencium>(entity =>
        {
            entity.HasKey(e => e.IdAsistencia);

            entity.HasIndex(e => new { e.IdUsuario, e.IdEvento }, "UX_Asistencia_Usuario_Evento")
                .IsUnique()
                .HasFilter("([IdEvento] IS NOT NULL)");

            entity.HasIndex(e => new { e.IdUsuario, e.IdTaller }, "UX_Asistencia_Usuario_Taller")
                .IsUnique()
                .HasFilter("([IdTaller] IS NOT NULL)");

            entity.Property(e => e.FechaHora)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Observacion).HasMaxLength(300);
            entity.Property(e => e.RegistradoPorQr)
                .HasDefaultValue(true)
                .HasColumnName("RegistradoPorQR");
            entity.Property(e => e.TipoAsistencia).HasMaxLength(20);

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.RegistrosAsistencia)
                .HasForeignKey(d => d.IdEvento)
                .HasConstraintName("FK_RegistrosAsistencia_Eventos");

            entity.HasOne(d => d.IdTallerNavigation).WithMany(p => p.RegistrosAsistencia)
                .HasForeignKey(d => d.IdTaller)
                .HasConstraintName("FK_RegistrosAsistencia_Talleres");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.RegistrosAsistencia)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RegistrosAsistencia_Usuarios");
        });

        modelBuilder.Entity<RespuestaDetalle>(entity =>
        {
            entity.HasKey(e => e.IdRespuestaDetalle);

            entity.Property(e => e.RespuestaTexto).HasMaxLength(1500);
            entity.Property(e => e.ValorNumerico).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdPreguntaNavigation).WithMany(p => p.RespuestaDetalles)
                .HasForeignKey(d => d.IdPregunta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestaDetalles_Preguntas");

            entity.HasOne(d => d.IdRespuestaEncuestaNavigation).WithMany(p => p.RespuestaDetalles)
                .HasForeignKey(d => d.IdRespuestaEncuesta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestaDetalles_Respuestas");

            entity.HasOne(d => d.OpcionesPreguntum).WithMany(p => p.RespuestaDetalles)
                .HasPrincipalKey(p => new { p.IdOpcion, p.IdPregunta })
                .HasForeignKey(d => new { d.IdOpcion, d.IdPregunta })
                .HasConstraintName("FK_RespuestaDetalles_Opciones");
        });

        modelBuilder.Entity<RespuestasEncuestum>(entity =>
        {
            entity.HasKey(e => e.IdRespuestaEncuesta);

            entity.HasIndex(e => new { e.IdEncuesta, e.IdUsuario }, "UQ_RespuestasEncuesta").IsUnique();

            entity.Property(e => e.FechaRespuesta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdEncuestaNavigation).WithMany(p => p.RespuestasEncuesta)
                .HasForeignKey(d => d.IdEncuesta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestasEncuesta_Encuestas");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.RespuestasEncuesta)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestasEncuesta_Usuarios");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.HasIndex(e => e.Nombre, "UQ_Roles_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<SuscripcionCancelacione>(entity =>
        {
            entity.HasKey(e => e.IdSuscripcion);

            entity.Property(e => e.IdSuscripcion).ValueGeneratedNever();
            entity.Property(e => e.FechaCancelacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(400);

            entity.HasOne(d => d.IdSuscripcionNavigation).WithOne(p => p.SuscripcionCancelacione)
                .HasForeignKey<SuscripcionCancelacione>(d => d.IdSuscripcion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SuscripcionCancelaciones_Suscripciones");
        });

        modelBuilder.Entity<Suscripcione>(entity =>
        {
            entity.HasKey(e => e.IdSuscripcion);

            entity.HasIndex(e => e.IdEmprendimiento, "UX_Suscripciones_Emprendimiento_Activa")
                .IsUnique()
                .HasFilter("([Estado]=N'Activa')");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Activa");

            entity.HasOne(d => d.IdEmprendimientoNavigation).WithOne(p => p.Suscripcione)
                .HasForeignKey<Suscripcione>(d => d.IdEmprendimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suscripciones_Emprendimientos");

            entity.HasOne(d => d.IdPlanNavigation).WithMany(p => p.Suscripciones)
                .HasForeignKey(d => d.IdPlan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suscripciones_Planes");
        });

        modelBuilder.Entity<Tallere>(entity =>
        {
            entity.HasKey(e => e.IdTaller);

            entity.HasIndex(e => new { e.FechaInicio, e.Estado }, "IX_Talleres_Fecha_Estado");

            entity.Property(e => e.Descripcion).HasMaxLength(1500);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Programado");
            entity.Property(e => e.FechaFin).HasPrecision(0);
            entity.Property(e => e.FechaInicio).HasPrecision(0);
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ImagenUrl).HasMaxLength(500);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Ubicacion).HasMaxLength(250);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Talleres)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Talleres_Categorias");

            entity.HasMany(d => d.IdEtiqueta).WithMany(p => p.IdTallers)
                .UsingEntity<Dictionary<string, object>>(
                    "TallerEtiqueta",
                    r => r.HasOne<Etiqueta>().WithMany()
                        .HasForeignKey("IdEtiqueta")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_TallerEtiquetas_Etiquetas"),
                    l => l.HasOne<Tallere>().WithMany()
                        .HasForeignKey("IdTaller")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_TallerEtiquetas_Talleres"),
                    j =>
                    {
                        j.HasKey("IdTaller", "IdEtiqueta");
                        j.ToTable("TallerEtiquetas");
                    });
        });

        modelBuilder.Entity<TokensRecuperacionContrasena>(entity =>
        {
            entity.HasKey(e => e.IdToken);

            entity.ToTable("TokensRecuperacionContrasena");

            entity.HasIndex(e => new { e.IdUsuario, e.Utilizado, e.FechaExpiracion }, "IX_TokensRecuperacion_Usuario");

            entity.HasIndex(e => e.TokenHash, "UQ_TokensRecuperacionContrasena_Token").IsUnique();

            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaExpiracion).HasPrecision(0);
            entity.Property(e => e.FechaUso).HasPrecision(0);
            entity.Property(e => e.TokenHash).HasMaxLength(500);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TokensRecuperacionContrasenas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TokensRecuperacionContrasena_Usuarios");
        });

        modelBuilder.Entity<UsosPromocion>(entity =>
        {
            entity.HasKey(e => e.IdUsoPromocion);

            entity.ToTable("UsosPromocion");

            entity.Property(e => e.FechaUso)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MontoDescuento).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdPromocionNavigation).WithMany(p => p.UsosPromocions)
                .HasForeignKey(d => d.IdPromocion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsosPromocion_Promociones");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.UsosPromocions)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsosPromocion_Usuarios");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.UsosPromocions)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsosPromocion_Ventas");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.HasIndex(e => e.Estado, "IX_Usuarios_Estado");

            entity.HasIndex(e => e.IdProvincia, "IX_Usuarios_Provincia");

            entity.HasIndex(e => e.Correo, "UQ_Usuarios_Correo").IsUnique();

            entity.Property(e => e.ApellidoMaterno).HasMaxLength(60);
            entity.Property(e => e.ApellidoPaterno).HasMaxLength(60);
            entity.Property(e => e.ContrasenaHash).HasMaxLength(500);
            entity.Property(e => e.Correo).HasMaxLength(254);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Activo");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FotoPerfilUrl).HasMaxLength(500);
            entity.Property(e => e.Nombre).HasMaxLength(80);
            entity.Property(e => e.Telefono).HasMaxLength(25);
            entity.Property(e => e.UltimoAcceso).HasPrecision(0);

            entity.HasOne(d => d.IdProvinciaNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdProvincia)
                .HasConstraintName("FK_Usuarios_Provincias");
        });

        modelBuilder.Entity<UsuarioRole>(entity =>
        {
            entity.HasKey(e => new { e.IdUsuario, e.IdRol });

            entity.Property(e => e.FechaAsignacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.UsuarioRoles)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsuarioRoles_Roles");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.UsuarioRoles)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsuarioRoles_Usuarios");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.IdVenta);

            entity.HasIndex(e => new { e.IdUsuario, e.FechaVenta }, "IX_Ventas_Usuario_Fecha").IsDescending(false, true);

            entity.HasIndex(e => e.NumeroOrden, "UQ_Ventas_NumeroOrden").IsUnique();

            entity.Property(e => e.Descuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DireccionEntrega).HasMaxLength(500);
            entity.Property(e => e.Estado)
                .HasMaxLength(30)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaVenta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MetodoPago).HasMaxLength(30);
            entity.Property(e => e.NumeroOrden).HasMaxLength(30);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoEntrega).HasMaxLength(30);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Usuarios");
        });

        modelBuilder.Entity<VentaDetalle>(entity =>
        {
            entity.HasKey(e => e.IdVentaDetalle);

            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Subtotal)
                .HasComputedColumnSql("(CONVERT([decimal](18,2),[Cantidad]*[PrecioUnitario]))", true)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.VentaDetalles)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentaDetalles_Productos");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.VentaDetalles)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentaDetalles_Ventas");
        });

        modelBuilder.Entity<VentasCancelada>(entity =>
        {
            entity.HasKey(e => e.IdVenta);

            entity.Property(e => e.IdVenta).ValueGeneratedNever();
            entity.Property(e => e.FechaCancelacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(500);

            entity.HasOne(d => d.IdVentaNavigation).WithOne(p => p.VentasCancelada)
                .HasForeignKey<VentasCancelada>(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasCanceladas_Ventas");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
