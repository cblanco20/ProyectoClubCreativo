using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class ContactoViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [RegularExpression(
            @"^[0-9]{8}$",
            ErrorMessage = "Ingrese un número de teléfono de 8 dígitos."
        )]
        [Display(Name = "Número de teléfono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Seleccione un asunto.")]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        [StringLength(
            1000,
            MinimumLength = 10,
            ErrorMessage = "El mensaje debe contener entre 10 y 1000 caracteres."
        )]
        public string Mensaje { get; set; } = string.Empty;
    }
}
