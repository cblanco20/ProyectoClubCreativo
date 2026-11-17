using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class SolicitudesPagoSinpeAdminViewModel
    {
        public string FiltroEstado { get; set; } = "Pendiente";

        public int TotalPendientes { get; set; }

        public int TotalAprobadas { get; set; }

        public int TotalRechazadas { get; set; }


        public List<DetallePagoSinpeAdminViewModel> Solicitudes { get; set; } = new();
    }

    public class DetallePagoSinpeAdminViewModel
    {
        public int IdSolicitudPago { get; set; }

        public int IdEmprendimiento { get; set; }

        public string NombreEmprendimiento { get; set; } = string.Empty;

        public string NombrePlan { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string NumeroReferencia { get; set; } = string.Empty;

        public DateTime FechaSolicitud { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string TipoSolicitud { get; set; } = string.Empty;

        public DateOnly? FechaInicioProgramada { get; set; }

        public DateTime? FechaRevision { get; set; }

        public string? Observaciones { get; set; }
    }

    public class RechazarPagoSinpeViewModel
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "La solicitud seleccionada no es válida.")]
        public int IdSolicitudPago { get; set; }

        [Required(ErrorMessage = "Debes indicar el motivo del rechazo.")]
        [StringLength(500,
            ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
        [Display(Name = "Motivo del rechazo")]
        public string Motivo { get; set; } = string.Empty;
    }
}