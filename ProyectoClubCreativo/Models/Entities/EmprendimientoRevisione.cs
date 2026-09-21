using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class EmprendimientoRevisione
{
    public int IdEmprendimiento { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public virtual Emprendimiento IdEmprendimientoNavigation { get; set; } = null!;
}
