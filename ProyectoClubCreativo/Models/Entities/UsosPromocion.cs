using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class UsosPromocion
{
    public long IdUsoPromocion { get; set; }

    public int IdPromocion { get; set; }

    public int IdUsuario { get; set; }

    public int IdVenta { get; set; }

    public decimal MontoDescuento { get; set; }

    public DateTime FechaUso { get; set; }

    public virtual Promocione IdPromocionNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual Venta IdVentaNavigation { get; set; } = null!;
}
