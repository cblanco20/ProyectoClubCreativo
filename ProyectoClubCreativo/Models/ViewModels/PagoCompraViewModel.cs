
using System.ComponentModel.DataAnnotations;

namespace ProyectoClubCreativo.Models.ViewModels
{
    public class PagoCompraViewModel
    {
        // Resumen de la compra
        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal TotalPagar { get; set; }

        // Datos de la tarjeta
        [Required(ErrorMessage = "Debe ingresar el nombre del titular.")]
        [Display(Name = "Nombre del titular")]
        [StringLength(100,
            ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string NombreTitular { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar el número de tarjeta.")]
        [Display(Name = "Número de tarjeta")]
        [RegularExpression(
            @"^\d{16}$",
            ErrorMessage = "El número de tarjeta debe contener 16 dígitos.")]
        public string NumeroTarjeta { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar el mes de vencimiento.")]
        [Range(1, 12,
            ErrorMessage = "El mes de vencimiento no es válido.")]
        [Display(Name = "Mes")]
        public int? MesVencimiento { get; set; }

        [Required(ErrorMessage = "Debe ingresar el año de vencimiento.")]
        [Display(Name = "Año")]
        public int? AnioVencimiento { get; set; }

        [Required(ErrorMessage = "Debe ingresar el código de seguridad.")]
        [Display(Name = "CVV")]
        [RegularExpression(
            @"^\d{3,4}$",
            ErrorMessage = "El CVV debe contener 3 o 4 dígitos.")]
        public string Cvv { get; set; } = string.Empty;
    }
}
