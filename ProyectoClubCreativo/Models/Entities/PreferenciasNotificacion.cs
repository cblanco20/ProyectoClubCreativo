using System;
using System.Collections.Generic;

namespace ProyectoClubCreativo.Models.Entities;

public partial class PreferenciasNotificacion
{
    public int IdUsuario { get; set; }

    public bool Compras { get; set; }

    public bool Eventos { get; set; }

    public bool Talleres { get; set; }

    public bool Promociones { get; set; }

    public bool Recordatorios { get; set; }

    public bool CanalCorreo { get; set; }

    public bool CanalPlataforma { get; set; }

    public string FrecuenciaCorreo { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
