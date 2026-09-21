using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Suscripcione
{
    public int IdSuscripcion { get; set; }

    public int IdEmprendimiento { get; set; }

    public int IdPlan { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Emprendimiento IdEmprendimientoNavigation { get; set; } = null!;

    public virtual PlanesSuscripcion IdPlanNavigation { get; set; } = null!;

    public virtual SuscripcionCancelacione? SuscripcionCancelacione { get; set; }
}
