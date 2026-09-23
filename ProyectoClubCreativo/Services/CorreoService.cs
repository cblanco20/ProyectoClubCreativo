using System.Net;
using System.Net.Mail;

namespace ProyectoClubCreativo.Services
{
    public class CorreoService
    {
        private readonly IConfiguration _configuration;

        public CorreoService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCorreoAsync(
            string destinatario,
            string asunto,
            string contenidoHtml)
        {
            string? remitente =
                _configuration["Correo:Remitente"];

            string? nombreRemitente =
                _configuration["Correo:NombreRemitente"];

            string? servidorSmtp =
                _configuration["Correo:ServidorSmtp"];

            string? usuario =
                _configuration["Correo:Usuario"];

            string? contrasena =
                _configuration["Correo:Contrasena"];

            int puerto =
                _configuration.GetValue<int>("Correo:Puerto");

            if (string.IsNullOrWhiteSpace(remitente) ||
                string.IsNullOrWhiteSpace(servidorSmtp) ||
                string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(contrasena))
            {
                throw new InvalidOperationException(
                    "La configuración del correo no está completa."
                );
            }

            using MailMessage mensaje = new();

            mensaje.From = new MailAddress(
                remitente,
                nombreRemitente ?? "Club Creativo"
            );

            mensaje.To.Add(destinatario);
            mensaje.Subject = asunto;
            mensaje.Body = contenidoHtml;
            mensaje.IsBodyHtml = true;

            using SmtpClient cliente = new(
                servidorSmtp,
                puerto
            );

            cliente.EnableSsl = true;

            cliente.Credentials = new NetworkCredential(
                usuario,
                contrasena
            );

            await cliente.SendMailAsync(mensaje);
        }
    }
}