using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class SolicitudEventoViewModel
    {
        [Required]
        public int IdEvento { get; set; }


        [Required(ErrorMessage = "Debe indicar el nombre del evento.")]
        [Display(Name = "Evento")]
        public string NombreEvento { get; set; } = string.Empty;


        [Required(ErrorMessage = "Debe seleccionar el tipo de espacio.")]
        [Display(Name = "Tipo de espacio")]
        public string TipoEspacio { get; set; } = string.Empty;


        [Required(ErrorMessage = "Debe explicar qué productos presentará.")]
        [StringLength(
            500,
            MinimumLength = 20,
            ErrorMessage = "La descripción debe tener entre 20 y 500 caracteres."
        )]
        [Display(Name = "Productos o servicios que presentará")]
        public string ProductosPresentados { get; set; } = string.Empty;


        [StringLength(
            400,
            ErrorMessage = "Las necesidades especiales no pueden superar los 400 caracteres."
        )]
        [Display(Name = "Necesidades especiales")]
        public string? NecesidadesEspeciales { get; set; }


        [Display(Name = "Requiere conexión eléctrica")]
        public bool RequiereElectricidad { get; set; }


        [Display(Name = "Requiere mesa")]
        public bool RequiereMesa { get; set; }


        [Display(Name = "Confirmo la solicitud")]
        public bool ConfirmaSolicitud { get; set; }
    }
}