using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class SeleccionPlanViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un plan de suscripción.")]
        [Display(Name = "Plan seleccionado")]
        public string PlanSeleccionado { get; set; } = string.Empty;


        [Display(Name = "Confirmo la selección del plan")]
        public bool ConfirmaSeleccion { get; set; }
    }
}