using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class MiSuscripcionViewModel
    {
        // Información de la suscripción actual
        public bool TieneSuscripcion { get; set; }

        public int? IdSuscripcion { get; set; }

        public int? IdPlan { get; set; }

        public string NombrePlan { get; set; } = string.Empty;

        public string? DescripcionPlan { get; set; }

        public decimal Precio { get; set; }

        public string Periodicidad { get; set; } = string.Empty;

        public string? Beneficios { get; set; }

        public DateOnly? FechaInicio { get; set; }

        public DateOnly? FechaFin { get; set; }

        public string Estado { get; set; } = string.Empty;

        // Estado de cancelación programada
        public bool TieneCancelacionProgramada { get; set; }

        public DateTime? FechaSolicitudCancelacion { get; set; }

        public string? MotivoCancelacionRegistrado { get; set; }


        // Historial de suscripciones
        public List<MovimientoSuscripcionViewModel> Historial { get; set; } = new();


        // Cancelación
        [Display(Name = "Motivo de cancelación")]
        [StringLength(
            400,
            ErrorMessage = "El motivo no puede superar los 400 caracteres."
        )]
        public string? MotivoCancelacion { get; set; }


        [Display(Name = "Confirmo la cancelación de la suscripción")]
        public bool ConfirmaCancelacion { get; set; }
    }


    public class MovimientoSuscripcionViewModel
    {
        public DateOnly Fecha { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string Estado { get; set; } = string.Empty;
    }


}