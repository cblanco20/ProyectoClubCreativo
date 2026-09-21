using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Recompensa
{
    public int IdRecompensa { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int CostoPuntos { get; set; }

    public string? Icono { get; set; }

    public bool Activa { get; set; }

    public virtual ICollection<CanjesRecompensa> CanjesRecompensas { get; set; } = new List<CanjesRecompensa>();
}
