using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class RegistroUsuarioViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(
            80,
            MinimumLength = 2,
            ErrorMessage = "El nombre debe contener entre 2 y 80 caracteres."
        )]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "Los apellidos deben contener entre 2 y 100 caracteres."
        )]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(
            @"^[0-9]{8}$",
            ErrorMessage = "Ingrese un número de teléfono de 8 dígitos."
        )]
        [Display(Name = "Número de teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione una provincia.")]
        public string Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "La contraseña debe contener al menos 8 caracteres."
        )]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(Contrasena),
            ErrorMessage = "Las contraseñas no coinciden."
        )]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        public bool AceptaTerminos { get; set; }
    }
}
