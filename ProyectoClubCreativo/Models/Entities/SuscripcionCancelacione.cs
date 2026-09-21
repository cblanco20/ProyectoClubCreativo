using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class SuscripcionCancelacione
{
    public int IdSuscripcion { get; set; }

    public string MotivoCancelacion { get; set; } = null!;

    public DateTime FechaCancelacion { get; set; }

    public virtual Suscripcione IdSuscripcionNavigation { get; set; } = null!;
}
