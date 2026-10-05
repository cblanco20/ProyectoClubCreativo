using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class SeleccionPlanViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un plan de suscripción.")]
        [Display(Name = "Plan seleccionado")]
        public int? IdPlanSeleccionado { get; set; }

        [Display(Name = "Confirmo la selección del plan")]
        public bool ConfirmaSeleccion { get; set; }

        public List<PlanDisponibleViewModel> Planes { get; set; } = [];
    }

    public class PlanDisponibleViewModel
    {
        public int IdPlan { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        public string Periodicidad { get; set; } = string.Empty;

        public string Beneficios { get; set; } = string.Empty;
    }
}