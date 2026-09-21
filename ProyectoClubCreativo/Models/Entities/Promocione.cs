using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Promocione
{
    public int IdPromocion { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string TipoDescuento { get; set; } = null!;

    public decimal ValorDescuento { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string Estado { get; set; } = null!;

    public int? LimiteUsos { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<UsosPromocion> UsosPromocions { get; set; } = new List<UsosPromocion>();

    public virtual ICollection<Emprendimiento> IdEmprendimientos { get; set; } = new List<Emprendimiento>();

    public virtual ICollection<Evento> IdEventos { get; set; } = new List<Evento>();

    public virtual ICollection<Producto> IdProductos { get; set; } = new List<Producto>();
}
