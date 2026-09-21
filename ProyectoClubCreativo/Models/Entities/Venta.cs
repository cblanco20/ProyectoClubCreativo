using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Venta
{
    public int IdVenta { get; set; }

    public string NumeroOrden { get; set; } = null!;

    public int IdUsuario { get; set; }

    public DateTime FechaVenta { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Total { get; set; }

    public string MetodoPago { get; set; } = null!;

    public string TipoEntrega { get; set; } = null!;

    public string? DireccionEntrega { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<MovimientosPunto> MovimientosPuntos { get; set; } = new List<MovimientosPunto>();

    public virtual ICollection<UsosPromocion> UsosPromocions { get; set; } = new List<UsosPromocion>();

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();

    public virtual VentasCancelada? VentasCancelada { get; set; }
}
