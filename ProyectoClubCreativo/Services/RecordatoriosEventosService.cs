using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models;
using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Services
{
    // HU-48: Envía recordatorios de eventos próximos.
    public class RecordatoriosEventosService : BackgroundService
    {
        private const string TituloUnDia = "Recordatorio: evento mañana";
        private const string TituloMismoDia = "Recordatorio: evento hoy";
        private const int HoraRecordatorioDia = 7;

        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecordatoriosEventosService> _logger;
        private readonly HashSet<string> _enviados = new();
        public RecordatoriosEventosService(
            IServiceScopeFactory scopeFactory,
            ILogger<RecordatoriosEventosService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EnviarRecordatoriosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error al enviar los recordatorios de eventos.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }

        private async Task EnviarRecordatoriosAsync(CancellationToken cancelacion)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            ClubCreativoDbContext context = scope.ServiceProvider
                .GetRequiredService<ClubCreativoDbContext>();

            NotificacionService notificaciones = scope.ServiceProvider
    .GetRequiredService<NotificacionService>();

            DateTime ahora = DateTime.Now;
            DateTime limite = ahora.AddHours(24);

            List<Evento> eventos = await context.Eventos
                .Where(e =>
                    (e.Estado == ReglasTalleres.Programado ||
                     e.Estado == "Activo") &&
                    e.FechaInicio > ahora &&
                    e.FechaInicio <= limite)
                .ToListAsync(cancelacion);

            foreach (Evento evento in eventos)
            {
                bool mismoDia = evento.FechaInicio.Date == ahora.Date;

                if (mismoDia && ahora.Hour < HoraRecordatorioDia)
                {
                    continue;
                }

                string titulo = mismoDia ? TituloMismoDia : TituloUnDia;
                string marcaEvento = $"«{evento.Nombre}»";

                List<int> idsUsuarios = await context.InscripcionesEventos
                    .Where(i =>
                        i.IdEvento == evento.IdEvento &&
                        (i.Estado == "Pendiente" ||
                         i.Estado == ReglasTalleres.InscripcionConfirmada))
                    .Select(i => i.IdUsuario)
                    .Distinct()
                    .ToListAsync(cancelacion);

                if (idsUsuarios.Count == 0)
                {
                    continue;
                }

                DateTime desde = ahora.AddDays(-2);

                List<int> yaNotificados = await context.Notificaciones
                    .Where(n =>
                        idsUsuarios.Contains(n.IdUsuario) &&
                        n.Titulo == titulo &&
                        n.FechaEnvio >= desde &&
                        n.Mensaje.Contains(marcaEvento))
                    .Select(n => n.IdUsuario)
                    .Distinct()
                    .ToListAsync(cancelacion);

     

                string mensaje = mismoDia
                    ? $"Hoy es el evento {marcaEvento}. " +
                      $"Inicia a las {evento.FechaInicio:hh:mm tt}. " +
                      $"Ubicación: {evento.Ubicacion}."
                    : $"Mañana es el evento {marcaEvento}. " +
                      $"Fecha: {evento.FechaInicio:dd/MM/yyyy} a las " +
                      $"{evento.FechaInicio:hh:mm tt}. " +
                      $"Ubicación: {evento.Ubicacion}. " +
                      $"{evento.Descripcion}";

                if (mensaje.Length > 1000)
                {
                    mensaje = mensaje[..997] + "...";
                }

                foreach (int idUsuario in idsUsuarios.Except(yaNotificados))
                {
                    string clave = $"{idUsuario}|{evento.IdEvento}|{titulo}";

                    if (!_enviados.Add(clave))
                    {
                        continue;
                    }
                    await notificaciones.AgregarAsync(
                        new Notificacione
                        {
                            IdUsuario = idUsuario,
                            Titulo = titulo,
                            Mensaje = mensaje,
                            Tipo = "Evento",
                            FechaEnvio = ahora,
                            Leida = false,
                            FechaLectura = null
                        },
                        esRecordatorio: true);
                }
            }

            await context.SaveChangesAsync(cancelacion);
        }
    }
}
