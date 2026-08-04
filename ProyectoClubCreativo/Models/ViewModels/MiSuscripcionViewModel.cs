using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class MiSuscripcionViewModel
    {
        [Display(Name = "Motivo de cancelación")]
        [StringLength(
            400,
            ErrorMessage = "El motivo no puede superar los 400 caracteres."
        )]
        public string? MotivoCancelacion { get; set; }


        [Display(Name = "Confirmo la cancelación de la suscripción")]
        public bool ConfirmaCancelacion { get; set; }
    }
}