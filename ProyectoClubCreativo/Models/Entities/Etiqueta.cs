using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class Etiqueta
{
    public int IdEtiqueta { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activa { get; set; }

    public virtual ICollection<Emprendimiento> IdEmprendimientos { get; set; } = new List<Emprendimiento>();

    public virtual ICollection<Evento> IdEventos { get; set; } = new List<Evento>();

    public virtual ICollection<Producto> IdProductos { get; set; } = new List<Producto>();

    public virtual ICollection<Tallere> IdTallers { get; set; } = new List<Tallere>();
}
