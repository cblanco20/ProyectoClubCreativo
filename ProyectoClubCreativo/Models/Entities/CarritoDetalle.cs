using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class CarritoDetalle
{
    public int IdCarritoDetalle { get; set; }

    public int IdCarrito { get; set; }

    public int IdProducto { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public virtual Carrito IdCarritoNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
