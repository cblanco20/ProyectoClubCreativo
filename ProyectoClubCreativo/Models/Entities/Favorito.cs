using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Favorito
{
    public long IdFavorito { get; set; }

    public int IdUsuario { get; set; }

    public int? IdProducto { get; set; }

    public int? IdEmprendimiento { get; set; }

    public int? IdEvento { get; set; }

    public int? IdTaller { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Emprendimiento? IdEmprendimientoNavigation { get; set; }

    public virtual Evento? IdEventoNavigation { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }

    public virtual Tallere? IdTallerNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
