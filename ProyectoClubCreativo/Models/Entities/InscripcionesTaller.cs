using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class InscripcionesTaller
{
    public int IdInscripcionTaller { get; set; }

    public int IdUsuario { get; set; }

    public int IdTaller { get; set; }

    public DateTime FechaInscripcion { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Tallere IdTallerNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
