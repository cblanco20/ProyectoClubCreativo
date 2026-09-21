using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string ApellidoPaterno { get; set; } = null!;

    public string? ApellidoMaterno { get; set; }

    public string Correo { get; set; } = null!;

    public string? Telefono { get; set; }

    public int? IdProvincia { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public string ContrasenaHash { get; set; } = null!;

    public string? FotoPerfilUrl { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public virtual ICollection<Auditorium> Auditoria { get; set; } = new List<Auditorium>();

    public virtual ICollection<CanjesRecompensa> CanjesRecompensas { get; set; } = new List<CanjesRecompensa>();

    public virtual Carrito? Carrito { get; set; }

    public virtual CodigosQrusuario? CodigosQrusuario { get; set; }

    public virtual ICollection<ComentariosResena> ComentariosResenas { get; set; } = new List<ComentariosResena>();

    public virtual ICollection<Emprendimiento> Emprendimientos { get; set; } = new List<Emprendimiento>();

    public virtual ICollection<Encuesta> Encuesta { get; set; } = new List<Encuesta>();

    public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();

    public virtual Provincia? IdProvinciaNavigation { get; set; }

    public virtual ICollection<InscripcionesEvento> InscripcionesEventos { get; set; } = new List<InscripcionesEvento>();

    public virtual ICollection<InscripcionesTaller> InscripcionesTallers { get; set; } = new List<InscripcionesTaller>();

    public virtual ICollection<IntentosAcceso> IntentosAccesos { get; set; } = new List<IntentosAcceso>();

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();

    public virtual ICollection<MovimientosPunto> MovimientosPuntos { get; set; } = new List<MovimientosPunto>();

    public virtual ICollection<Noticia> Noticia { get; set; } = new List<Noticia>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual PreferenciasNotificacion? PreferenciasNotificacion { get; set; }

    public virtual ICollection<RegistrosAsistencium> RegistrosAsistencia { get; set; } = new List<RegistrosAsistencium>();

    public virtual ICollection<RespuestasEncuestum> RespuestasEncuesta { get; set; } = new List<RespuestasEncuestum>();

    public virtual ICollection<TokensRecuperacionContrasena> TokensRecuperacionContrasenas { get; set; } = new List<TokensRecuperacionContrasena>();

    public virtual ICollection<UsosPromocion> UsosPromocions { get; set; } = new List<UsosPromocion>();

    public virtual ICollection<UsuarioRole> UsuarioRoles { get; set; } = new List<UsuarioRole>();

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
