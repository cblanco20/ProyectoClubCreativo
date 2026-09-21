using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class MovimientosPunto
{
    public long IdMovimientoPuntos { get; set; }

    public int IdUsuario { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public int Puntos { get; set; }

    public string Motivo { get; set; } = null!;

    public int? IdVenta { get; set; }

    public long? IdAsistencia { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public virtual RegistrosAsistencium? IdAsistenciaNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual Venta? IdVentaNavigation { get; set; }
}
