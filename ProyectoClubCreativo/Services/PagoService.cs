namespace ProyectoClubCreativo.Services
{
    public class ResultadoPago
    {
        public bool Aprobado { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public string CodigoTransaccion { get; set; } = string.Empty;
    }

    public class PagoService
    {
        public ResultadoPago ProcesarPago(
            string numeroTarjeta,
            decimal monto)
        {
            // Tarjeta de prueba utilizada para simular
            // una transacción rechazada por la pasarela.
            if (numeroTarjeta == "4000000000000002")
            {
                return new ResultadoPago
                {
                    Aprobado = false,
                    Mensaje = "El pago fue rechazado. Verifique los datos o intente con otro método de pago."
                };
            }

            // Las demás tarjetas válidas simulan
            // una transacción aprobada.
            return new ResultadoPago
            {
                Aprobado = true,
                Mensaje = "Pago aprobado.",
                CodigoTransaccion = Guid.NewGuid()
                    .ToString("N")
                    .ToUpper()
            };
        }
    }
}