using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class VentasCancelada
{
    public int IdVenta { get; set; }

    public string MotivoCancelacion { get; set; } = null!;

    public DateTime FechaCancelacion { get; set; }

    public virtual Venta IdVentaNavigation { get; set; } = null!;
}
