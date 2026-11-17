using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class SolicitudPagoSinpeViewModel
    {
        public int IdPlan { get; set; }

        public string NombrePlan { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string Periodicidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes ingresar el número de referencia del SINPE.")]
        [StringLength(100, ErrorMessage = "La referencia no puede superar los 100 caracteres.")]
        [Display(Name = "Número de referencia SINPE")]
        public string NumeroReferencia { get; set; } = string.Empty;

        public bool ConfirmaTransferencia { get; set; }
        public bool EsRenovacion { get; set; }
    }

    public class EstadoSolicitudSinpeViewModel
    {
        public int IdSolicitudPago { get; set; }

        public string NombrePlan { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string NumeroReferencia { get; set; } = string.Empty;

        public DateTime FechaSolicitud { get; set; }

        public string Estado { get; set; } = string.Empty;

        public DateTime? FechaRevision { get; set; }

        public string? Observaciones { get; set; }
    }
}
