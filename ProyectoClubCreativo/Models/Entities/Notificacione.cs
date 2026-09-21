using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Notificacione
{
    public long IdNotificacion { get; set; }

    public int IdUsuario { get; set; }

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public DateTime FechaEnvio { get; set; }

    public bool Leida { get; set; }

    public DateTime? FechaLectura { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
