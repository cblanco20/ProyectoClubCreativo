using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class RespuestasEncuestum
{
    public long IdRespuestaEncuesta { get; set; }

    public int IdEncuesta { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaRespuesta { get; set; }

    public virtual Encuesta IdEncuestaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<RespuestaDetalle> RespuestaDetalles { get; set; } = new List<RespuestaDetalle>();
}
