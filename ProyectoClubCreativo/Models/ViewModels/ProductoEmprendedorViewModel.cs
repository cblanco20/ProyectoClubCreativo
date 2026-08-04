using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class ProductoEmprendedorViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(
            120,
            MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres."
        )]
        [Display(Name = "Nombre del producto o servicio")]
        public string Nombre { get; set; } = string.Empty;


        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(
            1000,
            MinimumLength = 30,
            ErrorMessage = "La descripción debe tener entre 30 y 1000 caracteres."
        )]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;


        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(
            0.01,
            10000000,
            ErrorMessage = "El precio debe ser mayor que cero."
        )]
        [Display(Name = "Precio")]
        public decimal? Precio { get; set; }


        [Required(ErrorMessage = "Debe seleccionar una categoría.")]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = string.Empty;


        [StringLength(
            250,
            ErrorMessage = "Las etiquetas no pueden superar los 250 caracteres."
        )]
        [Display(Name = "Etiquetas")]
        public string? Etiquetas { get; set; }


        [Required(ErrorMessage = "Debe seleccionar el tipo de publicación.")]
        [Display(Name = "Tipo")]
        public string TipoPublicacion { get; set; } = string.Empty;


        [Range(
            0,
            100000,
            ErrorMessage = "El inventario no puede ser negativo."
        )]
        [Display(Name = "Cantidad disponible")]
        public int? Inventario { get; set; }


        [Display(Name = "Imágenes")]
        public List<IFormFile>? Imagenes { get; set; }


        [Required(ErrorMessage = "Debe seleccionar un estado.")]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = string.Empty;


        [Display(Name = "Promoción asociada")]
        public string? PromocionAsociada { get; set; }


        [Display(Name = "Producto destacado")]
        public bool EsDestacado { get; set; }


        [Display(Name = "Confirmo que la información es correcta")]
        public bool ConfirmaInformacion { get; set; }
    }
}