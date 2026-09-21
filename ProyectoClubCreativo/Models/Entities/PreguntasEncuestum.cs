using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class PreguntasEncuestum
{
    public int IdPregunta { get; set; }

    public int IdEncuesta { get; set; }

    public string Texto { get; set; } = null!;

    public string TipoPregunta { get; set; } = null!;

    public bool Obligatoria { get; set; }

    public short OrdenVisual { get; set; }

    public virtual Encuesta IdEncuestaNavigation { get; set; } = null!;

    public virtual ICollection<OpcionesPreguntum> OpcionesPregunta { get; set; } = new List<OpcionesPreguntum>();

    public virtual ICollection<RespuestaDetalle> RespuestaDetalles { get; set; } = new List<RespuestaDetalle>();
}
