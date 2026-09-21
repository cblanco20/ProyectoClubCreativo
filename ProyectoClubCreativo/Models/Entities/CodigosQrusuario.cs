using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class CodigosQrusuario
{
    public int IdCodigoQr { get; set; }

    public int IdUsuario { get; set; }

    public Guid Codigo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public bool Activo { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
