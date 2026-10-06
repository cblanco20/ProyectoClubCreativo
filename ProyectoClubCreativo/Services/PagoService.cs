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

        public ResultadoPago ProcesarRenovacionAutomatica(
    string estadoSimulado,
    decimal monto)
        {
            // Simula el cobro automático utilizando
            // el estado del método de pago guardado.
            if (!estadoSimulado.Equals(
                "Aprobado",
                StringComparison.OrdinalIgnoreCase))
            {
                return new ResultadoPago
                {
                    Aprobado = false,
                    Mensaje =
                        "No fue posible procesar la renovación automática. " +
                        "Debe actualizar su método de pago."
                };
            }

            return new ResultadoPago
            {
                Aprobado = true,
                Mensaje = "Renovación automática aprobada.",
                CodigoTransaccion = Guid.NewGuid()
                    .ToString("N")
                    .ToUpper()
            };
        }
    }
}