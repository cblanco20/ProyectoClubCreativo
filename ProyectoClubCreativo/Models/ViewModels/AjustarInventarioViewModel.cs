using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class AjustarInventarioViewModel
    {
        [Required]
        public int IdProducto { get; set; }


        [Required(ErrorMessage = "Debe indicar el nombre del producto.")]
        public string NombreProducto { get; set; } = string.Empty;


        [Required(ErrorMessage = "Debe indicar la nueva cantidad.")]
        [Range(
            0,
            100000,
            ErrorMessage = "La cantidad debe ser igual o mayor que cero."
        )]
        [Display(Name = "Nueva cantidad")]
        public int? NuevaCantidad { get; set; }


        [StringLength(
            250,
            ErrorMessage = "La observación no puede superar los 250 caracteres."
        )]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }


        [Display(Name = "Confirmo el ajuste de inventario")]
        public bool ConfirmaAjuste { get; set; }
    }
}