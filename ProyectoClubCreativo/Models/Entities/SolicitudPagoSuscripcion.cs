using System;

namespace ProyectoClubCreativo.Models.Entities
{
    public partial class SolicitudPagoSuscripcion
    {
        public int IdSolicitudPago { get; set; }

        public int IdEmprendimiento { get; set; }

        public int IdPlan { get; set; }

        public decimal Monto { get; set; }

        public string NumeroReferencia { get; set; } = string.Empty;

        public DateTime FechaSolicitud { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public DateTime? FechaRevision { get; set; }

        public int? IdAdministradorRevision { get; set; }

        public string? Observaciones { get; set; }

        public int? IdSuscripcionGenerada { get; set; }

        public string TipoSolicitud { get; set; } = "Nueva";

        public DateOnly? FechaInicioProgramada { get; set; }

        public int? IdSuscripcionAnterior { get; set; }

        public virtual Emprendimiento IdEmprendimientoNavigation { get; set; } = null!;

        public virtual PlanesSuscripcion IdPlanNavigation { get; set; } = null!;

        public virtual Suscripcione? IdSuscripcionGeneradaNavigation { get; set; }
    }
}