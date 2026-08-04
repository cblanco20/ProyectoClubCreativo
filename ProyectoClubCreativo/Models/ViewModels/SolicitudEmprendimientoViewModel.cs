using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class SolicitudEmprendimientoViewModel
    {
        [Required(ErrorMessage = "El nombre comercial es obligatorio.")]
        [StringLength(100, MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        [Display(Name = "Nombre comercial")]
        public string NombreComercial { get; set; } = string.Empty;


        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(800, MinimumLength = 30,
            ErrorMessage = "La descripción debe tener entre 30 y 800 caracteres.")]
        [Display(Name = "Descripción del emprendimiento")]
        public string Descripcion { get; set; } = string.Empty;


        [Required(ErrorMessage = "Debe seleccionar una categoría.")]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = string.Empty;


        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [RegularExpression(
            @"^[0-9-]{9,15}$",
            ErrorMessage = "Ingrese una cédula válida utilizando números y guiones."
        )]
        [Display(Name = "Cédula física o jurídica")]
        public string Cedula { get; set; } = string.Empty;


        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(
            @"^[2678]\d{7}$",
            ErrorMessage = "Ingrese un teléfono válido de 8 dígitos."
        )]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;


        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;


        [Url(ErrorMessage = "Ingrese una dirección web válida.")]
        [Display(Name = "Sitio web")]
        public string? SitioWeb { get; set; }


        [Url(ErrorMessage = "Ingrese una dirección válida de Instagram.")]
        [Display(Name = "Instagram")]
        public string? Instagram { get; set; }


        [Url(ErrorMessage = "Ingrese una dirección válida de Facebook.")]
        [Display(Name = "Facebook")]
        public string? Facebook { get; set; }


        [Display(Name = "Logo del emprendimiento")]
        public IFormFile? Logo { get; set; }


        [Display(Name = "Fotografías del emprendimiento")]
        public List<IFormFile>? Fotografias { get; set; }


        [Display(Name = "Deseo participar en Club Creativo")]
        public bool ParticipaClubCreativo { get; set; }


        [Display(Name = "Deseo participar en Hecho en CR")]
        public bool ParticipaHechoEnCr { get; set; }


        [Required(ErrorMessage = "Debe indicar cómo desea participar.")]
        [StringLength(600, MinimumLength = 20,
            ErrorMessage = "La información debe tener entre 20 y 600 caracteres.")]
        [Display(Name = "Información de participación")]
        public string InformacionParticipacion { get; set; } = string.Empty;

        [Display(Name = "Confirmo que la información es correcta")]
        public bool ConfirmaInformacion { get; set; }
    }
}