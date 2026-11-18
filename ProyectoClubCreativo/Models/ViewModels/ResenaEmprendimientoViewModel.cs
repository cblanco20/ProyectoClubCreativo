using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class ResenaEmprendimientoViewModel
    {
        [Range(1, int.MaxValue)]
        public int IdEmprendimiento { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una calificación.")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5 estrellas.")]
        public byte? Valoracion { get; set; }


        [Required(ErrorMessage = "Debe escribir un comentario.")]
        [StringLength(
            1500,
            ErrorMessage = "El comentario no puede superar los 1500 caracteres."
        )]
        [Display(Name = "Comentario")]
        public string? Contenido { get; set; }
    }
}