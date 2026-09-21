using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class InscripcionesEvento
{
    public int IdInscripcionEvento { get; set; }

    public int IdUsuario { get; set; }

    public int IdEvento { get; set; }

    public DateTime FechaInscripcion { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Evento IdEventoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
