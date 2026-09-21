using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class ComentariosResena
{
    public long IdComentario { get; set; }

    public int IdUsuario { get; set; }

    public int? IdProducto { get; set; }

    public int? IdEmprendimiento { get; set; }

    public int? IdEvento { get; set; }

    public string Contenido { get; set; } = null!;

    public byte? Valoracion { get; set; }

    public string Estado { get; set; } = null!;

    public int CantidadReportes { get; set; }

    public DateTime FechaPublicacion { get; set; }

    public DateTime? FechaEdicion { get; set; }

    public virtual Emprendimiento? IdEmprendimientoNavigation { get; set; }

    public virtual Evento? IdEventoNavigation { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
