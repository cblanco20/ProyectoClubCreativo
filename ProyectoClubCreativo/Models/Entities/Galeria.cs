using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Galeria
{
    public int IdGaleria { get; set; }

    public int? IdCategoria { get; set; }

    public int? IdEmprendimiento { get; set; }

    public int? IdProducto { get; set; }

    public int? IdEvento { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual GaleriaImagene? GaleriaImagene { get; set; }

    public virtual Categoria? IdCategoriaNavigation { get; set; }

    public virtual Emprendimiento? IdEmprendimientoNavigation { get; set; }

    public virtual Evento? IdEventoNavigation { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }
}
