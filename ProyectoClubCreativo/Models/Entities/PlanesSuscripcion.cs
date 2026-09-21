using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class PlanesSuscripcion
{
    public int IdPlan { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    public string Periodicidad { get; set; } = null!;

    public string? Beneficios { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<Suscripcione> Suscripciones { get; set; } = new List<Suscripcione>();
}
