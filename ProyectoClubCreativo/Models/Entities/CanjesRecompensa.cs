using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class CanjesRecompensa
{
    public long IdCanje { get; set; }

    public int IdUsuario { get; set; }

    public int IdRecompensa { get; set; }

    public int PuntosUtilizados { get; set; }

    public DateTime FechaCanje { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Recompensa IdRecompensaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
