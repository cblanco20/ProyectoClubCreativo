using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Auditorium
{
    public long IdAuditoria { get; set; }

    public int? IdUsuario { get; set; }

    public string Modulo { get; set; } = null!;

    public string Accion { get; set; } = null!;

    public string? Entidad { get; set; }

    public string? IdRegistro { get; set; }

    public string? Descripcion { get; set; }

    public string? DireccionIp { get; set; }

    public string Nivel { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
