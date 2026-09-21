using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class MensajesContacto
{
    public long IdMensajeContacto { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string? Telefono { get; set; }

    public string Asunto { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime FechaEnvio { get; set; }
}
