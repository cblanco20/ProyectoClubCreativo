using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class ActualizarVentaViewModel
    {
        [Required]
        public int IdVenta { get; set; }


        [Required(ErrorMessage = "Debe indicar el número de orden.")]
        [Display(Name = "Número de orden")]
        public string NumeroOrden { get; set; } = string.Empty;


        [Required(ErrorMessage = "Debe seleccionar el nuevo estado.")]
        [Display(Name = "Nuevo estado")]
        public string NuevoEstado { get; set; } = string.Empty;


        [StringLength(
            300,
            ErrorMessage = "La observación no puede superar los 300 caracteres."
        )]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }


        [Display(Name = "Confirmo el cambio de estado")]
        public bool ConfirmaCambio { get; set; }
    }
}