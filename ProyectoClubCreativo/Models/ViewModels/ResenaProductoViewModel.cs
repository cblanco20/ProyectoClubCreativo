using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class ResenaProductoViewModel
    {
        [Required]
        public int IdProducto { get; set; }

        [Required]
        public string NumeroOrden { get; set; } = string.Empty;

        [Required(
            ErrorMessage =
                "Debe seleccionar una calificación antes de enviar el comentario."
        )]
        [Range(
            1,
            5,
            ErrorMessage = "La calificación debe estar entre 1 y 5 estrellas."
        )]
        [Display(Name = "Calificación")]
        public byte? Valoracion { get; set; }

        [StringLength(
            1500,
            ErrorMessage = "El comentario no puede superar los 1500 caracteres."
        )]
        [Display(Name = "Comentario")]
        public string? Contenido { get; set; }
    }
}