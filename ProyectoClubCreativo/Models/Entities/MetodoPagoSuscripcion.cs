namespace ProyectoClubCreativo.Models.Entities;

public partial class MetodoPagoSuscripcion
{
    public int IdMetodoPago { get; set; }

    public int IdEmprendimiento { get; set; }

    public string UltimosCuatro { get; set; } = null!;

    public int MesVencimiento { get; set; }

    public int AnioVencimiento { get; set; }

    public string EstadoSimulado { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual Emprendimiento IdEmprendimientoNavigation { get; set; } = null!;
}
