using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class PerfilUsuarioViewModel
    {
        public int IdUsuario { get; set; }

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

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(
            @"^[0-9]{8}$",
            ErrorMessage = "Ingrese un número de teléfono de 8 dígitos."
        )]
        [Display(Name = "Número de teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Display(Name = "Provincia")]
        [Required(ErrorMessage = "Seleccione una provincia.")]
        public string Provincia { get; set; } = string.Empty;

        [Display(Name = "Fecha de nacimiento")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        public DateTime? FechaNacimiento { get; set; }

        public string FotoActual { get; set; } = string.Empty;

        [Display(Name = "Fotografía de perfil")]
        public IFormFile? Fotografia { get; set; }

        [DataType(DataType.Password)]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "La nueva contraseña debe contener al menos 8 caracteres."
        )]
        [Display(Name = "Nueva contraseña")]
        public string? NuevaContrasena { get; set; }

        [DataType(DataType.Password)]
        [Compare(
            nameof(NuevaContrasena),
            ErrorMessage = "Las contraseñas no coinciden."
        )]
        [Display(Name = "Confirmar contraseña")]
        public string? ConfirmarContrasena { get; set; }

        [Display(Name = "Notificaciones de compras")]
        public bool NotificacionesCompras { get; set; }

        [Display(Name = "Notificaciones de eventos")]
        public bool NotificacionesEventos { get; set; }

        [Display(Name = "Notificaciones de talleres")]
        public bool NotificacionesTalleres { get; set; }

        [Display(Name = "Promociones y campañas")]
        public bool NotificacionesPromociones { get; set; }

        [Display(Name = "Correo electrónico")]
        public bool CanalCorreo { get; set; }

        [Display(Name = "Notificaciones dentro de la plataforma")]
        public bool CanalPlataforma { get; set; }
    }
}
