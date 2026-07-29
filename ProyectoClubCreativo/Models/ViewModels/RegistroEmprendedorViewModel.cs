using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class RegistroEmprendedorViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100, MinimumLength = 2)]
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

        [Required(ErrorMessage = "El nombre del emprendimiento es obligatorio.")]
        [StringLength(120, MinimumLength = 2)]
        [Display(Name = "Nombre del emprendimiento")]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [Url(ErrorMessage = "Ingrese un enlace válido.")]
        [Display(Name = "Instagram o página web")]
        public string? SitioWeb { get; set; }

        [Required(ErrorMessage = "Seleccione una categoría.")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "Describa brevemente el emprendimiento.")]
        [StringLength(
            600,
            MinimumLength = 20,
            ErrorMessage = "La descripción debe contener entre 20 y 600 caracteres."
        )]
        public string Descripcion { get; set; } = string.Empty;

        public bool AceptaTerminos { get; set; }
    }
}