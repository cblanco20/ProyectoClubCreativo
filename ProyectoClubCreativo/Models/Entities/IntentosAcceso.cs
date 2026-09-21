using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class IntentosAcceso
{
    public long IdIntentoAcceso { get; set; }

    public string Correo { get; set; } = null!;

    public int? IdUsuario { get; set; }

    public bool Exitoso { get; set; }

    public string? DireccionIp { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
