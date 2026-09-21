using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class RegistrosAsistencium
{
    public long IdAsistencia { get; set; }

    public int IdUsuario { get; set; }

    public int? IdEvento { get; set; }

    public int? IdTaller { get; set; }

    public string TipoAsistencia { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public bool RegistradoPorQr { get; set; }

    public string? Observacion { get; set; }

    public virtual Evento? IdEventoNavigation { get; set; }

    public virtual Tallere? IdTallerNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<MovimientosPunto> MovimientosPuntos { get; set; } = new List<MovimientosPunto>();
}
