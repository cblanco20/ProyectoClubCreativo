using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class UsuarioRole
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
