using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class TokensRecuperacionContrasena
{
    public long IdToken { get; set; }

    public int IdUsuario { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Utilizado { get; set; }

    public DateTime? FechaUso { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
