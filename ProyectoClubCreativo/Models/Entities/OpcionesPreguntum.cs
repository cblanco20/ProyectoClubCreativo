using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class OpcionesPreguntum
{
    public int IdOpcion { get; set; }

    public int IdPregunta { get; set; }

    public string Texto { get; set; } = null!;

    public short OrdenVisual { get; set; }

    public virtual PreguntasEncuestum IdPreguntaNavigation { get; set; } = null!;

    public virtual ICollection<RespuestaDetalle> RespuestaDetalles { get; set; } = new List<RespuestaDetalle>();
}
