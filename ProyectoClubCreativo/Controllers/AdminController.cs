using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models;
using ProyectoClubCreativo.Models.Entities;
using ProyectoClubCreativo.Models.ViewModels;
using ProyectoClubCreativo.Models.ViewModels.Admin;
using ProyectoClubCreativo.Services;
using QRCoder;
using ClosedXML.Excel;

namespace ProyectoClubCreativo.Controllers
{
    public class AdminController : Controller
    {

        private readonly IWebHostEnvironment _entornoWeb;
        private readonly ClubCreativoDbContext _context;
        private readonly CorreoService _correoService;
        private readonly NotificacionService _notificaciones;
        private readonly ActualizacionSuscripcionesService _actualizacionSuscripciones;
        private readonly PromocionesService _promociones;
        private readonly AuditoriaService _auditoria;

        public AdminController(
            IWebHostEnvironment entornoWeb,
            ClubCreativoDbContext context,
            CorreoService correoService,
            NotificacionService notificaciones,
            ActualizacionSuscripcionesService actualizacionSuscripciones,
            PromocionesService promociones,
            AuditoriaService auditoria)
        {
            _auditoria = auditoria;
            _promociones = promociones;
            _entornoWeb = entornoWeb;
            _context = context;
            _correoService = correoService;
            _notificaciones = notificaciones;
            _actualizacionSuscripciones = actualizacionSuscripciones;
        }

        public override void OnActionExecuting(
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

            if (idUsuario is null)
            {
                context.Result = RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );

                return;
            }

            if (rolUsuario != "Administrador")
            {
                context.Result = RedirectToAction(
                    "AccesoDenegado",
                    "Cuenta"
                );

                return;
            }

            base.OnActionExecuting(context);
        }

        public override async Task OnActionExecutionAsync(
    Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context,
    Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        {
            OnActionExecuting(context);

            if (context.Result is not null)
            {
                if (context.Result is RedirectToActionResult { ActionName: "AccesoDenegado" })
                {
                    string nombreCuenta = HttpContext.Session.GetString("NombreUsuario") ?? "Una cuenta";
                    string rolCuenta = HttpContext.Session.GetString("RolUsuario") ?? "sin rol";
                    string rutaBloqueada = $"{context.RouteData.Values["controller"]}/{context.RouteData.Values["action"]}";

                    try
                    {
                        await _auditoria.RegistrarAlertaAsync(
                            "AccesoNoAutorizado",
                            $"{nombreCuenta} (rol {rolCuenta}) intentó acceder a la sección restringida {rutaBloqueada}.",
                            "Advertencia");
                    }
                    catch (Exception)
                    {
                    }
                }

                return;
            }

            Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext ejecutado = await next();

            if (ejecutado.Exception is not null ||
                !HttpMethods.IsPost(Request.Method) ||
                ejecutado.Result is not (RedirectToActionResult or RedirectResult) ||
                TempData.Peek("ErrorAdmin") is not null)
            {
                return;
            }

            string accion = context.ActionDescriptor.RouteValues["action"] ?? "Desconocida";

            if (accion == nameof(EliminarComentario))
            {
                return;
            }

            string? idRegistro = context.RouteData.Values["id"]?.ToString();

            if (string.IsNullOrEmpty(idRegistro) && Request.HasFormContentType)
            {
                idRegistro = Request.Form["id"].FirstOrDefault();
            }

            string nombre = HttpContext.Session.GetString("NombreUsuario") ?? "El administrador";

            string descripcion = string.IsNullOrEmpty(idRegistro)
                ? $"{nombre} ejecutó la acción {accion}."
                : $"{nombre} ejecutó la acción {accion} sobre el registro {idRegistro}.";

            try
            {
                await _auditoria.RegistrarAsync(
                    "Administración",
                    accion,
                    null,
                    idRegistro,
                    descripcion,
                    accion.StartsWith("Eliminar") ? "Advertencia" : "Informativo");
            }
            catch (Exception)
            {
            }
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "IniciarSesion",
                "Cuenta"
            );
        }

        [HttpGet]
        public async Task<IActionResult> Panel(FiltroDashboardViewModel filtro)
        {
            DateTime ahora = DateTime.Now;
            // HU-60: Validar el período seleccionado.
            if (filtro.FechaInicio.HasValue &&
                filtro.FechaFin.HasValue &&
                filtro.FechaInicio.Value.Date > filtro.FechaFin.Value.Date)
            {
                ModelState.AddModelError(
                    "Filtro.FechaFin",
                    "La fecha final no puede ser anterior a la fecha inicial.");

                // No aplicar un período inválido a las consultas.
                filtro.FechaInicio = null;
                filtro.FechaFin = null;
            }

            // HU-63: Consultar eventos según el filtro seleccionado.
            var consultaEventos = _context.Eventos
                .AsNoTracking()
                .AsQueryable();

            if (filtro.Evento.HasValue)
            {
                // Mostrar el evento seleccionado, incluso si ya finalizó.
                int idEvento = filtro.Evento.Value;

                consultaEventos = consultaEventos
                    .Where(e => e.IdEvento == idEvento);
            }
            else
            {
                // Sin filtro, mostrar solamente los próximos eventos.
                consultaEventos = consultaEventos
                    .Where(e =>
                        (e.Estado == "Programado" || e.Estado == "Activo") &&
                        e.FechaInicio >= ahora);
            }

            var eventos = await consultaEventos
                .OrderBy(e => e.FechaInicio)
                .Take(3)
                .Select(e => new
                {
                    e.IdEvento,
                    e.Nombre,
                    e.FechaInicio,
                    e.Ubicacion,
                    e.CupoMaximo,
                    Inscritos = e.InscripcionesEventos
                        .Count(i => i.Estado != "Cancelada")
                })
                .ToListAsync();

            var talleresProgramados = await _context.Talleres
                .AsNoTracking()
                .Where(t =>
                    t.Estado == ReglasTalleres.Programado &&
                    t.FechaInicio != null &&
                    t.FechaInicio >= ahora)
                .OrderBy(t => t.FechaInicio)
                .Select(t => new
                {
                    t.IdTaller,
                    t.Nombre,
                    FechaInicio = t.FechaInicio!.Value,
                    t.CupoMaximo,
                    Inscritos = t.InscripcionesTallers
                        .Count(i => i.Estado != "Cancelada")
                })
                .ToListAsync();

            var registrosTaller = await _context.InscripcionesTallers
                .AsNoTracking()
                .OrderByDescending(i => i.FechaInscripcion)
                .Take(4)
                .Select(i => new
                {
                    i.FechaInscripcion,
                    i.Estado,
                    Persona = i.IdUsuarioNavigation.Nombre + " " + i.IdUsuarioNavigation.ApellidoPaterno,
                    Actividad = i.IdTallerNavigation.Nombre
                })
                .ToListAsync();

            // HU-63: Consultar inscripciones de eventos.
            var consultaRegistrosEvento = _context.InscripcionesEventos
                .AsNoTracking()
                .AsQueryable();

            if (filtro.Evento.HasValue)
            {
                int idEvento = filtro.Evento.Value;

                consultaRegistrosEvento = consultaRegistrosEvento
                    .Where(i => i.IdEvento == idEvento);
            }

            var registrosEvento = await consultaRegistrosEvento
                .OrderByDescending(i => i.FechaInscripcion)
                .Take(4)
                .Select(i => new
                {
                    i.FechaInscripcion,
                    i.Estado,
                    Persona = i.IdUsuarioNavigation.Nombre + " " +
                              i.IdUsuarioNavigation.ApellidoPaterno,
                    Actividad = i.IdEventoNavigation.Nombre
                })
                .ToListAsync();

            List<AlertaAdminViewModel> alertas = talleresProgramados
                .Where(t => t.CupoMaximo > 0 &&
                            t.CupoMaximo - t.Inscritos <= ReglasTalleres.UmbralUltimosCupos)
                .Select(t => new AlertaAdminViewModel
                {
                    Tipo = "Cupos",
                    Mensaje = t.CupoMaximo - t.Inscritos <= 0
                        ? $"El taller \"{t.Nombre}\" alcanzó cupo lleno."
                        : $"El taller \"{t.Nombre}\" tiene los últimos cupos disponibles.",
                    Fecha = t.FechaInicio.ToString("dd/MM/yyyy"),
                    Icono = "bi-people-fill",
                    Nivel = t.CupoMaximo - t.Inscritos <= 0 ? "informativa" : "advertencia"
                })
                .ToList();

            // HU-64: Alertas de eventos con pocos cupos disponibles.
            var eventosConPocosCupos = await _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    (e.Estado == "Programado" || e.Estado == "Activo") &&
                    e.FechaInicio >= ahora &&
                    e.CupoMaximo.HasValue &&
                    e.CupoMaximo.Value > 0)
                .Select(e => new
                {
                    e.Nombre,
                    e.FechaInicio,
                    CuposDisponibles = e.CupoMaximo!.Value -
                        e.InscripcionesEventos.Count(i => i.Estado != "Cancelada")
                })
                .Where(e =>
                    e.CuposDisponibles <= ReglasTalleres.UmbralUltimosCupos)
                .OrderBy(e => e.FechaInicio)
                .ToListAsync();

            foreach (var evento in eventosConPocosCupos)
            {
                alertas.Add(new AlertaAdminViewModel
                {
                    Tipo = "Cupos",
                    Mensaje = evento.CuposDisponibles <= 0
                        ? $"El evento \"{evento.Nombre}\" alcanzó cupo lleno."
                        : $"El evento \"{evento.Nombre}\" tiene los últimos {evento.CuposDisponibles} cupos disponibles.",
                    Fecha = evento.FechaInicio.ToString("dd/MM/yyyy"),
                    Icono = "bi-calendar-event",
                    Nivel = evento.CuposDisponibles <= 0
                        ? "informativa"
                        : "advertencia"
                });
            }

            // HU-64: Alertas de solicitudes pendientes de aprobación.

            int emprendimientosPendientes = await _context.Emprendimientos
                .CountAsync(e => e.EstadoAprobacion == "Pendiente");

            if (emprendimientosPendientes > 0)
            {
                alertas.Add(new AlertaAdminViewModel
                {
                    Tipo = "Emprendimientos",
                    Mensaje = emprendimientosPendientes == 1
                        ? "Hay 1 emprendimiento pendiente de aprobación."
                        : $"Hay {emprendimientosPendientes} emprendimientos pendientes de aprobación.",
                    Fecha = ahora.ToString("dd/MM/yyyy"),
                    Icono = "bi-shop",
                    Nivel = "advertencia"
                });
            }

            // HU-64: Alertas de pagos de suscripciones pendientes.

            int pagosPendientes = await _context.SolicitudesPagoSuscripcion
                .CountAsync(s => s.Estado == "Pendiente");

            if (pagosPendientes > 0)
            {
                alertas.Add(new AlertaAdminViewModel
                {
                    Tipo = "Suscripciones",
                    Mensaje = pagosPendientes == 1
                        ? "Hay 1 solicitud de pago de suscripción pendiente de revisión."
                        : $"Hay {pagosPendientes} solicitudes de pago de suscripción pendientes de revisión.",
                    Fecha = ahora.ToString("dd/MM/yyyy"),
                    Icono = "bi-credit-card",
                    Nivel = "advertencia"
                });
            }
            int idAdministradorActual = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            int alertasSeguridadPendientes = await _context.Notificaciones.CountAsync(n =>
                n.IdUsuario == idAdministradorActual &&
                n.Tipo == "Seguridad" &&
                !n.Leida);

            if (alertasSeguridadPendientes > 0)
            {
                alertas.Insert(0, new AlertaAdminViewModel
                {
                    Tipo = "Seguridad",
                    Mensaje = alertasSeguridadPendientes == 1
                        ? "Hay 1 alerta de seguridad sin revisar. Consulte Seguridad y auditoría."
                        : $"Hay {alertasSeguridadPendientes} alertas de seguridad sin revisar. Consulte Seguridad y auditoría.",
                    Fecha = ahora.ToString("dd/MM/yyyy"),
                    Icono = "bi-shield-exclamation",
                    Nivel = "critica"
                });
            }

            // HU-63 y HU-64: Actividad reciente según el evento seleccionado.
            var movimientosTaller = filtro.Evento.HasValue
                ? registrosTaller.Take(0)
                : registrosTaller;

            List<ActividadAdminViewModel> actividad = movimientosTaller
                .Select(r => (
                    r.FechaInscripcion,
                    r.Estado,
                    r.Persona,
                    r.Actividad,
                    Tipo: "taller"
                ))
                .Concat(registrosEvento.Select(r => (
                    r.FechaInscripcion,
                    r.Estado,
                    r.Persona,
                    r.Actividad,
                    Tipo: "evento"
                )))
                .OrderByDescending(r => r.FechaInscripcion)
                .Take(4)
                .Select(r => new ActividadAdminViewModel
                {
                    Mensaje = r.Estado == ReglasTalleres.InscripcionCancelada
                        ? $"{r.Persona} canceló su registro en el {r.Tipo} \"{r.Actividad}\"."
                        : $"{r.Persona} se registró en el {r.Tipo} \"{r.Actividad}\".",
                    Fecha = r.FechaInscripcion.ToString("dd/MM/yyyy hh:mm tt"),
                    Icono = "bi-person-plus-fill"
                })
                .ToList();

            // HU-60: Ventas reales con pago confirmado.
            // Se consulta la tabla Ventas para no duplicar órdenes
            // que contienen varios productos.

            var ventasConfirmadas = _context.Ventas
                .AsNoTracking()
                .Where(v =>
                    v.Estado != "Cancelada" &&
                    (v.EstadoPago == "Aprobado" ||
                     v.Estado == "Entregada"));

            // HU-60: Aplicar filtros de fechas a las ventas confirmadas.
            if (filtro.FechaInicio.HasValue)
            {
                DateTime fechaInicio = filtro.FechaInicio.Value.Date;

                ventasConfirmadas = ventasConfirmadas
                    .Where(v => v.FechaVenta >= fechaInicio);
            }

            if (filtro.FechaFin.HasValue)
            {
                DateTime fechaFinExclusiva = filtro.FechaFin.Value.Date.AddDays(1);

                ventasConfirmadas = ventasConfirmadas
                    .Where(v => v.FechaVenta < fechaFinExclusiva);
            }


            // HU-63: Calcular ventas confirmadas según los filtros.
            decimal totalVentas;
            List<PuntoGraficoViewModel> ventasPorMes;

            if (filtro.Emprendimiento.HasValue)
            {
                // Si se selecciona un emprendimiento, calcular únicamente
                // los productos que le corresponden dentro de cada venta.
                int idEmprendimiento = filtro.Emprendimiento.Value;

                var detallesConfirmados = _context.VentaDetalles
                    .AsNoTracking()
                    .Where(d =>
                        d.IdProductoNavigation.IdEmprendimiento == idEmprendimiento &&
                        d.IdVentaNavigation.Estado != "Cancelada" &&
                        (d.IdVentaNavigation.EstadoPago == "Aprobado" ||
                         d.IdVentaNavigation.Estado == "Entregada"));

                // Aplicar también los filtros de fechas.
                if (filtro.FechaInicio.HasValue)
                {
                    DateTime fechaInicio = filtro.FechaInicio.Value.Date;

                    detallesConfirmados = detallesConfirmados
                        .Where(d => d.IdVentaNavigation.FechaVenta >= fechaInicio);
                }

                if (filtro.FechaFin.HasValue)
                {
                    DateTime fechaFinExclusiva = filtro.FechaFin.Value.Date.AddDays(1);

                    detallesConfirmados = detallesConfirmados
                        .Where(d => d.IdVentaNavigation.FechaVenta < fechaFinExclusiva);
                }

                // Total correspondiente al emprendimiento seleccionado.
                totalVentas = await detallesConfirmados
                    .SumAsync(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0m;

                // Ventas del emprendimiento agrupadas por mes.
                var ventasEmprendimiento = await detallesConfirmados
                    .GroupBy(d => new
                    {
                        d.IdVentaNavigation.FechaVenta.Year,
                        d.IdVentaNavigation.FechaVenta.Month
                    })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Total = g.Sum(d => d.Cantidad * d.PrecioUnitario)
                    })
                    .OrderBy(g => g.Year)
                    .ThenBy(g => g.Month)
                    .ToListAsync();

                ventasPorMes = ventasEmprendimiento
                    .Select(v => new PuntoGraficoViewModel
                    {
                        Etiqueta = $"{v.Month:00}/{v.Year}",
                        Valor = (int)Math.Min(
                            Math.Round(v.Total, 0),
                            int.MaxValue)
                    })
                    .ToList();
            }
            else
            {
                // Sin emprendimiento seleccionado, conservar el cálculo
                // general de ventas confirmadas, sin duplicar órdenes.
                totalVentas = await ventasConfirmadas
                    .SumAsync(v => (decimal?)v.Total) ?? 0m;

                var ventasAgrupadas = await ventasConfirmadas
                    .GroupBy(v => new
                    {
                        v.FechaVenta.Year,
                        v.FechaVenta.Month
                    })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Total = g.Sum(v => v.Total)
                    })
                    .OrderBy(g => g.Year)
                    .ThenBy(g => g.Month)
                    .ToListAsync();

                ventasPorMes = ventasAgrupadas
                    .Select(v => new PuntoGraficoViewModel
                    {
                        Etiqueta = $"{v.Month:00}/{v.Year}",
                        Valor = (int)Math.Min(
                            Math.Round(v.Total, 0),
                            int.MaxValue)
                    })
                    .ToList();
            }

            // HU-63: Productos publicados con filtro por emprendimiento.
            var productosConsulta = _context.Productos
                .AsNoTracking()
                .Where(p => p.Estado == "Publicado");

            if (filtro.Emprendimiento.HasValue)
            {
                int idEmprendimiento = filtro.Emprendimiento.Value;

                productosConsulta = productosConsulta
                    .Where(p => p.IdEmprendimiento == idEmprendimiento);
            }

            // HU-60: Productos publicados por categoría.
            var categoriasAgrupadas = await productosConsulta
                .GroupBy(p => p.IdCategoriaNavigation != null
                    ? p.IdCategoriaNavigation.Nombre
                    : "Sin categoría")
                .Select(g => new
                {
                    Categoria = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(g => g.Cantidad)
                .ToListAsync();

            var productosPorCategoria = categoriasAgrupadas
                .Select(c => new PuntoGraficoViewModel
                {
                    Etiqueta = c.Categoria,
                    Valor = c.Cantidad
                })
                .ToList();

            // HU-60: Cargar emprendimientos reales para el filtro.
            var emprendimientosFiltro = await _context.Emprendimientos
                .AsNoTracking()
                .OrderBy(e => e.NombreComercial)
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.IdEmprendimiento.ToString(),
                    Text = e.NombreComercial
                })
                .ToListAsync();

            // HU-63: Cargar eventos reales para el filtro.
            var eventosFiltro = await _context.Eventos
                .AsNoTracking()
                .OrderByDescending(e => e.FechaInicio)
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.IdEvento.ToString(),
                    Text = e.Nombre
                })
                .ToListAsync();

            DashboardAdminViewModel modelo = new()
            {
                Filtro = filtro,
                EmprendimientosFiltro = emprendimientosFiltro,
                EventosFiltro = eventosFiltro,

                TotalUsuarios = await _context.Usuarios.CountAsync(),

                EmprendimientosRegistrados =
                    await _context.Emprendimientos.CountAsync(),

                SolicitudesPendientes = emprendimientosPendientes + pagosPendientes,
                ProductosActivos = await productosConsulta.CountAsync(),

                VentasTotal = totalVentas,
                VariacionVentasPorcentaje = 0,

                VentasPorMes = ventasPorMes,
                ProductosPorCategoria = productosPorCategoria,

                ProximosEventos = eventos
                    .Select(e => new EventoAdminViewModel
                    {
                        Id = e.IdEvento,
                        Nombre = e.Nombre,
                        Fecha = e.FechaInicio.ToString("dd/MM/yyyy"),
                        Ubicacion = e.Ubicacion,
                        CupoTotal = e.CupoMaximo ?? 0,
                        Inscritos = e.Inscritos
                    })
                    .ToList(),

                TalleresDisponibles = talleresProgramados
                    .Take(3)
                    .Select(t => new TallerAdminViewModel
                    {
                        Id = t.IdTaller,
                        Nombre = t.Nombre,
                        Fecha = t.FechaInicio.ToString("dd/MM/yyyy"),
                        CupoTotal = t.CupoMaximo,
                        Inscritos = t.Inscritos
                    })
                    .ToList(),

                Alertas = alertas,
                ActividadReciente = actividad
            };

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Usuarios(string? busqueda, string? rol)
        {
            IQueryable<Usuario> consulta = _context.Usuarios.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string termino = busqueda.Trim();

                consulta = consulta.Where(u =>
                    u.Nombre.Contains(termino) ||
                    u.ApellidoPaterno.Contains(termino) ||
                    u.Correo.Contains(termino));
            }

            if (!string.IsNullOrWhiteSpace(rol) && rol != "Todos")
            {
                consulta = consulta.Where(u =>
                    u.UsuarioRoles.Any(ur => ur.IdRolNavigation.Nombre == rol));
            }

            var filas = await consulta
                .OrderBy(u => u.Nombre)
                .ThenBy(u => u.ApellidoPaterno)
                .Select(u => new
                {
                    u.IdUsuario,
                    u.Nombre,
                    u.ApellidoPaterno,
                    u.ApellidoMaterno,
                    u.Correo,
                    u.Estado,
                    u.FechaRegistro,
                    u.FotoPerfilUrl,
                    Rol = u.UsuarioRoles
                        .Select(ur => ur.IdRolNavigation.Nombre)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(new ListadoUsuariosAdminViewModel
            {
                Busqueda = busqueda,
                RolFiltro = rol,
                Usuarios = filas
                    .Select(u => new UsuarioAdminViewModel
                    {
                        Id = u.IdUsuario,
                        Nombre = $"{u.Nombre} {u.ApellidoPaterno} {u.ApellidoMaterno}".Trim(),
                        Correo = u.Correo,
                        Rol = u.Rol ?? "Sin rol",
                        Estado = u.Estado,
                        FechaRegistro = u.FechaRegistro.ToString("dd/MM/yyyy"),
                        Foto = string.IsNullOrWhiteSpace(u.FotoPerfilUrl)
                            ? "/images/logo.jpg"
                            : u.FotoPerfilUrl
                    })
                    .ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> PerfilUsuario(int id)
        {
            Usuario? usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.IdProvinciaNavigation)
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario is null)
            {
                TempData["MensajeAdmin"] = "El usuario no existe.";

                return RedirectToAction(nameof(Usuarios));
            }

            return View(new PerfilUsuarioAdminViewModel
            {
                Id = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Apellidos = $"{usuario.ApellidoPaterno} {usuario.ApellidoMaterno}".Trim(),
                Correo = usuario.Correo,
                Telefono = usuario.Telefono,
                Provincia = usuario.IdProvinciaNavigation?.Nombre,
                Rol = usuario.UsuarioRoles
                    .Select(ur => ur.IdRolNavigation.Nombre)
                    .FirstOrDefault() ?? "Usuario",
                Estado = usuario.Estado,
                FechaRegistro = usuario.FechaRegistro.ToString("dd/MM/yyyy"),
                Foto = string.IsNullOrWhiteSpace(usuario.FotoPerfilUrl)
                    ? "/images/logo.jpg"
                    : usuario.FotoPerfilUrl,
                ComprasRealizadas = await _context.Ventas
                    .CountAsync(v => v.IdUsuario == id && v.Estado != "Cancelada"),
                PuntosAcumulados = await _context.MovimientosPuntos
                    .Where(m => m.IdUsuario == id)
                    .SumAsync(m => (int?)m.Puntos) ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerfilUsuario(PerfilUsuarioAdminViewModel modelo)
        {
            Usuario? usuario = await _context.Usuarios
                .Include(u => u.UsuarioRoles)
                .FirstOrDefaultAsync(u => u.IdUsuario == modelo.Id);

            if (usuario is null)
            {
                TempData["MensajeAdmin"] = "El usuario no existe.";

                return RedirectToAction(nameof(Usuarios));
            }

            string correo = modelo.Correo.Trim();

            if (await _context.Usuarios.AnyAsync(u =>
                    u.Correo == correo && u.IdUsuario != modelo.Id))
            {
                ModelState.AddModelError(nameof(modelo.Correo), "Ya existe otro usuario con ese correo.");
            }

            int? idSesion = HttpContext.Session.GetInt32("IdUsuario");

            if (idSesion == modelo.Id &&
                (modelo.Estado != "Activo" || modelo.Rol != "Administrador"))
            {
                ModelState.AddModelError(string.Empty, "No puedes desactivar tu propia cuenta ni quitarte el rol de administrador.");
            }

            Provincia? provincia = null;

            if (!string.IsNullOrWhiteSpace(modelo.Provincia))
            {
                string nombreProvincia = modelo.Provincia.Trim();

                provincia = await _context.Provincias
                    .FirstOrDefaultAsync(p => p.Nombre == nombreProvincia);

                if (provincia is null)
                {
                    ModelState.AddModelError(nameof(modelo.Provincia), "La provincia no es válida.");
                }
            }

            Role? rol = await _context.Roles
                .FirstOrDefaultAsync(r => r.Nombre == modelo.Rol && r.Activo);

            if (rol is null)
            {
                ModelState.AddModelError(nameof(modelo.Rol), "El rol seleccionado no es válido.");
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string[] apellidos = modelo.Apellidos
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            usuario.Nombre = modelo.Nombre.Trim();
            usuario.ApellidoPaterno = apellidos[0];
            usuario.ApellidoMaterno = apellidos.Length > 1
                ? string.Join(' ', apellidos.Skip(1))
                : null;
            usuario.Correo = correo;
            usuario.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim();
            usuario.IdProvincia = provincia?.IdProvincia;
            usuario.Estado = modelo.Estado;

            if (!usuario.UsuarioRoles.Any(ur => ur.IdRol == rol!.IdRol))
            {
                _context.UsuarioRoles.RemoveRange(usuario.UsuarioRoles);

                _context.UsuarioRoles.Add(new UsuarioRole
                {
                    IdUsuario = usuario.IdUsuario,
                    IdRol = rol!.IdRol,
                    FechaAsignacion = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"Los datos de {usuario.Nombre} se actualizaron correctamente.";

            return RedirectToAction(nameof(PerfilUsuario), new { id = modelo.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Emprendimientos(string estado = "Pendiente")
        {
            List<SolicitudEmprendimientoAdminViewModel> solicitudes =
                await ObtenerSolicitudesAsync();

            ListadoEmprendimientosAdminViewModel modelo = new()
            {
                FiltroEstado = estado,
                Solicitudes = solicitudes.Where(s => s.Estado == estado).ToList(),
                TotalPendientes = solicitudes.Count(s => s.Estado == "Pendiente"),
                TotalAprobados = solicitudes.Count(s => s.Estado == "Aprobado"),
                TotalRechazados = solicitudes.Count(s => s.Estado == "Rechazado")
            };

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleSolicitud(int id = 1)
        {
            SolicitudEmprendimientoAdminViewModel? modelo =
                await ObtenerSolicitudAsync(id);

            if (modelo is null)
            {
                return RedirectToAction(nameof(Emprendimientos));
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .Include(e => e.EmprendimientoRevisione)
                .Include(e => e.IdUsuarioPropietarioNavigation)
                .FirstOrDefaultAsync(e =>
                    e.IdEmprendimiento == id &&
                    e.EstadoAprobacion == "Pendiente");

            if (emprendimiento is null)
            {
                TempData["MensajeAdmin"] =
                    "La solicitud no existe o ya fue resuelta.";

                return RedirectToAction(nameof(Emprendimientos), new { estado = "Pendiente" });
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            emprendimiento.EstadoAprobacion = "Aprobado";

            if (emprendimiento.EmprendimientoRevisione is not null)
            {
                emprendimiento.EmprendimientoRevisione.FechaResolucion = DateTime.Now;
            }

            await AsignarRolEmprendedorAsync(emprendimiento.IdUsuarioPropietario);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            await NotificarResolucionSolicitudAsync(
                emprendimiento,
                aprobada: true,
                motivo: null);

            // HU-70: Notificar la aprobación de novedades de emprendimientos favoritos.
            List<int> idsUsuariosFavoritos = await _context.Favoritos
                .AsNoTracking()
                .Where(f => f.IdEmprendimiento == emprendimiento.IdEmprendimiento)
                .Select(f => f.IdUsuario)
                .Distinct()
                .ToListAsync();

            foreach (int idUsuarioFavorito in idsUsuariosFavoritos)
            {
                await _notificaciones.AgregarAsync(new Notificacione
                {
                    IdUsuario = idUsuarioFavorito,
                    Titulo = "Novedades de un emprendimiento favorito",
                    Mensaje =
        $"El emprendimiento «{emprendimiento.NombreComercial}» " +
        "que guardaste como favorito tiene información aprobada y actualizada. " +
        "Consulta sus detalles.",
                    Tipo = "Promoción",

                    // HU-70: Referencia para abrir el detalle del emprendimiento favorito.
                    TipoReferencia = "Emprendimiento",
                    IdReferencia = emprendimiento.IdEmprendimiento,

                    FechaEnvio = DateTime.Now,
                    Leida = false,
                    FechaLectura = null
                });
            }

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = "La solicitud fue aprobada correctamente.";

            return RedirectToAction(nameof(Emprendimientos), new { estado = "Pendiente" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(RechazarSolicitudViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                SolicitudEmprendimientoAdminViewModel? solicitud =
                    await ObtenerSolicitudAsync(modelo.Id);

                if (solicitud is null)
                {
                    return RedirectToAction(nameof(Emprendimientos));
                }

                solicitud.MotivoRechazo = modelo.Motivo;

                return View(nameof(DetalleSolicitud), solicitud);
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .Include(e => e.EmprendimientoRevisione)
                .Include(e => e.IdUsuarioPropietarioNavigation)
                .FirstOrDefaultAsync(e =>
                    e.IdEmprendimiento == modelo.Id &&
                    e.EstadoAprobacion == "Pendiente");

            if (emprendimiento is null)
            {
                TempData["MensajeAdmin"] =
                    "La solicitud no existe o ya fue resuelta.";

                return RedirectToAction(nameof(Emprendimientos), new { estado = "Pendiente" });
            }

            string motivoRechazo = modelo.Motivo.Trim();

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            emprendimiento.EstadoAprobacion = "Rechazado";

            if (emprendimiento.EmprendimientoRevisione is not null)
            {
                emprendimiento.EmprendimientoRevisione.FechaResolucion = DateTime.Now;
            }

            _context.MotivosRechazos.Add(new MotivosRechazo
            {
                IdEmprendimiento = emprendimiento.IdEmprendimiento,
                Motivo = motivoRechazo,
                FechaRechazo = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            await NotificarResolucionSolicitudAsync(
                emprendimiento,
                aprobada: false,
                motivo: motivoRechazo);

            TempData["MensajeAdmin"] =
                "La solicitud fue rechazada y se notificó el motivo al solicitante.";

            return RedirectToAction(nameof(Emprendimientos), new { estado = "Pendiente" });
        }

        private async Task NotificarResolucionSolicitudAsync(
            Emprendimiento emprendimiento,
            bool aprobada,
            string? motivo)
        {
            string? correoDestino =
                emprendimiento.IdUsuarioPropietarioNavigation?.Correo;

            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                return;
            }

            string nombreDestinatario =
                emprendimiento.IdUsuarioPropietarioNavigation?.Nombre
                    ?? "emprendedor";

            string asunto = aprobada
                ? "Tu solicitud de emprendimiento fue aprobada - Club Creativo"
                : "Resultado de tu solicitud de emprendimiento - Club Creativo";

            string titulo = aprobada
                ? "¡Tu solicitud fue aprobada!"
                : "Resultado de tu solicitud";

            string cuerpo = aprobada
                ? $@"
                    <p>Hola {PlantillaCorreo.E(nombreDestinatario)},</p>
                    <p>¡Buenas noticias! Tu solicitud para incorporar
                       «<strong>{PlantillaCorreo.E(emprendimiento.NombreComercial)}</strong>» a Club Creativo fue <strong>aprobada</strong>.</p>
                    <p>Ya puedes iniciar sesión con tu cuenta de emprendedor para administrar tu perfil, productos y participación en la plataforma.</p>"
                : $@"
                    <p>Hola {PlantillaCorreo.E(nombreDestinatario)},</p>
                    <p>Tu solicitud para incorporar «<strong>{PlantillaCorreo.E(emprendimiento.NombreComercial)}</strong>» a Club Creativo fue <strong>rechazada</strong>.</p>
                    <p>Motivo indicado por el equipo revisor:</p>
                    <p style=""background:#faf7ff;border-left:4px solid #a855c7;padding:12px 16px;border-radius:6px;"">{PlantillaCorreo.E(motivo)}</p>
                    <p>Puedes iniciar sesión, corregir la información de tu emprendimiento y volver a enviarla para revisión.</p>";

            string urlBase = $"{Request.Scheme}://{Request.Host}";

            string contenidoCorreo = PlantillaCorreo.Construir(
                titulo: titulo,
                cuerpoHtml: cuerpo,
                textoBoton: "Iniciar sesión",
                urlBoton: $"{urlBase}/Cuenta/IniciarSesion",
                urlBase: urlBase);

            try
            {
                await _correoService.EnviarCorreoAsync(
                    correoDestino,
                    asunto,
                    contenidoCorreo
                );
            }
            catch
            {

            }
        }
        private async Task NotificarPagoSinpeAsync(
    int idEmprendimiento,
    string nombrePlan,
    decimal monto,
    bool aprobado,
    string? motivo = null,
    DateOnly? fechaInicio = null,
    DateOnly? fechaFin = null)
        {
            var emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .Include(e => e.IdUsuarioPropietarioNavigation)
                .FirstOrDefaultAsync(e =>
                    e.IdEmprendimiento == idEmprendimiento);

            string? correoDestino =
                emprendimiento?.IdUsuarioPropietarioNavigation?.Correo;

            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                return;
            }

            string nombre =
                emprendimiento?.IdUsuarioPropietarioNavigation?.Nombre
                ?? "emprendedor";

            string asunto = aprobado
                ? "Pago SINPE aprobado - Club Creativo"
                : "Pago SINPE rechazado - Club Creativo";

            string titulo = aprobado
                ? "¡Tu pago SINPE fue aprobado!"
                : "Resultado de tu pago SINPE";

            string cuerpo;

            if (aprobado)
            {
                cuerpo = $@"
            <p>Hola {PlantillaCorreo.E(nombre)},</p>

            <p>¡Buenas noticias! Hemos verificado y aprobado
            tu pago mediante SINPE Móvil para el plan
            <strong>{PlantillaCorreo.E(nombrePlan)}</strong>.</p>

            <p><strong>Monto aprobado:</strong>
            ₡{monto:N2}</p>

            <p><strong>Fecha de inicio:</strong>
            {fechaInicio:dd/MM/yyyy}</p>

            <p><strong>Fecha de vencimiento:</strong>
            {fechaFin:dd/MM/yyyy}</p>

            <p>Si la fecha de inicio todavía no ha llegado,
            tu suscripción se activará automáticamente
            cuando corresponda.</p>

            <p>Puedes consultar el estado de tu plan
            ingresando a Club Creativo MiVo.</p>";
            }
            else
            {
                cuerpo = $@"
            <p>Hola {PlantillaCorreo.E(nombre)},</p>

            <p>Te informamos que tu solicitud de pago
            mediante SINPE Móvil para el plan
            <strong>{PlantillaCorreo.E(nombrePlan)}</strong>
            no fue aprobada.</p>

            <p><strong>Monto de la solicitud:</strong>
            ₡{monto:N2}</p>

            <p><strong>Motivo del rechazo:</strong></p>

            <p style=""background:#faf7ff;
                border-left:4px solid #a855c7;
                padding:12px 16px;
                border-radius:6px;"">
                {PlantillaCorreo.E(motivo)}
            </p>

            <p>Si deseas adquirir el plan,
            puedes revisar el motivo indicado
            y registrar una nueva solicitud
            con los datos de pago correspondientes.</p>";
            }

            string urlBase = $"{Request.Scheme}://{Request.Host}";

            string contenidoCorreo = PlantillaCorreo.Construir(
                titulo: titulo,
                cuerpoHtml: cuerpo,
                textoBoton: "Ver mi suscripción",
                urlBoton: $"{urlBase}/Emprendedor/MiSuscripcion",
                urlBase: urlBase);

            try
            {
                await _correoService.EnviarCorreoAsync(
                    correoDestino,
                    asunto,
                    contenidoCorreo);
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices
                    .GetRequiredService<ILogger<AdminController>>();

                logger.LogError(
                    ex,
                    "Error al enviar el correo de resolución del pago SINPE. Solicitud del emprendimiento: {IdEmprendimiento}",
                    idEmprendimiento);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(RechazarSolicitudViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                SolicitudEmprendimientoAdminViewModel? solicitud =
                    await ObtenerSolicitudAsync(modelo.Id);

                if (solicitud is null)
                {
                    return RedirectToAction(nameof(Emprendimientos));
                }

                solicitud.MotivoRechazo = modelo.Motivo;

                return View(nameof(DetalleSolicitud), solicitud);
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .FirstOrDefaultAsync(e =>
                    e.IdEmprendimiento == modelo.Id &&
                    e.EstadoAprobacion == "Aprobado" &&
                    e.Activo);

            if (emprendimiento is null)
            {
                TempData["MensajeAdmin"] =
                    "El emprendimiento no existe o ya se encuentra inactivo.";

                return RedirectToAction(nameof(Emprendimientos), new { estado = "Aprobado" });
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            emprendimiento.Activo = false;

            _context.MotivosRechazos.Add(new MotivosRechazo
            {
                IdEmprendimiento = emprendimiento.IdEmprendimiento,
                Motivo = modelo.Motivo.Trim(),
                FechaRechazo = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            TempData["MensajeAdmin"] =
                "El emprendimiento fue desactivado y se notificó el motivo al propietario.";

            return RedirectToAction(nameof(Emprendimientos), new { estado = "Aprobado" });
        }
        private async Task AsignarRolEmprendedorAsync(int idUsuario)
        {
            bool yaEsEmprendedor =
                await _context.UsuarioRoles.AnyAsync(ur =>
                    ur.IdUsuario == idUsuario &&
                    ur.IdRolNavigation.Nombre == "Emprendedor");

            if (yaEsEmprendedor)
            {
                return;
            }

            Role? rolEmprendedor = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.Nombre == "Emprendedor" &&
                    r.Activo);

            if (rolEmprendedor is null)
            {
                throw new InvalidOperationException(
                    "No se encontró el rol de emprendedor en el sistema.");
            }

            List<UsuarioRole> rolesUsuario = await _context.UsuarioRoles
                .Where(ur =>
                    ur.IdUsuario == idUsuario &&
                    ur.IdRolNavigation.Nombre == "Usuario")
                .ToListAsync();

            _context.UsuarioRoles.RemoveRange(rolesUsuario);

            await _context.UsuarioRoles.AddAsync(new UsuarioRole
            {
                IdUsuario = idUsuario,
                IdRol = rolEmprendedor.IdRol,
                FechaAsignacion = DateTime.Now
            });
        }
        private async Task<List<SolicitudEmprendimientoAdminViewModel>> ObtenerSolicitudesAsync()
        {
            List<Emprendimiento> emprendimientos = await _context.Emprendimientos
                .AsNoTracking()
                .Include(e => e.IdUsuarioPropietarioNavigation)
                .Include(e => e.IdCategoriaNavigation)
                .Include(e => e.EmprendimientoRevisione)
                .Include(e => e.MotivosRechazos)
                .ToListAsync();

            return emprendimientos
                .OrderByDescending(e => e.EmprendimientoRevisione?.FechaSolicitud)
                .Select(MapearSolicitud)
                .ToList();
        }

        private async Task<SolicitudEmprendimientoAdminViewModel?> ObtenerSolicitudAsync(int id)
        {
            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .Include(e => e.IdUsuarioPropietarioNavigation)
                .Include(e => e.IdCategoriaNavigation)
                .Include(e => e.EmprendimientoRevisione)
                .Include(e => e.MotivosRechazos)
                .FirstOrDefaultAsync(e => e.IdEmprendimiento == id);

            return emprendimiento is null ? null : MapearSolicitud(emprendimiento);
        }

        private static SolicitudEmprendimientoAdminViewModel MapearSolicitud(Emprendimiento e)
        {
            Usuario? propietario = e.IdUsuarioPropietarioNavigation;

            return new SolicitudEmprendimientoAdminViewModel
            {
                Id = e.IdEmprendimiento,
                NombreComercial = e.NombreComercial,
                Solicitante = propietario is null
        ? string.Empty
        : $"{propietario.Nombre} {propietario.ApellidoPaterno} {propietario.ApellidoMaterno}".Trim(),
                Categoria = e.IdCategoriaNavigation?.Nombre ?? "Sin categoría",
                Correo = e.Correo,
                Telefono = e.Telefono ?? string.Empty,
                FechaSolicitud = e.EmprendimientoRevisione?.FechaSolicitud.ToString("dd/MM/yyyy")
        ?? string.Empty,
                Estado = e.EstadoAprobacion,
                Descripcion = e.Descripcion,
                Logo = e.LogoUrl ?? "/images/logo.jpg",
                Activo = e.Activo,
                MotivoRechazo = e.MotivosRechazos
        .OrderByDescending(m => m.FechaRechazo)
        .FirstOrDefault()?.Motivo
            };
        }

        [HttpGet]
        public async Task<IActionResult> Suscripciones()
        {
            await _actualizacionSuscripciones.ActualizarEstadosAsync();
            List<PlanSuscripcionViewModel> planes =
                await _context.PlanesSuscripcions
                    .AsNoTracking()
                    .OrderBy(p => p.IdPlan)
                    .Select(p => new PlanSuscripcionViewModel
                    {
                        Id = p.IdPlan,
                        Nombre = p.Nombre,
                        Precio = p.Precio,
                        Periodicidad = p.Periodicidad,
                        Beneficios = p.Beneficios ?? string.Empty,
                        MaximoProductos = p.MaximoProductos,
                        Activo = p.Activo,
                        CantidadSuscriptores = p.Suscripciones.Count()
                    })
                    .ToListAsync();


            List<Suscripcione> suscripciones =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdEmprendimientoNavigation)
                    .Include(s => s.IdPlanNavigation)
                    .Where(s => s.Estado == "Activa")
                    .OrderBy(s => s.FechaFin)
                    .ToListAsync();


            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            DateOnly fechaLimite =
                fechaActual.AddDays(7);


            List<SuscripcionActivaViewModel> activas =
                suscripciones
                    .Select(s => new SuscripcionActivaViewModel
                    {
                        Id = s.IdSuscripcion,

                        Emprendimiento =
                            s.IdEmprendimientoNavigation.NombreComercial,

                        Plan =
                            s.IdPlanNavigation.Nombre,

                        FechaInicio =
                            s.FechaInicio.ToString("dd/MM/yyyy"),

                        FechaVencimiento =
                            s.FechaFin.ToString("dd/MM/yyyy"),

                        Estado =
                            s.FechaFin < fechaActual
                                ? "Vencida"
                                : s.FechaFin <= fechaLimite
                                    ? "Por vencer"
                                    : "Vigente"
                    })
                    .ToList();


            List<DetallePagoSinpeAdminViewModel> solicitudesSinpe =
                await _context.SolicitudesPagoSuscripcion
                    .AsNoTracking()
                    .OrderByDescending(s => s.FechaSolicitud)
                    .Select(s => new DetallePagoSinpeAdminViewModel
                    {
                        IdSolicitudPago = s.IdSolicitudPago,
                        IdEmprendimiento = s.IdEmprendimiento,
                        NombreEmprendimiento =
                            s.IdEmprendimientoNavigation.NombreComercial,
                        NombrePlan = s.IdPlanNavigation.Nombre,
                        Monto = s.Monto,
                        NumeroReferencia = s.NumeroReferencia,
                        FechaSolicitud = s.FechaSolicitud,
                        Estado = s.Estado,
                        TipoSolicitud = s.TipoSolicitud,
                        FechaInicioProgramada = s.FechaInicioProgramada,
                        FechaRevision = s.FechaRevision,
                        Observaciones = s.Observaciones
                    })
                    .ToListAsync();

            GestionSuscripcionesViewModel modelo = new()
            {
                Planes = planes,
                Activas = activas,
                TotalPorVencer =
                    activas.Count(s => s.Estado == "Por vencer"),

                SolicitudesSinpe = solicitudesSinpe,
                TotalSolicitudesSinpePendientes =
                    solicitudesSinpe.Count(s => s.Estado == "Pendiente")
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarPagoSinpe(
    RechazarPagoSinpeViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                TempData["MensajeAdmin"] =
                    "Debes indicar un motivo válido para rechazar el pago.";

                return RedirectToAction(nameof(Suscripciones));
            }

            var solicitud = await _context.SolicitudesPagoSuscripcion
                .FirstOrDefaultAsync(s =>
                    s.IdSolicitudPago == modelo.IdSolicitudPago &&
                    s.Estado == "Pendiente");

            if (solicitud == null)
            {
                TempData["MensajeAdmin"] =
                    "La solicitud no existe o ya fue revisada.";

                return RedirectToAction(nameof(Suscripciones));
            }

            solicitud.Estado = "Rechazada";
            solicitud.FechaRevision = DateTime.Now;
            solicitud.IdAdministradorRevision =
                HttpContext.Session.GetInt32("IdUsuario");
            solicitud.Observaciones = modelo.Motivo.Trim();

            await _context.SaveChangesAsync();
            var plan = await _context.PlanesSuscripcions
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.IdPlan == solicitud.IdPlan);

            await NotificarPagoSinpeAsync(
                idEmprendimiento: solicitud.IdEmprendimiento,
                nombrePlan: plan?.Nombre ?? "Plan de suscripción",
                monto: solicitud.Monto,
                aprobado: false,
                motivo: solicitud.Observaciones);

            TempData["MensajeAdmin"] =
                "La solicitud de pago SINPE fue rechazada correctamente.";

            return RedirectToAction(nameof(Suscripciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarPagoSinpe(int idSolicitudPago)
        {
            if (idSolicitudPago <= 0)
            {
                TempData["MensajeAdmin"] = "La solicitud seleccionada no es válida.";
                return RedirectToAction(nameof(Suscripciones));
            }

            // La aprobación solo debe ejecutarse después de que
            // el administrador verifique la transferencia SINPE.
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var solicitud = await _context.SolicitudesPagoSuscripcion
                .FirstOrDefaultAsync(s =>
                    s.IdSolicitudPago == idSolicitudPago &&
                    s.Estado == "Pendiente");

            if (solicitud == null)
            {
                TempData["MensajeAdmin"] =
                    "La solicitud no existe o ya fue revisada.";

                return RedirectToAction(nameof(Suscripciones));
            }

            var emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdEmprendimiento == solicitud.IdEmprendimiento &&
                    e.Activo &&
                    e.EstadoAprobacion == "Aprobado");

            var plan = await _context.PlanesSuscripcions
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.IdPlan == solicitud.IdPlan);

            if (emprendimiento == null || plan == null)
            {
                TempData["MensajeAdmin"] =
                    "No se puede aprobar el pago porque el emprendimiento o el plan no son válidos.";

                return RedirectToAction(nameof(Suscripciones));
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            var suscripciones = await _context.Suscripciones
                .Where(s => s.IdEmprendimiento == solicitud.IdEmprendimiento)
                .ToListAsync();

            // Validar que el tipo de solicitud sea reconocido.
            if (solicitud.TipoSolicitud != "Nueva" &&
                solicitud.TipoSolicitud != "CambioPlan" &&
                solicitud.TipoSolicitud != "Renovacion")
            {
                TempData["MensajeAdmin"] =
                    "El tipo de solicitud de pago no es válido.";

                return RedirectToAction(nameof(Suscripciones));
            }

            // Validar que el monto registrado corresponda al plan.
            if (solicitud.Monto != plan.Precio)
            {
                TempData["MensajeAdmin"] =
                    "El monto de la solicitud no coincide con el precio actual del plan. " +
                    "Es necesario revisar el pago antes de aprobar.";

                return RedirectToAction(nameof(Suscripciones));
            }

            // Comprobar la suscripción original de cambios y renovaciones.
            if (solicitud.TipoSolicitud == "CambioPlan" ||
                solicitud.TipoSolicitud == "Renovacion")
            {
                var suscripcionAnterior = suscripciones
                    .FirstOrDefault(s =>
                        s.IdSuscripcion == solicitud.IdSuscripcionAnterior);

                if (suscripcionAnterior == null)
                {
                    TempData["MensajeAdmin"] =
                        "No se encontró la suscripción original de esta solicitud.";

                    return RedirectToAction(nameof(Suscripciones));
                }
                if (suscripcionAnterior.Estado != "Activa" &&
    suscripcionAnterior.Estado != "Vencida")
                {
                    TempData["MensajeAdmin"] =
                        "La suscripción original fue cancelada o modificada. " +
                        "Es necesario revisar esta solicitud antes de aprobarla.";

                    return RedirectToAction(nameof(Suscripciones));
                }
                bool tieneCancelacionRegistrada =
                    await _context.SuscripcionCancelaciones
                        .AnyAsync(c =>
                            c.IdSuscripcion == suscripcionAnterior.IdSuscripcion);

                if (tieneCancelacionRegistrada)
                {
                    TempData["MensajeAdmin"] =
                        "Esta suscripción tiene una cancelación registrada. " +
                        "No se puede aprobar automáticamente su renovación o cambio de plan.";

                    return RedirectToAction(nameof(Suscripciones));
                }

                if (solicitud.FechaInicioProgramada !=
                    suscripcionAnterior.FechaFin.AddDays(1))
                {
                    TempData["MensajeAdmin"] =
                        "La fecha programada no coincide con el vencimiento del plan anterior.";

                    return RedirectToAction(nameof(Suscripciones));
                }

                if (solicitud.TipoSolicitud == "Renovacion" &&
                    suscripcionAnterior.IdPlan != solicitud.IdPlan)
                {
                    TempData["MensajeAdmin"] =
                        "Una renovación debe conservar el mismo plan.";

                    return RedirectToAction(nameof(Suscripciones));
                }

                if (solicitud.TipoSolicitud == "CambioPlan" &&
                    suscripcionAnterior.IdPlan == solicitud.IdPlan)
                {
                    TempData["MensajeAdmin"] =
                        "El cambio de plan debe seleccionar un plan diferente.";

                    return RedirectToAction(nameof(Suscripciones));
                }
            }

            // Evitar aprobar otro plan que ya esté programado.
            bool existeProgramada = suscripciones.Any(s =>
                s.Estado == "Pendiente" &&
                s.FechaFin >= hoy);

            if (existeProgramada)
            {
                TempData["MensajeAdmin"] =
                    "Este emprendimiento ya tiene una suscripción programada " +
                    "o pendiente de activación.";

                return RedirectToAction(nameof(Suscripciones));
            }

            // Buscar la suscripción actualmente vigente.
            var suscripcionActual = suscripciones
                .Where(s =>
                    s.Estado == "Activa" &&
                    s.FechaInicio <= hoy &&
                    s.FechaFin >= hoy)
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefault();

            DateOnly fechaInicio;

            if (solicitud.TipoSolicitud == "Renovacion" ||
    solicitud.TipoSolicitud == "CambioPlan")
            {
                // Conservar la fecha acordada al solicitar
                // la renovación o el cambio de plan.
                if (!solicitud.FechaInicioProgramada.HasValue)
                {
                    TempData["MensajeAdmin"] =
                        "La solicitud no tiene una fecha de inicio programada.";

                    return RedirectToAction(nameof(Suscripciones));
                }

                fechaInicio = solicitud.FechaInicioProgramada.Value < hoy
                    ? hoy
                    : solicitud.FechaInicioProgramada.Value;
            }
            else
            {
                // Una suscripción nueva comienza cuando se aprueba.
                if (suscripcionActual != null)
                {
                    TempData["MensajeAdmin"] =
                        "Este emprendimiento ya tiene una suscripción vigente.";

                    return RedirectToAction(nameof(Suscripciones));
                }

                bool tienePlanPendiente = suscripciones.Any(s =>
                    s.Estado == "Pendiente");

                if (tienePlanPendiente)
                {
                    TempData["MensajeAdmin"] =
                        "Este emprendimiento ya tiene una suscripción pendiente de activación.";

                    return RedirectToAction(nameof(Suscripciones));
                }
                fechaInicio = hoy;

            }


            // No permitir que un cambio o renovación comience
            // antes de que finalice el plan vigente.
            if (suscripcionActual != null &&
                fechaInicio <= suscripcionActual.FechaFin)
            {
                TempData["MensajeAdmin"] =
                    "La fecha de inicio del nuevo plan se superpone " +
                    "con la suscripción actual.";

                return RedirectToAction(nameof(Suscripciones));
            }

            // Liberar el índice único cuando existan planes vencidos
            // que todavía tengan el estado Activa.
            foreach (var anterior in suscripciones.Where(s =>
                s.Estado == "Activa" &&
                s.FechaFin < hoy))
            {
                anterior.Estado = "Vencida";
            }

            DateOnly fechaFin;

            if (plan.Periodicidad.Equals(
                "Anual", StringComparison.OrdinalIgnoreCase))
            {
                fechaFin = fechaInicio.AddYears(1).AddDays(-1);
            }
            else if (plan.Periodicidad.Equals(
                "Trimestral", StringComparison.OrdinalIgnoreCase))
            {
                fechaFin = fechaInicio.AddMonths(3).AddDays(-1);
            }
            else
            {
                fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
            }

            bool existePeriodoSuperpuesto = suscripciones.Any(s =>
                s.Estado != "Cancelada" &&
                s.FechaInicio <= fechaFin &&
                s.FechaFin >= fechaInicio);

            if (existePeriodoSuperpuesto)
            {
                TempData["MensajeAdmin"] =
                    "Ya existe una suscripción que coincide con el período solicitado. " +
                    "No se puede aprobar un período duplicado.";

                return RedirectToAction(nameof(Suscripciones));
            }

            Suscripcione nuevaSuscripcion = new()
            {
                IdEmprendimiento = solicitud.IdEmprendimiento,
                IdPlan = plan.IdPlan,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = fechaInicio > hoy ? "Pendiente" : "Activa",
                RenovacionAutomatica = false,
                MaximoProductosContratado = plan.MaximoProductos
            };

            _context.Suscripciones.Add(nuevaSuscripcion);

            solicitud.Estado = "Aprobada";
            solicitud.FechaRevision = DateTime.Now;
            solicitud.IdAdministradorRevision =
                HttpContext.Session.GetInt32("IdUsuario");
            solicitud.Observaciones = fechaInicio > hoy
                ? "Pago verificado. La suscripción se activará en la fecha programada."
                : "Pago verificado. Suscripción activada correctamente.";

            await _context.SaveChangesAsync();

            solicitud.IdSuscripcionGenerada = nuevaSuscripcion.IdSuscripcion;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            await NotificarPagoSinpeAsync(
                idEmprendimiento: solicitud.IdEmprendimiento,
                nombrePlan: plan.Nombre,
                monto: solicitud.Monto,
                aprobado: true,
                fechaInicio: nuevaSuscripcion.FechaInicio,
                fechaFin: nuevaSuscripcion.FechaFin);

            TempData["MensajeAdmin"] = fechaInicio > hoy
                ? "Pago aprobado. La suscripción quedó programada."
                : "Pago aprobado y suscripción activada correctamente.";

            return RedirectToAction(nameof(Suscripciones));
        }

        [HttpGet]
        public IActionResult CrearPlan()
        {
            ViewData["Titulo"] = "Crear plan de suscripción";
            return View("FormularioPlan", new PlanSuscripcionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPlan(PlanSuscripcionViewModel modelo)
        {
            ViewData["Titulo"] = "Crear plan de suscripción";

            if (!ModelState.IsValid)
            {
                return View("FormularioPlan", modelo);
            }

            if (!modelo.MaximoProductos.HasValue || modelo.MaximoProductos.Value < 1)
            {
                ModelState.AddModelError(
                    nameof(modelo.MaximoProductos),
                    "Debes indicar una cantidad válida de productos.");

                return View("FormularioPlan", modelo);
            }

            bool nombreExiste = await _context.PlanesSuscripcions
                .AnyAsync(p => p.Nombre == modelo.Nombre.Trim());

            if (nombreExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.Nombre),
                    "Ya existe un plan de suscripción con este nombre.");

                return View("FormularioPlan", modelo);
            }

            PlanesSuscripcion nuevoPlan = new()
            {
                Nombre = modelo.Nombre.Trim(),
                Precio = modelo.Precio,
                Periodicidad = modelo.Periodicidad,
                Beneficios = string.IsNullOrWhiteSpace(modelo.Beneficios)
                    ? null
                    : modelo.Beneficios.Trim(),
                MaximoProductos = modelo.MaximoProductos,
                Activo = modelo.Activo
            };

            _context.PlanesSuscripcions.Add(nuevoPlan);
            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"El plan \"{nuevoPlan.Nombre}\" fue creado correctamente.";

            return RedirectToAction(nameof(Suscripciones));
        }

        [HttpGet]
        public async Task<IActionResult> EditarPlan(int id)
        {
            PlanesSuscripcion? plan = await _context.PlanesSuscripcions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPlan == id);

            if (plan is null)
            {
                TempData["MensajeAdmin"] =
                    "El plan de suscripción no existe.";

                return RedirectToAction(nameof(Suscripciones));
            }

            PlanSuscripcionViewModel modelo = new()
            {
                Id = plan.IdPlan,
                Nombre = plan.Nombre,
                Precio = plan.Precio,
                Periodicidad = plan.Periodicidad,
                Beneficios = plan.Beneficios ?? string.Empty,
                MaximoProductos = plan.MaximoProductos,
                Activo = plan.Activo,
                CantidadSuscriptores = await _context.Suscripciones
                    .CountAsync(s => s.IdPlan == plan.IdPlan)
            };

            ViewData["Titulo"] = "Editar plan de suscripción";

            return View("FormularioPlan", modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPlan(PlanSuscripcionViewModel modelo)
        {
            ViewData["Titulo"] = "Editar plan de suscripción";

            if (!ModelState.IsValid)
            {
                return View("FormularioPlan", modelo);
            }

            if (!modelo.MaximoProductos.HasValue || modelo.MaximoProductos.Value < 1)
            {
                ModelState.AddModelError(
                    nameof(modelo.MaximoProductos),
                    "Debes indicar una cantidad válida de productos.");

                return View("FormularioPlan", modelo);
            }

            PlanesSuscripcion? plan = await _context.PlanesSuscripcions
                .FirstOrDefaultAsync(p => p.IdPlan == modelo.Id);

            if (plan is null)
            {
                TempData["MensajeAdmin"] =
                    "El plan de suscripción no existe.";

                return RedirectToAction(nameof(Suscripciones));
            }

            bool nombreExiste = await _context.PlanesSuscripcions
                .AnyAsync(p =>
                    p.Nombre == modelo.Nombre.Trim() &&
                    p.IdPlan != modelo.Id);

            if (nombreExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.Nombre),
                    "Ya existe otro plan de suscripción con este nombre.");

                return View("FormularioPlan", modelo);
            }

            plan.Nombre = modelo.Nombre.Trim();
            plan.Precio = modelo.Precio;
            plan.Periodicidad = modelo.Periodicidad;
            plan.Beneficios = string.IsNullOrWhiteSpace(modelo.Beneficios)
                ? null
                : modelo.Beneficios.Trim();
            plan.MaximoProductos = modelo.MaximoProductos;
            plan.Activo = modelo.Activo;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"El plan \"{plan.Nombre}\" fue actualizado correctamente.";

            return RedirectToAction(nameof(Suscripciones));
        }

        [HttpGet]
        public async Task<IActionResult> Productos(string? busqueda, string? categoria, string? estado)
        {
            List<ProductoAdminViewModel> productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdEmprendimientoNavigation)
                .Include(p => p.ProductoImagene)
                .OrderByDescending(p => p.FechaRegistro)
                .Select(p => new ProductoAdminViewModel
                {
                    Id = p.IdProducto,
                    Nombre = p.Nombre,
                    Emprendimiento = p.IdEmprendimientoNavigation.NombreComercial,
                    Categoria = p.IdCategoriaNavigation != null
                        ? p.IdCategoriaNavigation.Nombre
                        : "Sin categoría",
                    Precio = p.Precio,
                    Estado = p.Estado,
                    Imagen = p.ProductoImagene != null
                        ? p.ProductoImagene.UrlImagen
                        : "/images/logo.jpg",
                    FechaPublicacion = p.FechaPublicacion.HasValue
                        ? p.FechaPublicacion.Value.ToString("dd/MM/yyyy")
                        : p.FechaRegistro.ToString("dd/MM/yyyy"),
                    Reportes = 0,
                    Descripcion = p.Descripcion
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                productos = productos
                    .Where(p =>
                        p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        p.Emprendimiento.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
            {
                productos = productos.Where(p => p.Categoria == categoria).ToList();
            }

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                productos = productos.Where(p => p.Estado == estado).ToList();
            }

            ListadoProductosAdminViewModel modelo = new()
            {
                Busqueda = busqueda,
                FiltroCategoria = categoria,
                FiltroEstado = estado,
                Productos = productos
            };

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> RevisarProducto(int id)
        {
            ProductoAdminViewModel? modelo = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdEmprendimientoNavigation)
                .Include(p => p.ProductoImagene)
                .Where(p => p.IdProducto == id)
                .Select(p => new ProductoAdminViewModel
                {
                    Id = p.IdProducto,
                    Nombre = p.Nombre,
                    Emprendimiento = p.IdEmprendimientoNavigation.NombreComercial,
                    Categoria = p.IdCategoriaNavigation != null
                        ? p.IdCategoriaNavigation.Nombre
                        : "Sin categoría",
                    Precio = p.Precio,
                    Estado = p.Estado,
                    Imagen = p.ProductoImagene != null
                        ? p.ProductoImagene.UrlImagen
                        : "/images/logo.jpg",
                    FechaPublicacion = p.FechaPublicacion.HasValue
                        ? p.FechaPublicacion.Value.ToString("dd/MM/yyyy")
                        : p.FechaRegistro.ToString("dd/MM/yyyy"),
                    Reportes = 0,
                    Descripcion = p.Descripcion
                })
                .FirstOrDefaultAsync();

            if (modelo is null)
            {
                TempData["ErrorAdmin"] = "El producto no está disponible.";
                return RedirectToAction(nameof(Productos));
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoProducto(int id, string estado)
        {
            string[] estadosPermitidos = ["Publicado", "Pendiente", "Inactivo"];

            if (!estadosPermitidos.Contains(estado))
            {
                TempData["ErrorAdmin"] = "El estado seleccionado no es válido.";
                return RedirectToAction(nameof(RevisarProducto), new { id });
            }

            Producto? producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto is null)
            {
                TempData["ErrorAdmin"] = "El producto no está disponible.";
                return RedirectToAction(nameof(Productos));
            }

            producto.Estado = estado;
            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"El estado del producto se actualizó a \"{estado}\".";
            return RedirectToAction(nameof(RevisarProducto), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            Producto? producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto is null)
            {
                TempData["ErrorAdmin"] = "El producto no está disponible.";
                return RedirectToAction(nameof(Productos));
            }

            producto.Estado = "Inactivo";
            producto.PublicadoHechoEnCr = false;
            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = "El producto fue eliminado por contenido inapropiado.";
            return RedirectToAction(nameof(Productos));
        }

        [HttpGet]
        public async Task<IActionResult> Eventos(string estado = "Todos")
        {
            IQueryable<Evento> consulta = _context.Eventos
                .AsNoTracking()
                .Where(e => e.Estado != "Eliminado");

            if (estado != "Todos")
            {
                consulta = consulta.Where(e => e.Estado == estado);
            }

            var eventos = await consulta
                .OrderByDescending(e => e.FechaInicio)
                .Select(e => new
                {
                    e.IdEvento,
                    e.Nombre,
                    e.FechaInicio,
                    e.FechaFin,
                    e.Ubicacion,
                    e.Descripcion,
                    e.CupoMaximo,
                    e.Estado,
                    Inscritos = e.InscripcionesEventos
                        .Count(i => i.Estado != "Cancelada")
                })
                .ToListAsync();

            DateTime ahora = DateTime.Now;

            return View(new ListadoEventosAdminViewModel
            {
                FiltroEstado = estado,
                Eventos = eventos
                    .Select(e => new EventoAdminViewModel
                    {
                        Id = e.IdEvento,
                        Nombre = e.Nombre,
                        Fecha = e.FechaInicio.ToString("dd/MM/yyyy hh:mm tt"),
                        Ubicacion = e.Ubicacion,
                        Descripcion = e.Descripcion,
                        CupoTotal = e.CupoMaximo ?? 0,
                        Inscritos = e.Inscritos,
                        Estado = e.Estado,
                        EsPasado = e.Estado == "Finalizado" ||
                                   (e.FechaFin ?? e.FechaInicio) < ahora
                    })
                    .ToList()
            });
        }

        private async Task CargarOpcionesEventoAsync()
        {
            ViewBag.CategoriasEvento = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Modulo == "Eventos" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            // HU-73: Cargar etiquetas activas para los eventos.
            ViewBag.EtiquetasEvento = await _context.Etiquetas
                .AsNoTracking()
                .Where(e => e.Activa)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.EmprendimientosEvento = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.EstadoAprobacion == "Aprobado")
                .OrderBy(e => e.NombreComercial)
                .ToListAsync();
        }

        private async Task ValidarDatosEventoAsync(
            EventoAdminViewModel modelo,
            List<int> idsEmprendimientos)
        {
            if (modelo.Imagen is { Length: > 0 })
            {
                string extension =
                    Path.GetExtension(modelo.Imagen.FileName).ToLowerInvariant();

                if (!ExtensionesImagenEvento.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagen),
                        "La imagen debe ser JPG, PNG o WEBP.");
                }
                else if (modelo.Imagen.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagen),
                        "La imagen no puede superar los 5 MB.");
                }
            }
            if (modelo.FechaInicio.HasValue &&
                modelo.FechaFin.HasValue &&
                modelo.FechaFin.Value < modelo.FechaInicio.Value)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaFin),
                    "La fecha de finalización no puede ser anterior a la fecha de inicio.");
            }

            if (modelo.IdCategoria.HasValue &&
                !await _context.Categorias.AnyAsync(c =>
                    c.IdCategoria == modelo.IdCategoria.Value &&
                    c.Modulo == "Eventos" &&
                    c.Activa))
            {
                ModelState.AddModelError(
                    nameof(modelo.IdCategoria),
                    "La categoría seleccionada no es válida.");
            }

            if (idsEmprendimientos.Count > 0)
            {
                int validos = await _context.Emprendimientos
                    .CountAsync(e =>
                        idsEmprendimientos.Contains(e.IdEmprendimiento) &&
                        e.EstadoAprobacion == "Aprobado");

                if (validos != idsEmprendimientos.Count)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdsEmprendimientos),
                        "Alguno de los emprendimientos seleccionados no está disponible.");
                }
            }
        }
        private static readonly string[] ExtensionesImagenEvento =
    { ".jpg", ".jpeg", ".png", ".webp" };

        private async Task<string> GuardarImagenEventoAsync(IFormFile imagen)
        {
            string carpeta = Path.Combine(
                _entornoWeb.WebRootPath, "uploads", "eventos");

            Directory.CreateDirectory(carpeta);

            string nombreArchivo =
                $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName).ToLowerInvariant()}";

            using (var flujo = new FileStream(
                Path.Combine(carpeta, nombreArchivo), FileMode.Create))
            {
                await imagen.CopyToAsync(flujo);
            }

            return $"/uploads/eventos/{nombreArchivo}";
        }

        [HttpGet]
        public async Task<IActionResult> CrearEvento()
        {
            ViewData["Titulo"] = "Crear evento";
            await CargarOpcionesEventoAsync();

            return View("FormularioEvento", new EventoAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEvento(EventoAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear evento";

            List<int> idsEmprendimientos =
                modelo.IdsEmprendimientos.Distinct().ToList();

            await ValidarDatosEventoAsync(modelo, idsEmprendimientos);

            // HU-73: Validar las etiquetas seleccionadas.
            List<int> idsEtiquetas = (modelo.IdEtiquetas ?? new List<int>())
                .Distinct()
                .ToList();

            List<Etiqueta> etiquetasSeleccionadas = await _context.Etiquetas
                .Where(e => idsEtiquetas.Contains(e.IdEtiqueta) && e.Activa)
                .ToListAsync();

            if (etiquetasSeleccionadas.Count != idsEtiquetas.Count)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdEtiquetas),
                    "Una o varias etiquetas seleccionadas no son válidas.");
            }

            if (!ModelState.IsValid)
            {
                await CargarOpcionesEventoAsync();

                return View("FormularioEvento", modelo);
            }

            Evento evento = new()
            {
                IdCategoria = modelo.IdCategoria,
                Nombre = modelo.Nombre.Trim(),
                Descripcion = modelo.Descripcion.Trim(),
                FechaInicio = modelo.FechaInicio!.Value,
                FechaFin = modelo.FechaFin,
                Ubicacion = modelo.Ubicacion.Trim(),
                CupoMaximo = modelo.CupoTotal,
                Estado = "Programado",
                FechaRegistro = DateTime.Now,

                // HU-41: Generar automáticamente el identificador QR del evento.
    CodigoQrEvento = Guid.NewGuid()

            };

            foreach (int idEmprendimiento in idsEmprendimientos)
            {
                evento.ParticipacionesEventos.Add(new ParticipacionesEvento
                {
                    IdEmprendimiento = idEmprendimiento,
                    Estado = "Aprobada",
                    FechaSolicitud = DateTime.Now
                });
            }

            if (modelo.Imagen is { Length: > 0 })
            {
                evento.ImagenUrl = await GuardarImagenEventoAsync(modelo.Imagen);
            }

            // HU-73: Asociar las etiquetas seleccionadas al evento.
            foreach (Etiqueta etiqueta in etiquetasSeleccionadas)
            {
                evento.IdEtiqueta.Add(etiqueta);
            }

            _context.Eventos.Add(evento);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"El evento \"{evento.Nombre}\" fue creado correctamente.";

            return RedirectToAction(nameof(Eventos));
        }

        [HttpGet]
        public async Task<IActionResult> EditarEvento(int id)
        {
            ViewData["Titulo"] = "Editar evento";

            Evento? evento = await _context.Eventos
    .AsNoTracking()
    .Include(e => e.ParticipacionesEventos)
    .Include(e => e.IdEtiqueta)
    .FirstOrDefaultAsync(e => e.IdEvento == id);
            if (evento is null || evento.Estado == "Eliminado")
            {
                TempData["ErrorAdmin"] =
                    "El evento no está disponible. Es posible que haya sido eliminado.";

                return RedirectToAction(nameof(Eventos));
            }

            await CargarOpcionesEventoAsync();

            return View("FormularioEvento", new EventoAdminViewModel
            {
                Id = evento.IdEvento,
                Nombre = evento.Nombre,
                FechaInicio = evento.FechaInicio,
                FechaFin = evento.FechaFin,
                Ubicacion = evento.Ubicacion,
                Descripcion = evento.Descripcion,
                IdCategoria = evento.IdCategoria,

                // HU-73: Cargar las etiquetas asociadas al evento.
                IdEtiquetas = evento.IdEtiqueta
    .Select(e => e.IdEtiqueta)
    .ToList(),

                CupoTotal = evento.CupoMaximo ?? 0,
                Estado = evento.Estado,
                ImagenUrl = evento.ImagenUrl,
                IdsEmprendimientos = evento.ParticipacionesEventos
                    .Where(p => p.Estado == "Aprobada")
                    .Select(p => p.IdEmprendimiento)
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEvento(EventoAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar evento";

            Evento? evento = await _context.Eventos
    .Include(e => e.ParticipacionesEventos)
    .Include(e => e.IdEtiqueta)
    .FirstOrDefaultAsync(e => e.IdEvento == modelo.Id);

            if (evento is null || evento.Estado == "Eliminado")
            {
                TempData["ErrorAdmin"] =
                    "El evento no está disponible. Es posible que haya sido eliminado.";

                return RedirectToAction(nameof(Eventos));
            }

            List<int> idsEmprendimientos =
                modelo.IdsEmprendimientos.Distinct().ToList();

            await ValidarDatosEventoAsync(modelo, idsEmprendimientos);

            // HU-73: Validar las etiquetas seleccionadas.
            List<int> idsEtiquetas = (modelo.IdEtiquetas ?? new List<int>())
                .Distinct()
                .ToList();

            List<Etiqueta> etiquetasSeleccionadas = await _context.Etiquetas
                .Where(e => idsEtiquetas.Contains(e.IdEtiqueta) && e.Activa)
                .ToListAsync();

            if (etiquetasSeleccionadas.Count != idsEtiquetas.Count)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdEtiquetas),
                    "Una o varias etiquetas seleccionadas no son válidas.");
            }

            string[] estadosPermitidos =
                { "Programado", "Finalizado", evento.Estado };

            if (!estadosPermitidos.Contains(modelo.Estado))
            {
                ModelState.AddModelError(
                    nameof(modelo.Estado),
                    "El estado seleccionado no es válido.");
            }

            if (!ModelState.IsValid)
            {
                await CargarOpcionesEventoAsync();

                return View("FormularioEvento", modelo);
            }

            // HU-70: Detectar novedades en eventos favoritos.
            bool cambioFecha =
                evento.FechaInicio != modelo.FechaInicio!.Value ||
                evento.FechaFin != modelo.FechaFin;

            bool cambioInformacion =
                evento.Nombre != modelo.Nombre.Trim() ||
                evento.Descripcion != modelo.Descripcion.Trim() ||
                evento.Ubicacion != modelo.Ubicacion.Trim() ||
                evento.Estado != modelo.Estado ||
                evento.IdCategoria != modelo.IdCategoria ||
                evento.CupoMaximo != modelo.CupoTotal ||
                modelo.Imagen is { Length: > 0 };

            bool hayNovedades = cambioFecha || cambioInformacion;

            evento.IdCategoria = modelo.IdCategoria;
            evento.Nombre = modelo.Nombre.Trim();
            evento.Descripcion = modelo.Descripcion.Trim();
            evento.FechaInicio = modelo.FechaInicio.Value;
            evento.FechaFin = modelo.FechaFin;
            evento.Ubicacion = modelo.Ubicacion.Trim();
            evento.CupoMaximo = modelo.CupoTotal;
            evento.Estado = modelo.Estado;

            if (modelo.Imagen is { Length: > 0 })
            {
                evento.ImagenUrl = await GuardarImagenEventoAsync(modelo.Imagen);
            }

            // HU-73: Actualizar las etiquetas asociadas al evento.
            evento.IdEtiqueta.Clear();

            foreach (Etiqueta etiqueta in etiquetasSeleccionadas)
            {
                evento.IdEtiqueta.Add(etiqueta);
            }

            foreach (int idEmprendimiento in idsEmprendimientos)
            {
                ParticipacionesEvento? participacion =
                    evento.ParticipacionesEventos.FirstOrDefault(p =>
                        p.IdEmprendimiento == idEmprendimiento);

                if (participacion is null)
                {
                    evento.ParticipacionesEventos.Add(new ParticipacionesEvento
                    {
                        IdEmprendimiento = idEmprendimiento,
                        Estado = "Aprobada",
                        FechaSolicitud = DateTime.Now
                    });
                }
                else
                {
                    participacion.Estado = "Aprobada";
                }
            }

            foreach (ParticipacionesEvento participacion in
                evento.ParticipacionesEventos.Where(p =>
                    p.Estado == "Aprobada" &&
                    !idsEmprendimientos.Contains(p.IdEmprendimiento)))
            {
                participacion.Estado = "Cancelada";
            }

            // HU-70: Buscar usuarios que tienen este evento como favorito.
            List<int> idsUsuariosFavoritos = new();

            if (hayNovedades)
            {
                idsUsuariosFavoritos = await _context.Favoritos
                    .AsNoTracking()
                    .Where(f => f.IdEvento == evento.IdEvento)
                    .Select(f => f.IdUsuario)
                    .Distinct()
                    .ToListAsync();
            }

            int notificados = 0;

            if (cambioFecha)
            {
                List<int> idsUsuarios = await _context.InscripcionesEventos
                    .Where(i =>
                        i.IdEvento == evento.IdEvento &&
                        (i.Estado == "Pendiente" || i.Estado == "Confirmada"))
                    .Select(i => i.IdUsuario)
                    .Distinct()
                    .ToListAsync();

                string mensaje =
                    $"El evento «{evento.Nombre}» cambió su fecha u horario. " +
                    $"Nuevo inicio: {evento.FechaInicio:dd/MM/yyyy hh:mm tt}." +
                    (evento.FechaFin.HasValue
                        ? $" Finaliza: {evento.FechaFin:dd/MM/yyyy hh:mm tt}."
                        : string.Empty);

                foreach (int idUsuario in idsUsuarios)
                {
                    await _notificaciones.AgregarAsync(new Notificacione
                    {
                        IdUsuario = idUsuario,
                        Titulo = "Cambio en un evento",
                        Mensaje = mensaje,
                        Tipo = "Evento",

                        // HU-70: Referencia para abrir el detalle del evento.
                        TipoReferencia = "Evento",
                        IdReferencia = evento.IdEvento,

                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    });
                }
                    notificados = idsUsuarios.Count;
            }

            // HU-70: Notificar novedades a usuarios que tienen el evento como favorito.
            if (hayNovedades)
            {
                // Evitar duplicar avisos a usuarios inscritos que ya fueron notificados.
                List<int> idsYaNotificados = new();

                if (cambioFecha)
                {
                    idsYaNotificados = await _context.InscripcionesEventos
                        .AsNoTracking()
                        .Where(i =>
                            i.IdEvento == evento.IdEvento &&
                            (i.Estado == "Pendiente" || i.Estado == "Confirmada"))
                        .Select(i => i.IdUsuario)
                        .Distinct()
                        .ToListAsync();
                }

                foreach (int idUsuario in idsUsuariosFavoritos
                    .Except(idsYaNotificados))
                {
                    await _notificaciones.AgregarAsync(new Notificacione
                    {
                        IdUsuario = idUsuario,
                        Titulo = "Novedades de un evento favorito",
                        Mensaje =
    $"El evento «{evento.Nombre}» que guardaste como favorito " +
    "tiene información actualizada. Consulta sus detalles.",
                        Tipo = "Evento",

                        // HU-70: Referencia para abrir el detalle del evento favorito.
                        TipoReferencia = "Evento",
                        IdReferencia = evento.IdEvento,

                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = notificados > 0
                ? $"El evento \"{evento.Nombre}\" fue actualizado y se notificó a {notificados} usuario(s) registrado(s)."
                : $"El evento \"{evento.Nombre}\" fue actualizado correctamente.";

            return RedirectToAction(nameof(Eventos));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEvento(int id, string? motivo = null)
        {
            Evento? evento = await _context.Eventos
                .Include(e => e.InscripcionesEventos)
                .FirstOrDefaultAsync(e => e.IdEvento == id);

            if (evento is null || evento.Estado == "Eliminado")
            {
                TempData["ErrorAdmin"] =
                    "El evento no está disponible. Es posible que ya haya sido eliminado.";

                return RedirectToAction(nameof(Eventos));
            }

            bool esPasado =
                evento.Estado == "Finalizado" ||
                (evento.FechaFin ?? evento.FechaInicio) < DateTime.Now;

            int notificados = 0;

            if (!esPasado && evento.Estado != "Cancelado")
            {
                List<InscripcionesEvento> inscripciones = evento.InscripcionesEventos
                    .Where(i => i.Estado == "Pendiente" || i.Estado == "Confirmada")
                    .ToList();

                string motivoLimpio = motivo?.Trim() ?? string.Empty;

                if (motivoLimpio.Length > 500)
                {
                    motivoLimpio = motivoLimpio[..500];
                }

                string mensaje =
                    $"El evento «{evento.Nombre}» programado para " +
                    $"{evento.FechaInicio:dd/MM/yyyy hh:mm tt} fue cancelado " +
                    "por la organización de Club Creativo." +
                    (motivoLimpio.Length > 0
                        ? $" Motivo: {motivoLimpio}"
                        : string.Empty);

                foreach (InscripcionesEvento inscripcion in inscripciones)
                {
                    inscripcion.Estado = "Cancelada";

                        await _notificaciones.AgregarAsync(new Notificacione
                        {
                            IdUsuario = inscripcion.IdUsuario,
                            Titulo = "Evento cancelado",
                            Mensaje = mensaje,
                        Tipo = "Evento",
                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    });
                }

                notificados = inscripciones
                    .Select(i => i.IdUsuario)
                    .Distinct()
                    .Count();
            }

            evento.Estado = "Eliminado";

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = notificados > 0
                ? $"El evento \"{evento.Nombre}\" fue eliminado y se notificó la cancelación a {notificados} usuario(s) registrado(s)."
                : $"El evento \"{evento.Nombre}\" fue eliminado correctamente.";

            return RedirectToAction(nameof(Eventos));
        }
        [HttpGet]
        public async Task<IActionResult> ParticipantesEvento(int id)
        {
            Evento? evento = await _context.Eventos
                .AsNoTracking()
                .Include(e => e.ParticipacionesEventos)
                    .ThenInclude(p => p.IdEmprendimientoNavigation)
                .Include(e => e.InscripcionesEventos)
                    .ThenInclude(i => i.IdUsuarioNavigation)
                .FirstOrDefaultAsync(e => e.IdEvento == id);

            if (evento is null || evento.Estado == "Eliminado")
            {
                TempData["ErrorAdmin"] =
                    "El evento no está disponible. Es posible que haya sido eliminado.";

                return RedirectToAction(nameof(Eventos));
            }

            return View(new EventoAdminViewModel
            {
                Id = evento.IdEvento,
                Nombre = evento.Nombre,
                Fecha = evento.FechaInicio.ToString("dd/MM/yyyy hh:mm tt"),
                Ubicacion = evento.Ubicacion,
                CupoTotal = evento.CupoMaximo ?? 0,
                Inscritos = evento.InscripcionesEventos
                    .Count(i => i.Estado != "Cancelada"),
                Estado = evento.Estado,
                EmprendimientosAsignados = evento.ParticipacionesEventos
                    .Where(p => p.Estado == "Aprobada")
                    .Select(p => p.IdEmprendimientoNavigation.NombreComercial)
                    .OrderBy(n => n)
                    .ToList(),
                Participantes = evento.InscripcionesEventos
                    .OrderByDescending(i => i.FechaInscripcion)
                    .Select(i => new ParticipanteEventoAdminViewModel
                    {
                        Nombre = i.IdUsuarioNavigation.Nombre + " " +
                                 i.IdUsuarioNavigation.ApellidoPaterno,
                        Correo = i.IdUsuarioNavigation.Correo,
                        Estado = i.Estado
                    })
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AsignarEmprendimiento(int id, string nombreEmprendimiento)
        {
            TempData["MensajeAdmin"] = $"\"{nombreEmprendimiento}\" fue asignado al evento.";
            return RedirectToAction(nameof(ParticipantesEvento), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarEvento(int id, string motivo)
        {
            TempData["MensajeAdmin"] = $"El evento fue cancelado. Motivo: {motivo}";
            return RedirectToAction(nameof(Eventos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerarQrEvento(int id)
        {
            TempData["MensajeAdmin"] = "El código QR del evento fue generado correctamente.";
            return RedirectToAction(nameof(ParticipantesEvento), new { id });
        }

        private async Task CargarOpcionesTallerAsync()
        {
            ViewBag.CategoriasTaller = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Modulo == "Talleres" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            // HU-73: Cargar etiquetas disponibles para los talleres.
            ViewBag.EtiquetasTaller = await _context.Etiquetas
                .AsNoTracking()
                .Where(e => e.Activa)
                .OrderBy(e => e.Nombre)
                .ToListAsync();
        }

        private async Task<string> GuardarImagenTallerAsync(IFormFile imagen)
        {
            string carpeta = Path.Combine(
                _entornoWeb.WebRootPath, "uploads", "talleres");

            Directory.CreateDirectory(carpeta);

            string nombreArchivo =
                $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName).ToLowerInvariant()}";

            using (var flujo = new FileStream(
                Path.Combine(carpeta, nombreArchivo), FileMode.Create))
            {
                await imagen.CopyToAsync(flujo);
            }

            return $"/uploads/talleres/{nombreArchivo}";
        }

        private async Task ValidarTallerAsync(
            TallerAdminViewModel modelo,
            bool publicar,
            int inscritosActivos)
        {
            if (modelo.Imagen is { Length: > 0 })
            {
                string extension =
                    Path.GetExtension(modelo.Imagen.FileName).ToLowerInvariant();

                if (!ExtensionesImagenEvento.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagen), "La imagen debe ser JPG, PNG o WEBP.");
                }
                else if (modelo.Imagen.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagen), "La imagen no puede superar los 5 MB.");
                }
            }

            if (modelo.FechaInicio.HasValue &&
                modelo.FechaFin.HasValue &&
                modelo.FechaFin.Value < modelo.FechaInicio.Value)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaFin),
                    "La fecha de finalización no puede ser anterior a la fecha de inicio.");
            }

            if (modelo.IdCategoria.HasValue &&
                !await _context.Categorias.AnyAsync(c =>
                    c.IdCategoria == modelo.IdCategoria.Value &&
                    c.Modulo == "Talleres" &&
                    c.Activa))
            {
                ModelState.AddModelError(
                    nameof(modelo.IdCategoria),
                    "La categoría seleccionada no es válida.");
            }

            if (modelo.CupoTotal.HasValue && modelo.CupoTotal.Value < inscritosActivos)
            {
                ModelState.AddModelError(
                    nameof(modelo.CupoTotal),
                    $"El cupo máximo no puede ser menor a los {inscritosActivos} participante(s) ya registrado(s).");
            }

            if (!publicar)
            {
                return;
            }

            // HU-73: La categoría es obligatoria al publicar talleres.
            if (!modelo.IdCategoria.HasValue)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdCategoria),
                    "Seleccione una categoría.");
            }

            if (string.IsNullOrWhiteSpace(modelo.Descripcion))
            {
                ModelState.AddModelError(nameof(modelo.Descripcion), "La descripción es obligatoria para publicar.");
            }

            if (!modelo.FechaInicio.HasValue)
            {
                ModelState.AddModelError(nameof(modelo.FechaInicio), "La fecha y hora de inicio son obligatorias para publicar.");
            }

            if (string.IsNullOrWhiteSpace(modelo.Ubicacion))
            {
                ModelState.AddModelError(nameof(modelo.Ubicacion), "La ubicación es obligatoria para publicar.");
            }

            if (!modelo.CupoTotal.HasValue || modelo.CupoTotal.Value < 1)
            {
                ModelState.AddModelError(nameof(modelo.CupoTotal), "Defina un cupo máximo mayor a 0 para publicar.");
            }

            if (string.IsNullOrWhiteSpace(modelo.ResponsableNombre))
            {
                ModelState.AddModelError(nameof(modelo.ResponsableNombre), "Indique la persona responsable del taller.");
            }
        }

        private static void AplicarDatosTaller(Tallere taller, TallerAdminViewModel modelo)
        {
            taller.Nombre = modelo.Nombre.Trim();
            taller.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim();
            taller.FechaInicio = modelo.FechaInicio;
            taller.FechaFin = modelo.FechaFin;
            taller.Ubicacion = string.IsNullOrWhiteSpace(modelo.Ubicacion) ? null : modelo.Ubicacion.Trim();
            taller.IdCategoria = modelo.IdCategoria;
            taller.CupoMaximo = modelo.CupoTotal ?? 0;
            taller.ResponsableNombre = string.IsNullOrWhiteSpace(modelo.ResponsableNombre) ? null : modelo.ResponsableNombre.Trim();
            taller.ResponsableCorreo = string.IsNullOrWhiteSpace(modelo.ResponsableCorreo) ? null : modelo.ResponsableCorreo.Trim();
            taller.ResponsableTelefono = string.IsNullOrWhiteSpace(modelo.ResponsableTelefono) ? null : modelo.ResponsableTelefono.Trim();
        }

        [HttpGet]
        public async Task<IActionResult> Talleres(string estado = "Todos")
        {
            IQueryable<Tallere> consulta = _context.Talleres.AsNoTracking();

            if (estado != "Todos")
            {
                consulta = consulta.Where(t => t.Estado == estado);
            }

            var talleres = await consulta
                .OrderByDescending(t => t.FechaInicio ?? t.FechaRegistro)
                .Select(t => new
                {
                    t.IdTaller,
                    t.Nombre,
                    t.FechaInicio,
                    t.FechaFin,
                    t.CupoMaximo,
                    t.Estado,
                    Inscritos = t.InscripcionesTallers.Count(i => i.Estado != "Cancelada")
                })
                .ToListAsync();

            DateTime ahora = DateTime.Now;

            return View(new ListadoTalleresAdminViewModel
            {
                FiltroEstado = estado,
                Talleres = talleres.Select(t => new TallerAdminViewModel
                {
                    Id = t.IdTaller,
                    Nombre = t.Nombre,
                    Fecha = t.FechaInicio.HasValue
                        ? t.FechaInicio.Value.ToString("dd/MM/yyyy hh:mm tt")
                        : "Sin fecha",
                    CupoTotal = t.CupoMaximo,
                    Inscritos = t.Inscritos,
                    Estado = t.Estado,
                    EsPasado = t.Estado == ReglasTalleres.Finalizado ||
                               (t.FechaInicio.HasValue && (t.FechaFin ?? t.FechaInicio.Value) < ahora)
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> CrearTaller()
        {
            ViewData["Titulo"] = "Crear taller";
            await CargarOpcionesTallerAsync();

            return View("FormularioTaller", new TallerAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearTaller(TallerAdminViewModel modelo, string accion = "publicar")
        {
            ViewData["Titulo"] = "Crear taller";

            bool publicar = accion != "borrador";

            await ValidarTallerAsync(modelo, publicar, 0);

            // HU-73: Validar las etiquetas seleccionadas.
            List<int> idsEtiquetas = (modelo.IdEtiquetas ?? new List<int>())
                .Distinct()
                .ToList();

            List<Etiqueta> etiquetasSeleccionadas = await _context.Etiquetas
                .Where(e => idsEtiquetas.Contains(e.IdEtiqueta) && e.Activa)
                .ToListAsync();

            if (etiquetasSeleccionadas.Count != idsEtiquetas.Count)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdEtiquetas),
                    "Una o varias etiquetas seleccionadas no son válidas.");
            }

            if (publicar &&
                modelo.FechaInicio.HasValue &&
                modelo.FechaInicio.Value < DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaInicio),
                    "La fecha de inicio debe ser futura.");
            }

            if (!ModelState.IsValid)
            {
                await CargarOpcionesTallerAsync();

                return View("FormularioTaller", modelo);
            }

            Tallere taller = new()
            {
                Estado = publicar ? ReglasTalleres.Programado : ReglasTalleres.Borrador,
                FechaRegistro = DateTime.Now,
                // HU-42: Generar el QR automáticamente al publicar el taller.
                // Si es borrador, todavía no se genera.
                CodigoQrTaller = publicar ? Guid.NewGuid() : null
            };

            AplicarDatosTaller(taller, modelo);

            if (modelo.Imagen is { Length: > 0 })
            {
                taller.ImagenUrl = await GuardarImagenTallerAsync(modelo.Imagen);
            }

            // HU-73: Asociar las etiquetas seleccionadas al taller.
            foreach (Etiqueta etiqueta in etiquetasSeleccionadas)
            {
                taller.IdEtiqueta.Add(etiqueta);
            }

            _context.Talleres.Add(taller);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = publicar
                ? $"El taller \"{taller.Nombre}\" fue publicado correctamente."
                : $"El taller \"{taller.Nombre}\" se guardó como borrador. No será visible hasta que lo publiques.";

            return RedirectToAction(nameof(Talleres));
        }

        [HttpGet]
        public async Task<IActionResult> EditarTaller(int id)
        {
            ViewData["Titulo"] = "Editar taller";

            Tallere? taller = await _context.Talleres
     .AsNoTracking()
     .Include(t => t.InscripcionesTallers)
     .Include(t => t.IdEtiqueta)
     .FirstOrDefaultAsync(t => t.IdTaller == id);

            if (taller is null)
            {
                TempData["ErrorAdmin"] = "El taller no está disponible.";

                return RedirectToAction(nameof(Talleres));
            }

            if (taller.Estado == ReglasTalleres.Cancelado || taller.Estado == ReglasTalleres.Finalizado)
            {
                TempData["ErrorAdmin"] = $"Un taller {taller.Estado.ToLowerInvariant()} ya no se puede editar.";

                return RedirectToAction(nameof(Talleres));
            }

            await CargarOpcionesTallerAsync();

            return View("FormularioTaller", new TallerAdminViewModel
            {
                Id = taller.IdTaller,
                Nombre = taller.Nombre,
                Descripcion = taller.Descripcion,
                FechaInicio = taller.FechaInicio,
                FechaFin = taller.FechaFin,
                Ubicacion = taller.Ubicacion,
                IdCategoria = taller.IdCategoria,

                // HU-73: Cargar las etiquetas asociadas al taller.
                IdEtiquetas = taller.IdEtiqueta
    .Select(e => e.IdEtiqueta)
    .ToList(),

                CupoTotal = taller.CupoMaximo,
                ResponsableNombre = taller.ResponsableNombre,
                ResponsableCorreo = taller.ResponsableCorreo,
                ResponsableTelefono = taller.ResponsableTelefono,
                ImagenUrl = taller.ImagenUrl,
                Estado = taller.Estado,
                Inscritos = taller.InscripcionesTallers.Count(i => i.Estado != "Cancelada")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarTaller(TallerAdminViewModel modelo, string accion = "guardar")
        {
            ViewData["Titulo"] = "Editar taller";

            Tallere? taller = await _context.Talleres
    .Include(t => t.InscripcionesTallers)
    .Include(t => t.IdEtiqueta)
    .FirstOrDefaultAsync(t => t.IdTaller == modelo.Id);

            if (taller is null ||
                taller.Estado == ReglasTalleres.Cancelado ||
                taller.Estado == ReglasTalleres.Finalizado)
            {
                TempData["ErrorAdmin"] = "El taller no está disponible para edición.";

                return RedirectToAction(nameof(Talleres));
            }

            bool eraBorrador = taller.Estado == ReglasTalleres.Borrador;
            bool publicar = eraBorrador ? accion == "publicar" : true;
            int inscritosActivos = taller.InscripcionesTallers.Count(i => i.Estado != "Cancelada");

            await ValidarTallerAsync(modelo, publicar, inscritosActivos);

            // HU-73: Validar las etiquetas seleccionadas.
            List<int> idsEtiquetas = (modelo.IdEtiquetas ?? new List<int>())
                .Distinct()
                .ToList();

            List<Etiqueta> etiquetasSeleccionadas = await _context.Etiquetas
                .Where(e => idsEtiquetas.Contains(e.IdEtiqueta) && e.Activa)
                .ToListAsync();

            if (etiquetasSeleccionadas.Count != idsEtiquetas.Count)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdEtiquetas),
                    "Una o varias etiquetas seleccionadas no son válidas.");
            }

            if (eraBorrador &&
                publicar &&
                modelo.FechaInicio.HasValue &&
                modelo.FechaInicio.Value < DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaInicio),
                    "La fecha de inicio debe ser futura.");
            }

            if (!ModelState.IsValid)
            {
                modelo.Estado = taller.Estado;
                modelo.Inscritos = inscritosActivos;
                modelo.ImagenUrl = taller.ImagenUrl;
                await CargarOpcionesTallerAsync();

                return View("FormularioTaller", modelo);
            }

            int cupoAnterior = taller.CupoMaximo;

            // HU-70: Detectar cambios en la información del taller.
            bool cambioInformacionTaller =
                taller.Nombre != modelo.Nombre.Trim() ||
                (taller.Descripcion ?? "") != (modelo.Descripcion?.Trim() ?? "") ||
                taller.FechaInicio != modelo.FechaInicio ||
                taller.FechaFin != modelo.FechaFin ||
                (taller.Ubicacion ?? "") != (modelo.Ubicacion?.Trim() ?? "") ||
                taller.IdCategoria != modelo.IdCategoria ||
                taller.CupoMaximo != modelo.CupoTotal ||
                modelo.Imagen is { Length: > 0 };

            // Notificar únicamente cambios de talleres ya publicados.
            bool hayNovedadesTaller =
                !eraBorrador && cambioInformacionTaller;

            AplicarDatosTaller(taller, modelo);

            if (eraBorrador && publicar)
            {
                taller.Estado = ReglasTalleres.Programado;
                // HU-42: Generar el QR cuando un borrador se publica.
                // No reemplazar un código que ya exista.
                if (!taller.CodigoQrTaller.HasValue)
                {
                    taller.CodigoQrTaller = Guid.NewGuid();
                }
            }

            if (modelo.Imagen is { Length: > 0 })
            {
                taller.ImagenUrl = await GuardarImagenTallerAsync(modelo.Imagen);
            }

            // HU-73: Actualizar las etiquetas asociadas al taller.
            taller.IdEtiqueta.Clear();

            foreach (Etiqueta etiqueta in etiquetasSeleccionadas)
            {
                taller.IdEtiqueta.Add(etiqueta);
            }

            // HU-70: Notificar novedades a usuarios con este taller favorito.
            if (hayNovedadesTaller)
            {
                List<int> idsUsuariosFavoritos = await _context.Favoritos
                    .AsNoTracking()
                    .Where(f => f.IdTaller == taller.IdTaller)
                    .Select(f => f.IdUsuario)
                    .Distinct()
                    .ToListAsync();

                foreach (int idUsuario in idsUsuariosFavoritos)
                {
                    await _notificaciones.AgregarAsync(new Notificacione
                    {
                        IdUsuario = idUsuario,
                        Titulo = "Novedades de un taller favorito",
                        Mensaje =
        $"El taller «{taller.Nombre}» que guardaste como favorito " +
        "tiene información actualizada. Consulta sus detalles.",
                        Tipo = "Taller",

                        // HU-70: Referencia para abrir el detalle del taller favorito.
                        TipoReferencia = "Taller",
                        IdReferencia = taller.IdTaller,

                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    });
                }
            }

            await _context.SaveChangesAsync();

            if (eraBorrador)
            {
                TempData["MensajeAdmin"] = publicar
                    ? $"El taller \"{taller.Nombre}\" fue publicado correctamente."
                    : $"El borrador \"{taller.Nombre}\" fue guardado.";
            }
            else if (taller.CupoMaximo > cupoAnterior)
            {
                TempData["MensajeAdmin"] =
                    $"El taller \"{taller.Nombre}\" fue actualizado. El cupo pasó de {cupoAnterior} a {taller.CupoMaximo}; " +
                    $"quedan {taller.CupoMaximo - inscritosActivos} lugar(es) disponible(s).";
            }
            else
            {
                TempData["MensajeAdmin"] = $"El taller \"{taller.Nombre}\" fue actualizado correctamente.";
            }

            return RedirectToAction(nameof(Talleres));
        }

        [HttpGet]
        public async Task<IActionResult> InscritosTaller(int id)
        {
            Tallere? taller = await _context.Talleres
                .AsNoTracking()
                .Include(t => t.InscripcionesTallers)
                    .ThenInclude(i => i.IdUsuarioNavigation)
                .FirstOrDefaultAsync(t => t.IdTaller == id);

            if (taller is null)
            {
                TempData["ErrorAdmin"] = "El taller no está disponible.";

                return RedirectToAction(nameof(Talleres));
            }

            return View(new TallerAdminViewModel
            {
                Id = taller.IdTaller,
                Nombre = taller.Nombre,
                CupoTotal = taller.CupoMaximo,
                Estado = taller.Estado,
                QrGenerado = taller.CodigoQrTaller.HasValue,
                Inscritos = taller.InscripcionesTallers.Count(i => i.Estado != "Cancelada"),
                ListaInscritos = taller.InscripcionesTallers
                    .Where(i => i.Estado != "Cancelada")
                    .OrderBy(i => i.FechaInscripcion)
                    .Select(i => new InscritoTallerAdminViewModel
                    {
                        Nombre = i.IdUsuarioNavigation.Nombre + " " + i.IdUsuarioNavigation.ApellidoPaterno,
                        Correo = i.IdUsuarioNavigation.Correo,
                        Estado = i.Estado,
                        Fecha = i.FechaInscripcion.ToString("dd/MM/yyyy hh:mm tt")
                    })
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarTaller(int id, string motivo)
        {
            Tallere? taller = await _context.Talleres.FirstOrDefaultAsync(t => t.IdTaller == id);

            if (taller is null ||
                taller.Estado == ReglasTalleres.Cancelado ||
                taller.Estado == ReglasTalleres.Finalizado)
            {
                TempData["ErrorAdmin"] = "El taller no está disponible para cancelación.";

                return RedirectToAction(nameof(Talleres));
            }

            taller.Estado = ReglasTalleres.Cancelado;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"El taller \"{taller.Nombre}\" fue cancelado. Motivo: {motivo}";

            return RedirectToAction(nameof(Talleres));
        }

        // HU-42: Generar el código QR de un taller.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarQrTaller(int id)
        {
            Tallere? taller = await _context.Talleres
                .FirstOrDefaultAsync(t => t.IdTaller == id);

            if (taller == null)
            {
                TempData["ErrorAdmin"] = "El taller no existe.";
                return RedirectToAction(nameof(Talleres));
            }

            if (taller.Estado != ReglasTalleres.Programado)
            {
                TempData["ErrorAdmin"] =
                    "Solo se puede generar el código QR de un taller programado.";

                return RedirectToAction(nameof(InscritosTaller), new { id });
            }

            if (taller.CodigoQrTaller.HasValue)
            {
                TempData["MensajeAdmin"] =
                    "Este taller ya tiene un código QR generado.";

                return RedirectToAction(nameof(InscritosTaller), new { id });
            }

            // HU-42: Generar un identificador único para el taller.
            taller.CodigoQrTaller = Guid.NewGuid();

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                "El código QR del taller fue generado correctamente.";

            return RedirectToAction(nameof(InscritosTaller), new { id });
        }

        // HU-42: Regenerar el código QR de un taller.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerarQrTaller(int id)
        {
            Tallere? taller = await _context.Talleres
                .FirstOrDefaultAsync(t => t.IdTaller == id);

            if (taller == null)
            {
                TempData["ErrorAdmin"] =
                    "El taller no existe o no está disponible.";

                return RedirectToAction(nameof(Talleres));
            }

            if (taller.Estado != ReglasTalleres.Programado)
            {
                TempData["ErrorAdmin"] =
                    "Solo se puede regenerar el código QR de un taller programado.";

                return RedirectToAction(nameof(InscritosTaller), new { id });
            }

            if (!taller.CodigoQrTaller.HasValue)
            {
                TempData["ErrorAdmin"] =
                    "Este taller todavía no tiene un código QR generado.";

                return RedirectToAction(nameof(InscritosTaller), new { id });
            }

            // HU-42: Reemplazar el identificador anterior por uno nuevo.
            taller.CodigoQrTaller = Guid.NewGuid();

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                "El código QR del taller fue regenerado correctamente.";

            return RedirectToAction(nameof(InscritosTaller), new { id });
        }

        // HU-42: Descargar el código QR de un taller.
        [HttpGet]
        public async Task<IActionResult> DescargarQrTaller(int id)
        {
            Tallere? taller = await _context.Talleres
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdTaller == id);

            if (taller == null)
            {
                TempData["ErrorAdmin"] =
                    "El taller no existe o no está disponible.";

                return RedirectToAction(nameof(Talleres));
            }

            if (!taller.CodigoQrTaller.HasValue)
            {
                TempData["ErrorAdmin"] =
                    "Este taller todavía no tiene un código QR generado.";

                return RedirectToAction(nameof(InscritosTaller), new { id });
            }

            // HU-42: Crear el enlace que contendrá el QR del taller.
            string contenidoQr = Url.Action(
                "RegistrarAsistenciaQrTaller",
                "Usuario",
                new
                {
                    id = taller.IdTaller,
                    codigo = taller.CodigoQrTaller.Value
                },
                Request.Scheme
            )!;

            // Generar la imagen PNG utilizando QRCoder.
            using QRCodeGenerator generador = new();

            using QRCodeData datosQr = generador.CreateQrCode(
                contenidoQr,
                QRCodeGenerator.ECCLevel.Q
            );

            PngByteQRCode codigoQr = new(datosQr);

            byte[] imagenQr = codigoQr.GetGraphic(20);

            return File(
                imagenQr,
                "image/png",
                $"QR_Taller_{taller.IdTaller}.png"
            );
        }



        [HttpGet]
        public async Task<IActionResult> Ventas(string estado = "Todas")
        {
            // HU-61: Consultar órdenes reales de SQL Server.
            var ordenes = await _context.Ventas
                .AsNoTracking()
                .OrderByDescending(v => v.FechaVenta)
                .Select(v => new
                {
                    v.IdVenta,
                    v.NumeroOrden,
                    v.FechaVenta,
                    v.Total,
                    v.Estado,
                    v.EstadoPago,
                    v.MetodoPago,

                    NombreCliente =
                        v.IdUsuarioNavigation.Nombre + " " +
                        v.IdUsuarioNavigation.ApellidoPaterno,

                    Emprendimientos = v.VentaDetalles
                        .Select(d =>
                            d.IdProductoNavigation
                                .IdEmprendimientoNavigation.NombreComercial)
                        .Distinct()
                        .ToList()
                })
                .ToListAsync();

            // Convertir las órdenes al ViewModel existente.
            List<VentaAdminViewModel> ventas = ordenes
                .Select(v => new VentaAdminViewModel
                {
                    Id = v.IdVenta,
                    NumeroOrden = v.NumeroOrden,
                    Cliente = v.NombreCliente,

                    Emprendimiento = v.Emprendimientos.Any()
                        ? string.Join(", ", v.Emprendimientos)
                        : "Sin emprendimiento",

                    Fecha = v.FechaVenta.ToString("dd/MM/yyyy"),
                    Total = v.Total,
                    Estado = v.Estado,
                    MetodoPago = v.MetodoPago
                })
                .ToList();

            // HU-61: Indicadores reales de ventas.
            ListadoVentasAdminViewModel modelo = new()
            {
                FiltroEstado = estado,

                Ventas = estado == "Todas"
                    ? ventas
                    : ventas.Where(v => v.Estado == estado).ToList(),

                TotalVendido = ordenes
                    .Where(v =>
                        v.Estado != "Cancelada" &&
                        (v.EstadoPago == "Aprobado" ||
                         v.Estado == "Entregada"))
                    .Sum(v => v.Total),

                TotalOrdenes = ventas.Count,

                TotalPendientes = ventas.Count(v =>
                    v.Estado == "Pendiente"),

                TotalCanceladas = ventas.Count(v =>
                    v.Estado == "Cancelada")
            };

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleVenta(int id)
        {
            // HU-61: Consultar el detalle real de la orden.
            var venta = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.IdVenta == id)
                .Select(v => new
                {
                    v.IdVenta,
                    v.NumeroOrden,
                    v.FechaVenta,
                    v.Total,
                    v.Estado,
                    v.EstadoPago,
                    v.MetodoPago,
                    v.NumeroReferenciaSinpe,

                    Cliente = v.IdUsuarioNavigation.Nombre + " " +
                              v.IdUsuarioNavigation.ApellidoPaterno,

                    Emprendimientos = v.VentaDetalles
                        .Select(d =>
                            d.IdProductoNavigation
                                .IdEmprendimientoNavigation.NombreComercial)
                        .Distinct()
                        .ToList(),

                    Productos = v.VentaDetalles
                        .Select(d => new
                        {
                            Producto = d.IdProductoNavigation.Nombre,
                            d.Cantidad,
                            d.PrecioUnitario
                        })
                        .ToList(),

                    MotivoCancelacion = v.VentasCancelada != null
                        ? v.VentasCancelada.MotivoCancelacion
                        : null
                })
                .FirstOrDefaultAsync();

            if (venta == null)
            {
                return NotFound();
            }

            VentaAdminViewModel modelo = new()
            {
                Id = venta.IdVenta,
                NumeroOrden = venta.NumeroOrden,
                Cliente = venta.Cliente,

                Emprendimiento = venta.Emprendimientos.Any()
                    ? string.Join(", ", venta.Emprendimientos)
                    : "Sin emprendimiento",

                Fecha = venta.FechaVenta.ToString("dd/MM/yyyy"),
                Total = venta.Total,
                Estado = venta.Estado,
                EstadoPago = venta.EstadoPago,
                MetodoPago = venta.MetodoPago,
                NumeroReferenciaSinpe = venta.NumeroReferenciaSinpe,
                MotivoCancelacion = venta.MotivoCancelacion,

                Items = venta.Productos
                    .Select(p => new ItemVentaAdminViewModel
                    {
                        Producto = p.Producto,
                        Cantidad = p.Cantidad,
                        PrecioUnitario = p.PrecioUnitario
                    })
                    .ToList()
            };

            return View(modelo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarPagoVentaSinpe(int id)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                Venta? venta = await _context.Ventas
                    .FirstOrDefaultAsync(v => v.IdVenta == id);

                if (venta == null)
                {
                    await transaccion.RollbackAsync();
                    return NotFound();
                }

                if (venta.MetodoPago != "SINPE Móvil" ||
                    venta.EstadoPago != "Pendiente" ||
                    venta.Estado == "Cancelada" ||
                    venta.Estado == "Entregada" ||
                    string.IsNullOrWhiteSpace(venta.NumeroReferenciaSinpe))
                {
                    await transaccion.RollbackAsync();

                    TempData["ErrorAdmin"] =
                        "No se puede aprobar el pago de esta orden.";

                    return RedirectToAction(
                        nameof(DetalleVenta), new { id });
                }

                venta.EstadoPago = "Aprobado";

                if (venta.Estado == "Pendiente")
                {
                    venta.Estado = "Pagada";
                }

                var existeAcumulacion = await _context.MovimientosPuntos
                    .AnyAsync(m =>
                        m.IdVenta == venta.IdVenta &&
                        m.TipoMovimiento == "Acumulacion");

                if (!existeAcumulacion)
                {
                    var configuracionPuntos = await _context.ConfiguracionPuntos
                        .AsNoTracking()
                        .OrderByDescending(c => c.FechaActualizacion)
                        .FirstOrDefaultAsync();

                    if (configuracionPuntos == null ||
                        configuracionPuntos.ColonesPorPunto <= 0)
                    {
                        throw new InvalidOperationException(
                            "No existe una configuración válida para los puntos.");
                    }

                    int puntosGanados = (int)Math.Floor(
                        venta.Subtotal / configuracionPuntos.ColonesPorPunto);

                    if (puntosGanados > 0)
                    {
                        var movimientoPuntos = new MovimientosPunto
                        {
                            IdUsuario = venta.IdUsuario,
                            TipoMovimiento = "Acumulacion",
                            Puntos = puntosGanados,
                            Motivo = "Puntos ganados por la compra " + venta.NumeroOrden,
                            FechaMovimiento = DateTime.Now,
                            IdVenta = venta.IdVenta
                        };

                        _context.MovimientosPuntos.Add(movimientoPuntos);
                    }
                }


                await _notificaciones.AgregarAsync(new Notificacione
                {
                    IdUsuario = venta.IdUsuario,
                    NumeroOrden = venta.NumeroOrden,
                    Titulo = "Pago confirmado",
                    Mensaje =
                        $"¡Tu pago SINPE Móvil del pedido {venta.NumeroOrden} " +
                        "fue aprobado correctamente! " +
                        "Los puntos correspondientes a tu compra ya fueron acreditados. " +
                        "Tu pedido continuará con el proceso de preparación.",
                    Tipo = "Compra",
                    FechaEnvio = DateTime.Now,
                    Leida = false,
                    FechaLectura = null
                });



                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["MensajeAdmin"] =
                    "El pago SINPE Móvil fue aprobado correctamente.";

                return RedirectToAction(
                    nameof(DetalleVenta), new { id });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                TempData["ErrorAdmin"] =
                    "Ocurrió un error al aprobar el pago.";

                return RedirectToAction(
                    nameof(DetalleVenta), new { id });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarVenta(int id, string motivo)
        {
            int? idAdministrador =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idAdministrador.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            if (string.IsNullOrWhiteSpace(motivo) ||
                motivo.Trim().Length > 300)
            {
                TempData["ErrorAdmin"] =
                    "Debe indicar un motivo de cancelación de máximo 300 caracteres.";

                return RedirectToAction(nameof(DetalleVenta), new { id });
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                Venta? venta = await _context.Ventas
                    .Include(v => v.VentasCancelada)
                    .Include(v => v.VentaDetalles)
                    .FirstOrDefaultAsync(v => v.IdVenta == id);

                if (venta is null)
                {
                    await transaccion.RollbackAsync();
                    return NotFound();
                }

                if (venta.Estado == "Cancelada" ||
                    venta.VentasCancelada != null)
                {
                    await transaccion.RollbackAsync();

                    TempData["ErrorAdmin"] =
                        "Esta orden ya fue cancelada.";

                    return RedirectToAction(nameof(DetalleVenta), new { id });
                }

                if (venta.Estado == "Entregada")
                {
                    await transaccion.RollbackAsync();

                    TempData["ErrorAdmin"] =
                        "No se puede cancelar una orden entregada.";

                    return RedirectToAction(nameof(DetalleVenta), new { id });
                }

                if (venta.EstadoPago != "Pendiente")
                {
                    await transaccion.RollbackAsync();

                    TempData["ErrorAdmin"] =
                        "Solo se pueden cancelar órdenes con pago pendiente.";

                    return RedirectToAction(nameof(DetalleVenta), new { id });
                }

                if (!venta.VentaDetalles.Any())
                {
                    await transaccion.RollbackAsync();

                    TempData["ErrorAdmin"] =
                        "La orden no tiene productos registrados.";

                    return RedirectToAction(nameof(DetalleVenta), new { id });
                }

                var cantidadesPorProducto = venta.VentaDetalles
                    .GroupBy(d => d.IdProducto)
                    .Select(g => new
                    {
                        IdProducto = g.Key,
                        Cantidad = g.Sum(d => d.Cantidad)
                    })
                    .ToList();

                var idsProductos = cantidadesPorProducto
                    .Select(p => p.IdProducto)
                    .ToList();

                var productos = await _context.Productos
                    .Where(p => idsProductos.Contains(p.IdProducto))
                    .ToListAsync();

                if (productos.Count != idsProductos.Count ||
                    cantidadesPorProducto.Any(p => p.Cantidad <= 0))
                {
                    throw new InvalidOperationException(
                        "Los productos de la orden no son válidos.");
                }

                DateTime fechaActual = DateTime.Now;

                foreach (var detalle in cantidadesPorProducto)
                {
                    Producto producto = productos.First(p =>
                        p.IdProducto == detalle.IdProducto);

                    if (producto.TipoPublicacion != "Producto")
                    {
                        continue;
                    }

                    int stockAnterior = producto.StockActual;
                    int stockNuevo = checked(
                        stockAnterior + detalle.Cantidad);

                    producto.StockActual = stockNuevo;

                    _context.MovimientosInventarios.Add(
                        new MovimientosInventario
                        {
                            IdProducto = producto.IdProducto,
                            IdUsuario = idAdministrador.Value,
                            TipoMovimiento = "Devolucion",
                            Cantidad = detalle.Cantidad,
                            StockAnterior = stockAnterior,
                            StockNuevo = stockNuevo,
                            Observacion =
                                "Devolución por cancelación administrativa " +
                                "de la orden " + venta.NumeroOrden,
                            FechaMovimiento = fechaActual
                        });
                }

                int puntosAcumulados =
                    await _context.MovimientosPuntos
                        .Where(m =>
                            m.IdVenta == venta.IdVenta &&
                            m.TipoMovimiento == "Acumulacion")
                        .SumAsync(m => (int?)m.Puntos) ?? 0;

                bool existeReversion =
                    await _context.MovimientosPuntos
                        .AnyAsync(m =>
                            m.IdVenta == venta.IdVenta &&
                            m.TipoMovimiento == "Reversion");

                if (puntosAcumulados > 0 && !existeReversion)
                {
                    _context.MovimientosPuntos.Add(
                        new MovimientosPunto
                        {
                            IdUsuario = venta.IdUsuario,
                            TipoMovimiento = "Reversion",
                            Puntos = -puntosAcumulados,
                            Motivo =
                                "Reversión de puntos por cancelación " +
                                "administrativa de la orden " +
                                venta.NumeroOrden,
                            IdVenta = venta.IdVenta,
                            FechaMovimiento = fechaActual
                        });
                }

                venta.Estado = "Cancelada";

                if (venta.MetodoPago == "SINPE Móvil")
                {
                    venta.EstadoPago = "Rechazado";
                }


                venta.VentasCancelada = new VentasCancelada
                {
                    IdVenta = venta.IdVenta,
                    MotivoCancelacion = motivo.Trim(),
                    FechaCancelacion = fechaActual
                };

                var idsEmprendedores = await _context.Productos
                    .AsNoTracking()
                    .Where(p => idsProductos.Contains(p.IdProducto))
                    .Select(p =>
                        p.IdEmprendimientoNavigation.IdUsuarioPropietario)
                    .Distinct()
                    .ToListAsync();

                // Se guardan las notificaciones de plataforma
                // junto con la cancelación.
                _context.Notificaciones.Add(new Notificacione
                {
                    IdUsuario = venta.IdUsuario,
                    NumeroOrden = venta.NumeroOrden,
                    Titulo = "Pedido cancelado",
                    Mensaje =
                        $"Tu pedido {venta.NumeroOrden} fue cancelado " +
                        $"por administración. Motivo: {motivo.Trim()}",
                    Tipo = "Compra",
                    FechaEnvio = fechaActual,
                    Leida = false,
                    FechaLectura = null
                });

                foreach (int idEmprendedor in idsEmprendedores)
                {
                    _context.Notificaciones.Add(new Notificacione
                    {
                        IdUsuario = idEmprendedor,
                        NumeroOrden = venta.NumeroOrden,
                        Titulo = "Pedido cancelado",
                        Mensaje =
                            $"La orden {venta.NumeroOrden} fue cancelada " +
                            "por administración.",
                        Tipo = "Compra",
                        FechaEnvio = fechaActual,
                        Leida = false,
                        FechaLectura = null
                    });
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["MensajeAdmin"] =
                    $"La orden {venta.NumeroOrden} fue cancelada correctamente.";

                return RedirectToAction(nameof(DetalleVenta), new { id });
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                TempData["ErrorAdmin"] =
                    "No fue posible cancelar la orden. " +
                    "No se confirmaron los cambios.";

                return RedirectToAction(nameof(DetalleVenta), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PuntosRecompensas(string pestana = "Recompensas")
        {
            ConfiguracionPunto? configuracion = await _context.ConfiguracionPuntos
                .AsNoTracking()
                .OrderByDescending(c => c.IdConfiguracion)
                .FirstOrDefaultAsync();

            List<RecompensaAdminViewModel> recompensas = await _context.Recompensas
                .AsNoTracking()
                .OrderBy(r => r.CostoPuntos)
                .ThenBy(r => r.Nombre)
                .Select(r => new RecompensaAdminViewModel
                {
                    Id = r.IdRecompensa,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion ?? string.Empty,
                    CostoPuntos = r.CostoPuntos,
                    Activa = r.Activa,
                    CantidadCanjes = r.CanjesRecompensas.Count()
                })
                .ToListAsync();

            var movimientos = await _context.MovimientosPuntos
                .AsNoTracking()
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(200)
                .Select(m => new
                {
                    Usuario = m.IdUsuarioNavigation.Nombre + " " + m.IdUsuarioNavigation.ApellidoPaterno,
                    m.TipoMovimiento,
                    m.Puntos,
                    m.Motivo,
                    m.FechaMovimiento
                })
                .ToListAsync();

            return View(new PuntosRecompensasAdminViewModel
            {
                Pestana = pestana,
                Recompensas = recompensas,
                Movimientos = movimientos
                    .Select(m => new MovimientoPuntosAdminViewModel
                    {
                        Usuario = m.Usuario,
                        Tipo = m.TipoMovimiento switch
                        {
                            "Acumulacion" => "Ganados",
                            "Canje" => "Canjeados",
                            "Reversion" => "Reversión",
                            _ => m.TipoMovimiento
                        },
                        Puntos = m.Puntos,
                        Motivo = m.Motivo,
                        Fecha = m.FechaMovimiento.ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToList(),
                ColonesPorPunto = configuracion?.ColonesPorPunto ?? 1000m
            });
        }

        // HU-40: Guardar configuración de puntos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracionPuntos(
            decimal ColonesPorPunto)
        {
            // Validar que la tasa sea mayor a cero
            if (ColonesPorPunto <= 0 || ColonesPorPunto > 99999999.99m)
            {
                TempData["Error"] =
                    "La tasa de conversión debe ser mayor a cero y estar dentro del rango permitido.";

                return RedirectToAction("PuntosRecompensas",
                    new { pestana = "Configuracion" });
            }

            // Obtener la configuración actual
            var configuracion = await _context.ConfiguracionPuntos
                .OrderByDescending(c => c.IdConfiguracion)
                .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                // Crear la configuración si todavía no existe
                configuracion = new ConfiguracionPunto
                {
                    ColonesPorPunto = ColonesPorPunto,
                    FechaActualizacion = DateTime.Now
                };

                _context.ConfiguracionPuntos.Add(configuracion);
            }
            else
            {
                // Actualizar la configuración existente
                configuracion.ColonesPorPunto = ColonesPorPunto;
                configuracion.FechaActualizacion = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "La configuración de puntos se guardó correctamente.";

            return RedirectToAction("PuntosRecompensas",
                new { pestana = "Configuracion" });
        }

        [HttpGet]
        public IActionResult CrearRecompensa()
        {
            ViewData["Titulo"] = "Crear recompensa";

            return View("FormularioRecompensa", new RecompensaAdminViewModel());
        }

        private async Task ValidarRecompensaAsync(RecompensaAdminViewModel modelo)
        {
            modelo.Nombre = (modelo.Nombre ?? string.Empty).Trim();
            modelo.Descripcion = (modelo.Descripcion ?? string.Empty).Trim();

            if (modelo.Nombre.Length == 0)
            {
                ModelState.AddModelError(nameof(modelo.Nombre), "El nombre es obligatorio.");
            }
            else
            {
                string nombre = modelo.Nombre;
                int idActual = modelo.Id;

                bool duplicada = await _context.Recompensas
                    .AnyAsync(r => r.IdRecompensa != idActual && r.Nombre == nombre);

                if (duplicada)
                {
                    ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe una recompensa con este nombre.");
                }
            }

            if (modelo.CostoPuntos <= 0)
            {
                ModelState.AddModelError(nameof(modelo.CostoPuntos), "El costo debe ser mayor que cero.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRecompensa(RecompensaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear recompensa";

            ModelState.Remove(nameof(RecompensaAdminViewModel.CantidadCanjes));
            ModelState.Remove(nameof(RecompensaAdminViewModel.Descripcion));

            await ValidarRecompensaAsync(modelo);

            if (!ModelState.IsValid)
            {
                return View("FormularioRecompensa", modelo);
            }

            Recompensa recompensa = new()
            {
                Nombre = modelo.Nombre,
                Descripcion = modelo.Descripcion.Length == 0 ? null : modelo.Descripcion,
                CostoPuntos = modelo.CostoPuntos,
                Icono = "bi-gift-fill",
                Activa = modelo.Activa
            };

            _context.Recompensas.Add(recompensa);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"La recompensa \"{recompensa.Nombre}\" fue creada correctamente.";

            return RedirectToAction(nameof(PuntosRecompensas));
        }

        [HttpGet]
        public async Task<IActionResult> EditarRecompensa(int id)
        {
            ViewData["Titulo"] = "Editar recompensa";

            RecompensaAdminViewModel? modelo = await _context.Recompensas
                .AsNoTracking()
                .Where(r => r.IdRecompensa == id)
                .Select(r => new RecompensaAdminViewModel
                {
                    Id = r.IdRecompensa,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion ?? string.Empty,
                    CostoPuntos = r.CostoPuntos,
                    Activa = r.Activa,
                    CantidadCanjes = r.CanjesRecompensas.Count()
                })
                .FirstOrDefaultAsync();

            if (modelo is null)
            {
                TempData["ErrorAdmin"] = "La recompensa no está disponible.";

                return RedirectToAction(nameof(PuntosRecompensas));
            }

            return View("FormularioRecompensa", modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarRecompensa(RecompensaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar recompensa";

            ModelState.Remove(nameof(RecompensaAdminViewModel.CantidadCanjes));
            ModelState.Remove(nameof(RecompensaAdminViewModel.Descripcion));

            Recompensa? recompensa = await _context.Recompensas
                .FirstOrDefaultAsync(r => r.IdRecompensa == modelo.Id);

            if (recompensa is null)
            {
                TempData["ErrorAdmin"] = "La recompensa no está disponible.";

                return RedirectToAction(nameof(PuntosRecompensas));
            }

            await ValidarRecompensaAsync(modelo);

            if (!ModelState.IsValid)
            {
                return View("FormularioRecompensa", modelo);
            }

            recompensa.Nombre = modelo.Nombre;
            recompensa.Descripcion = modelo.Descripcion.Length == 0 ? null : modelo.Descripcion;
            recompensa.CostoPuntos = modelo.CostoPuntos;
            recompensa.Activa = modelo.Activa;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"La recompensa \"{recompensa.Nombre}\" fue actualizada correctamente.";

            return RedirectToAction(nameof(PuntosRecompensas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarRecompensa(int id)
        {
            Recompensa? recompensa = await _context.Recompensas
                .Include(r => r.CanjesRecompensas)
                .FirstOrDefaultAsync(r => r.IdRecompensa == id);

            if (recompensa is null)
            {
                TempData["ErrorAdmin"] = "La recompensa no está disponible.";

                return RedirectToAction(nameof(PuntosRecompensas));
            }

            if (recompensa.CanjesRecompensas.Count > 0)
            {
                TempData["ErrorAdmin"] =
                    $"La recompensa \"{recompensa.Nombre}\" ya tiene canjes registrados y no se puede eliminar. Puede desactivarla.";

                return RedirectToAction(nameof(PuntosRecompensas));
            }

            string nombre = recompensa.Nombre;

            _context.Recompensas.Remove(recompensa);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"La recompensa \"{nombre}\" fue eliminada correctamente.";

            return RedirectToAction(nameof(PuntosRecompensas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustarPuntos(AjustarPuntosViewModel modelo)
        {
            string correo = (modelo.Correo ?? string.Empty).Trim();
            string motivo = (modelo.Motivo ?? string.Empty).Trim();

            if (correo.Length == 0 || motivo.Length == 0 || modelo.Puntos == 0)
            {
                TempData["ErrorAdmin"] = "Complete el correo, una cantidad de puntos distinta de cero y el motivo.";

                return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
            }

            if (motivo.Length > 300)
            {
                TempData["ErrorAdmin"] = "El motivo no puede superar los 300 caracteres.";

                return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
            }

            Usuario? usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Correo == correo);

            if (usuario is null)
            {
                TempData["ErrorAdmin"] = $"No existe un usuario con el correo {correo}.";

                return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
            }

            int saldo = await _context.MovimientosPuntos
                .Where(m => m.IdUsuario == usuario.IdUsuario)
                .SumAsync(m => (int?)m.Puntos) ?? 0;

            if (saldo + modelo.Puntos < 0)
            {
                TempData["ErrorAdmin"] =
                    $"El usuario solo tiene {saldo} punto(s). No se pueden restar {Math.Abs(modelo.Puntos)}.";

                return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
            }

            _context.MovimientosPuntos.Add(new MovimientosPunto
            {
                IdUsuario = usuario.IdUsuario,
                TipoMovimiento = "Ajuste",
                Puntos = modelo.Puntos,
                Motivo = motivo,
                FechaMovimiento = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"Se ajustaron {modelo.Puntos} punto(s) a {usuario.Nombre} {usuario.ApellidoPaterno}. Saldo actual: {saldo + modelo.Puntos}.";

            return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
        }

        [HttpGet]
        public async Task<IActionResult> AsistenciaQr()
        {
            List<ActividadQrAdminViewModel> actividades =
                await _context.Eventos
                    .AsNoTracking()
                    .Where(e => e.Estado != "Eliminado")
                    .OrderByDescending(e => e.FechaInicio)
                    .Select(e => new ActividadQrAdminViewModel
                    {
                        Id = e.IdEvento,
                        Nombre = e.Nombre,
                        Tipo = "Evento",
                        Fecha = e.FechaInicio.ToString("dd/MM/yyyy"),
                        QrGenerado = e.CodigoQrEvento.HasValue,

                        Asistentes = _context.InscripcionesEventos
                            .Where(i =>
                                i.IdEvento == e.IdEvento &&
                                i.Estado == "Confirmada")
                            .Select(i => new AsistenteQrAdminViewModel
                            {
                                IdUsuario = i.IdUsuario,
                                Nombre = i.IdUsuarioNavigation.Nombre + " " +
                                         i.IdUsuarioNavigation.ApellidoPaterno,
                                Correo = i.IdUsuarioNavigation.Correo,

                                Presente = _context.RegistrosAsistencia.Any(a =>
                                    a.IdUsuario == i.IdUsuario &&
                                    a.IdEvento == e.IdEvento)
                            })
                            .ToList()
                    })
                    .ToListAsync();
            // HU-42: Incluir talleres en el listado de Asistencia QR.
            List<ActividadQrAdminViewModel> talleres =
                await _context.Talleres
                    .AsNoTracking()
                    .Where(t => t.Estado == ReglasTalleres.Programado)
                    .OrderByDescending(t => t.FechaInicio)
                    .Select(t => new ActividadQrAdminViewModel
                    {
                        Id = t.IdTaller,
                        Nombre = t.Nombre,
                        Tipo = "Taller",
                        Fecha = t.FechaInicio.HasValue
                            ? t.FechaInicio.Value.ToString("dd/MM/yyyy")
                            : "Sin fecha",

                        QrGenerado = t.CodigoQrTaller.HasValue,

                        Asistentes = _context.InscripcionesTallers
                            .Where(i =>
                                i.IdTaller == t.IdTaller &&
                                i.Estado != "Cancelada")
                            .Select(i => new AsistenteQrAdminViewModel
                            {
                                IdUsuario = i.IdUsuario,
                                Nombre = i.IdUsuarioNavigation.Nombre + " " +
                                         i.IdUsuarioNavigation.ApellidoPaterno,
                                Correo = i.IdUsuarioNavigation.Correo,
                                Presente = _context.RegistrosAsistencia.Any(a =>
                                    a.IdUsuario == i.IdUsuario &&
                                    a.IdTaller == t.IdTaller)
                            })
                            .ToList()
                    })
                    .ToListAsync();

            // Unir eventos y talleres sin modificar sus registros.
            actividades.AddRange(talleres);

            return View(actividades);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleAsistenciaQr(int id)
        {
            Evento? evento = await _context.Eventos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdEvento == id &&
                    e.Estado != "Eliminado");

            if (evento == null)
            {
                TempData["ErrorAdmin"] = "El evento no existe o no está disponible.";

                return RedirectToAction(nameof(AsistenciaQr));
            }

            List<AsistenteQrAdminViewModel> asistentes =
                await _context.InscripcionesEventos
                    .AsNoTracking()
                    .Where(i =>
                        i.IdEvento == id &&
                        i.Estado == "Confirmada")
                    .Select(i => new AsistenteQrAdminViewModel
                    {
                        IdUsuario = i.IdUsuario,
                        Nombre = i.IdUsuarioNavigation.Nombre + " " +
                                 i.IdUsuarioNavigation.ApellidoPaterno,
                        Correo = i.IdUsuarioNavigation.Correo,
                        Presente = _context.RegistrosAsistencia.Any(a =>
                            a.IdUsuario == i.IdUsuario &&
                            a.IdEvento == id)
                    })
                    .ToListAsync();

            ActividadQrAdminViewModel modelo = new()
            {
                Id = evento.IdEvento,
                Nombre = evento.Nombre,
                Tipo = "Evento",
                Fecha = evento.FechaInicio.ToString("dd/MM/yyyy"),
                QrGenerado = evento.CodigoQrEvento.HasValue,
                Asistentes = asistentes
            };

            return View(modelo);
        }

        // HU-44: Consultar el reporte de asistencia de un evento o taller.
        [HttpGet]
        public async Task<IActionResult> ReporteAsistencia(int id, string tipo)
        {
            ReporteAsistenciaViewModel modelo = new()
            {
                IdActividad = id,
                TipoActividad = tipo
            };

            if (tipo == "Evento")
            {
                Evento? evento = await _context.Eventos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdEvento == id &&
                        e.Estado != "Eliminado");

                if (evento is null)
                {
                    TempData["ErrorAdmin"] = "El evento no existe o no está disponible.";
                    return RedirectToAction(nameof(AsistenciaQr));
                }

                modelo.NombreActividad = evento.Nombre;

                modelo.Asistentes = await _context.RegistrosAsistencia
                    .AsNoTracking()
                    .Where(a => a.IdEvento == id)
                    .OrderBy(a => a.FechaHora)
                    .Select(a => new AsistenteReporteViewModel
                    {
                        Nombre = a.IdUsuarioNavigation.Nombre + " " +
                                 a.IdUsuarioNavigation.ApellidoPaterno,
                        Correo = a.IdUsuarioNavigation.Correo,
                        FechaHora = a.FechaHora
                    })
                    .ToListAsync();
            }
            else if (tipo == "Taller")
            {
                Tallere? taller = await _context.Talleres
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdTaller == id);

                if (taller is null)
                {
                    TempData["ErrorAdmin"] = "El taller no existe.";
                    return RedirectToAction(nameof(AsistenciaQr));
                }

                modelo.NombreActividad = taller.Nombre;

                modelo.Asistentes = await _context.RegistrosAsistencia
                    .AsNoTracking()
                    .Where(a => a.IdTaller == id)
                    .OrderBy(a => a.FechaHora)
                    .Select(a => new AsistenteReporteViewModel
                    {
                        Nombre = a.IdUsuarioNavigation.Nombre + " " +
                                 a.IdUsuarioNavigation.ApellidoPaterno,
                        Correo = a.IdUsuarioNavigation.Correo,
                        FechaHora = a.FechaHora
                    })
                    .ToListAsync();
            }
            else
            {
                TempData["ErrorAdmin"] = "El tipo de actividad no es válido.";
                return RedirectToAction(nameof(AsistenciaQr));
            }

            return View(modelo);
        }

        // HU-44: Descargar reporte individual de asistencia en Excel
        [HttpGet]
        public async Task<IActionResult> DescargarReporteAsistencia(int id, string tipo)
        {
            if (id <= 0 || (tipo != "Evento" && tipo != "Taller"))
            {
                return BadRequest("La actividad seleccionada no es válida.");
            }

            string nombreActividad;

            if (tipo == "Evento")
            {
                var evento = await _context.Eventos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdEvento == id &&
                        e.Estado != "Eliminado");

                if (evento == null)
                    return NotFound("El evento no existe.");

                nombreActividad = evento.Nombre;
            }
            else
            {
                var taller = await _context.Talleres
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdTaller == id);

                if (taller == null)
                    return NotFound("El taller no existe.");

                nombreActividad = taller.Nombre;
            }

            var asistentes = await _context.RegistrosAsistencia
                .AsNoTracking()
                .Where(a => tipo == "Evento"
                    ? a.IdEvento == id
                    : a.IdTaller == id)
                .OrderBy(a => a.FechaHora)
                .Select(a => new
                {
                    Nombre = a.IdUsuarioNavigation.Nombre + " " +
                             a.IdUsuarioNavigation.ApellidoPaterno,
                    Correo = a.IdUsuarioNavigation.Correo,
                    a.FechaHora
                })
                .ToListAsync();

            // HU-44: Generar archivo Excel con formato.
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Asistencia");

            // Título principal.
            hoja.Range("A1:D1").Merge();
            hoja.Cell("A1").Value = "CLUB CREATIVO - REPORTE DE ASISTENCIA";
            hoja.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.FromHtml("#F97316");
            hoja.Range("A1:D1").Style.Font.FontColor = XLColor.White;
            hoja.Range("A1:D1").Style.Font.Bold = true;
            hoja.Range("A1:D1").Style.Font.FontSize = 16;
            hoja.Row(1).Height = 32;

            // Información de la actividad.
            hoja.Cell("A3").Value = "Tipo de actividad:";
            hoja.Cell("B3").Value = tipo;

            hoja.Cell("A4").Value = "Actividad:";
            hoja.Cell("B4").Value = nombreActividad;

            hoja.Cell("A5").Value = "Total de asistencias:";
            hoja.Cell("B5").Value = asistentes.Count;

            hoja.Range("A3:A5").Style.Font.Bold = true;

            // Encabezados de la tabla.
            hoja.Cell("A7").Value = "Nombre del asistente";
            hoja.Cell("B7").Value = "Correo electrónico";
            hoja.Cell("C7").Value = "Fecha de asistencia";
            hoja.Cell("D7").Value = "Hora de asistencia";

            var encabezados = hoja.Range("A7:D7");
            encabezados.Style.Fill.BackgroundColor = XLColor.FromHtml("#F97316");
            encabezados.Style.Font.FontColor = XLColor.White;
            encabezados.Style.Font.Bold = true;
            encabezados.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Row(7).Height = 25;

            // Datos reales de asistencia.
            int fila = 8;

            foreach (var asistente in asistentes)
            {
                hoja.Cell(fila, 1).Value = asistente.Nombre;
                hoja.Cell(fila, 2).Value = asistente.Correo;
                hoja.Cell(fila, 3).Value = asistente.FechaHora.Date;
                hoja.Cell(fila, 4).Value = asistente.FechaHora.TimeOfDay;

                hoja.Cell(fila, 3).Style.DateFormat.Format = "dd/MM/yyyy";
                hoja.Cell(fila, 4).Style.DateFormat.Format = "hh:mm AM/PM";

                if (fila % 2 == 0)
                {
                    hoja.Range(fila, 1, fila, 4).Style.Fill.BackgroundColor =
                        XLColor.FromHtml("#FFF7ED");
                }

                fila++;
            }

            // Mensaje cuando no hay asistencias.
            if (asistentes.Count == 0)
            {
                hoja.Range("A8:D8").Merge();
                hoja.Cell("A8").Value =
                    "Todavía no hay asistencias registradas para esta actividad.";
                hoja.Cell("A8").Style.Font.Italic = true;
            }

            // Bordes de la tabla.
            int ultimaFila = Math.Max(8, fila - 1);
            hoja.Range(7, 1, ultimaFila, 4).Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;
            hoja.Range(7, 1, ultimaFila, 4).Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            // Anchos de columna para evitar textos cortados.
            hoja.Column(1).Width = 30;
            hoja.Column(2).Width = 42;
            hoja.Column(3).Width = 24;
            hoja.Column(4).Width = 24;

            hoja.SheetView.FreezeRows(7);

            // Generar y descargar el archivo.
            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);

            string nombreArchivo =
                $"Reporte_Asistencia_{tipo}_{id}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                memoria.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarQrAsistencia(int id)
        {
            Evento? evento = await _context.Eventos
                .FirstOrDefaultAsync(e =>
                    e.IdEvento == id &&
                    e.Estado != "Eliminado");

            if (evento == null)
            {
                TempData["ErrorAdmin"] = "El evento no existe o no está disponible.";
                return RedirectToAction(nameof(AsistenciaQr));
            }

            if (evento.CodigoQrEvento.HasValue)
            {
                TempData["MensajeAdmin"] = "Este evento ya tiene un código QR generado.";
                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // HU-41: Generar el identificador QR del evento.
            evento.CodigoQrEvento = Guid.NewGuid();

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = "El código QR del evento fue generado correctamente.";

            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerarQrAsistencia(int id)
        {
            Evento? evento = await _context.Eventos
                .FirstOrDefaultAsync(e =>
                    e.IdEvento == id &&
                    e.Estado != "Eliminado");

            if (evento == null)
            {
                TempData["ErrorAdmin"] = "El evento no existe o no está disponible.";
                return RedirectToAction(nameof(AsistenciaQr));
            }

            if (!evento.CodigoQrEvento.HasValue)
            {
                TempData["ErrorAdmin"] = "Este evento todavía no tiene un código QR generado.";
                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // HU-41: Reemplazar el identificador QR anterior por uno nuevo.
            evento.CodigoQrEvento = Guid.NewGuid();

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = "El código QR del evento fue regenerado correctamente.";

            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> DescargarQrAsistencia(int id)
        {
            Evento? evento = await _context.Eventos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdEvento == id &&
                    e.Estado != "Eliminado");

            if (evento == null)
            {
                TempData["ErrorAdmin"] = "El evento no existe o no está disponible.";
                return RedirectToAction(nameof(AsistenciaQr));
            }

            if (!evento.CodigoQrEvento.HasValue)
            {
                TempData["ErrorAdmin"] = "Este evento todavía no tiene un código QR generado.";
                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // HU-41: Crear el contenido del QR con el identificador del evento
            // y su código único vigente.
            string contenidoQr = Url.Action(
     "RegistrarAsistenciaQr",
     "Usuario",
     new
     {
         id = evento.IdEvento,
         codigo = evento.CodigoQrEvento.Value
     },
     Request.Scheme
 )!;

            // Generar la imagen PNG mediante QRCoder.
            using QRCodeGenerator generador = new();

            using QRCodeData datosQr = generador.CreateQrCode(
                contenidoQr,
                QRCodeGenerator.ECCLevel.Q
            );

            PngByteQRCode codigoQr = new(datosQr);

            byte[] imagenQr = codigoQr.GetGraphic(20);

            return File(
                imagenQr,
                "image/png",
                $"QR_Evento_{evento.IdEvento}.png"
            );
        }

        [HttpGet]
        public async Task<IActionResult> Notificaciones(string pestana = "Enviar")
        {
            return View(new NotificacionesAdminViewModel
            {
                Pestana = pestana,
                Historial = await ObtenerHistorialNotificacionesAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarNotificacion(
    [Bind(Prefix = "Nueva")] NotificacionFormViewModel modelo)
        {
            string titulo = (modelo.Titulo ?? string.Empty).Trim();
            string mensaje = (modelo.Mensaje ?? string.Empty).Trim();

            if (titulo.Length > 150)
            {
                ModelState.AddModelError(
                    "Nueva.Titulo",
                    "El título no puede superar los 150 caracteres.");
            }

            if (mensaje.Length > 1000)
            {
                ModelState.AddModelError(
                    "Nueva.Mensaje",
                    "El mensaje no puede superar los 1000 caracteres.");
            }

            List<int> idsDestino = new();
            string destinatario = "Todos los usuarios";

            if (ModelState.IsValid)
            {
                IQueryable<Usuario> usuarios = _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Estado == "Activo");

                if (modelo.TipoDestinatario == "Individual")
                {
                    string correo = (modelo.UsuarioDestino ?? string.Empty)
                        .Trim()
                        .ToLowerInvariant();

                    if (string.IsNullOrWhiteSpace(correo))
                    {
                        ModelState.AddModelError(
                            "Nueva.UsuarioDestino",
                            "Ingrese el correo del usuario.");
                    }
                    else
                    {
                        idsDestino = await usuarios
                            .Where(u => u.Correo.ToLower() == correo)
                            .Select(u => u.IdUsuario)
                            .ToListAsync();

                        if (idsDestino.Count == 0)
                        {
                            ModelState.AddModelError(
                                "Nueva.UsuarioDestino",
                                "No se encontró un usuario activo con ese correo.");
                        }

                        destinatario = $"Usuario: {correo}";
                    }
                }
                else if (modelo.TipoDestinatario == "Grupo")
                {
                    string? nombreRol = modelo.GrupoDestino switch
                    {
                        "Usuarios" => "Usuario",
                        "Emprendedores" => "Emprendedor",
                        "Administradores" => "Administrador",
                        _ => null
                    };

                    if (nombreRol is null)
                    {
                        ModelState.AddModelError(
                            "Nueva.GrupoDestino",
                            "Seleccione un grupo.");
                    }
                    else
                    {
                        idsDestino = await usuarios
                            .Where(u => u.UsuarioRoles.Any(ur =>
                                ur.IdRolNavigation.Activo &&
                                ur.IdRolNavigation.Nombre == nombreRol))
                            .Select(u => u.IdUsuario)
                            .ToListAsync();

                        if (idsDestino.Count == 0)
                        {
                            ModelState.AddModelError(
                                string.Empty,
                                "El grupo seleccionado no tiene usuarios activos.");
                        }

                        destinatario = $"Grupo: {modelo.GrupoDestino}";
                    }
                }
                else
                {
                    idsDestino = await usuarios
                        .Select(u => u.IdUsuario)
                        .ToListAsync();
                }
            }

            if (!ModelState.IsValid)
            {
                return View(nameof(Notificaciones), new NotificacionesAdminViewModel
                {
                    Pestana = "Enviar",
                    Nueva = modelo,
                    Historial = await ObtenerHistorialNotificacionesAsync()
                });
            }

            DateTime ahora = DateTime.Now;

            Notificacione Crear(int idUsuario) => new()
            {
                IdUsuario = idUsuario,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = "Anuncio",
                FechaEnvio = ahora,
                Leida = false,
                FechaLectura = null
            };

            if (modelo.TipoDestinatario == "Individual")
            {
                await _notificaciones.AgregarAsync(Crear(idsDestino[0]));
            }
            else
            {
                foreach (int idUsuario in idsDestino)
                {
                    _context.Notificaciones.Add(Crear(idUsuario));
                }
            }

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"La notificación \"{titulo}\" fue enviada a {destinatario} " +
                $"({idsDestino.Count} usuario(s)).";

            return RedirectToAction(nameof(Notificaciones), new { pestana = "Historial" });
        }
        private async Task<List<NotificacionEnviadaViewModel>> ObtenerHistorialNotificacionesAsync()
        {
            var envios = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.Tipo == "Anuncio")
                .GroupBy(n => new { n.Titulo, n.Mensaje, n.FechaEnvio })
                .Select(g => new
                {
                    g.Key.Titulo,
                    g.Key.Mensaje,
                    g.Key.FechaEnvio,
                    Total = g.Count(),
                    IdUsuario = g.Min(n => n.IdUsuario)
                })
                .OrderByDescending(e => e.FechaEnvio)
                .Take(50)
                .ToListAsync();

            List<int> idsIndividuales = envios
                .Where(e => e.Total == 1)
                .Select(e => e.IdUsuario)
                .ToList();

            Dictionary<int, string> correos = await _context.Usuarios
                .AsNoTracking()
                .Where(u => idsIndividuales.Contains(u.IdUsuario))
                .ToDictionaryAsync(u => u.IdUsuario, u => u.Correo);

            return envios
                .Select((e, i) => new NotificacionEnviadaViewModel
                {
                    Id = i + 1,
                    Titulo = e.Titulo,
                    Mensaje = e.Mensaje,
                    Destinatario = e.Total == 1 &&
                                   correos.TryGetValue(e.IdUsuario, out string? correo)
                        ? $"Usuario: {correo}"
                        : "Varios usuarios",
                    Fecha = e.FechaEnvio.ToString("dd/MM/yyyy HH:mm"),
                    TotalDestinatarios = e.Total
                })
                .ToList();
        }
        [HttpGet]
        public async Task<IActionResult> Galerias()
        {
            return View(await ObtenerGaleriasAsync());
        }

        [HttpGet]
        public async Task<IActionResult> CrearGaleria()
        {
            ViewData["Titulo"] = "Crear galería";

            GaleriaAdminViewModel modelo = new();
            await CargarContenidosGaleriaAsync(modelo);

            return View("FormularioGaleria", modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearGaleria(GaleriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear galería";

            if (modelo.Estado != "Publicada" && modelo.Estado != "Inactiva")
            {
                ModelState.AddModelError(
                    nameof(modelo.Estado),
                    "Seleccione un estado válido.");
            }

            string tipo = string.Empty;
            int idContenido = 0;
            bool contenidoValido = false;

            if (!string.IsNullOrWhiteSpace(modelo.Contenido))
            {
                if (!TryObtenerContenidoGaleria(modelo.Contenido, out tipo, out idContenido))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Contenido),
                        "Seleccione un contenido válido.");
                }
                else if (!await ExisteContenidoGaleriaAsync(tipo, idContenido))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Contenido),
                        "El contenido seleccionado ya no existe.");
                }
                else
                {
                    contenidoValido = true;
                }
            }

            List<IFormFile> imagenes = modelo.NuevasImagenes
                .Where(i => i.Length > 0)
                .ToList();

            if (imagenes.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.NuevasImagenes),
                    "Seleccione al menos una imagen.");
            }
            else
            {
                List<string> errores = await ValidarImagenesGaleriaAsync(
                    imagenes,
                    contenidoValido ? tipo : null,
                    idContenido);

                foreach (string error in errores)
                {
                    ModelState.AddModelError(nameof(modelo.NuevasImagenes), error);
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarContenidosGaleriaAsync(modelo);
                return View("FormularioGaleria", modelo);
            }

            List<string> urlsGuardadas = new();

            try
            {
                Galeria galeria = new()
                {
                    Nombre = modelo.Nombre.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion)
                        ? null
                        : modelo.Descripcion.Trim(),
                    Estado = modelo.Estado,
                    FechaCreacion = DateTime.Now,
                    IdEvento = tipo == "Evento" ? idContenido : (int?)null,
                    IdEmprendimiento = tipo == "Emprendimiento" ? idContenido : (int?)null,
                    IdProducto = tipo == "Producto" ? idContenido : (int?)null
                };

                byte orden = 1;

                foreach (IFormFile imagen in imagenes)
                {
                    string url = await GuardarImagenGaleriaAsync(imagen);
                    urlsGuardadas.Add(url);

                    string nombreOriginal = Path.GetFileName(imagen.FileName);

                    galeria.GaleriaImagenes.Add(new GaleriaImagene
                    {
                        UrlImagen = url,
                        NombreArchivo = nombreOriginal.Length > 255
                            ? nombreOriginal[..255]
                            : nombreOriginal,
                        EsPortada = orden == 1,
                        OrdenVisual = orden
                    });

                    orden++;
                }

                _context.Galerias.Add(galeria);
                await _context.SaveChangesAsync();
            }
            catch
            {
                foreach (string url in urlsGuardadas)
                {
                    EliminarArchivoGaleria(url);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al guardar la galería. Intente nuevamente.");

                await CargarContenidosGaleriaAsync(modelo);
                return View("FormularioGaleria", modelo);
            }

            TempData["MensajeAdmin"] =
                $"La galería \"{modelo.Nombre.Trim()}\" fue publicada correctamente.";

            return RedirectToAction(nameof(Galerias));
        }

        [HttpGet]
        public async Task<IActionResult> EditarGaleria(int id)
        {
            GaleriaAdminViewModel? galeria =
                (await ObtenerGaleriasAsync(id)).FirstOrDefault();

            if (galeria is null)
            {
                TempData["MensajeAdmin"] = "La galería solicitada no existe.";
                return RedirectToAction(nameof(Galerias));
            }

            ViewData["Titulo"] = "Editar galería";
            await CargarContenidosGaleriaAsync(galeria);

            return View("FormularioGaleria", galeria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarGaleria(
            GaleriaAdminViewModel modelo,
            List<int>? imagenesEliminar)
        {
            Galeria? galeria = await _context.Galerias
                .Include(g => g.GaleriaImagenes)
                .FirstOrDefaultAsync(g => g.IdGaleria == modelo.Id);

            if (galeria is null)
            {
                TempData["MensajeAdmin"] = "La galería ya no existe.";
                return RedirectToAction(nameof(Galerias));
            }

            List<int> idsEliminar = (imagenesEliminar ?? new List<int>())
                .Distinct()
                .Where(id => galeria.GaleriaImagenes.Any(i => i.IdImagen == id))
                .ToList();

            if (modelo.Estado != "Publicada" && modelo.Estado != "Inactiva")
            {
                ModelState.AddModelError(
                    nameof(modelo.Estado),
                    "Seleccione un estado válido.");
            }

            string tipo = string.Empty;
            int idContenido = 0;
            bool contenidoValido = false;

            if (!string.IsNullOrWhiteSpace(modelo.Contenido))
            {
                if (!TryObtenerContenidoGaleria(modelo.Contenido, out tipo, out idContenido))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Contenido),
                        "Seleccione un contenido válido.");
                }
                else if (!await ExisteContenidoGaleriaAsync(tipo, idContenido))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Contenido),
                        "El contenido seleccionado ya no existe.");
                }
                else
                {
                    contenidoValido = true;
                }
            }

            List<IFormFile> nuevas = modelo.NuevasImagenes
                .Where(i => i.Length > 0)
                .ToList();

            if (nuevas.Count > 0)
            {
                List<string> errores = await ValidarImagenesGaleriaAsync(
                    nuevas,
                    contenidoValido ? tipo : null,
                    idContenido,
                    idsEliminar);

                foreach (string error in errores)
                {
                    ModelState.AddModelError(nameof(modelo.NuevasImagenes), error);
                }
            }

            if (galeria.GaleriaImagenes.Count - idsEliminar.Count + nuevas.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.NuevasImagenes),
                    "La galería debe conservar al menos una imagen.");
            }

            if (!ModelState.IsValid)
            {
                return await MostrarFormularioGaleriaAsync(modelo);
            }

            List<string> urlsNuevas = new();
            List<string> urlsEliminadas = new();

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            try
            {
                galeria.Nombre = modelo.Nombre.Trim();
                galeria.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion)
                    ? null
                    : modelo.Descripcion.Trim();
                galeria.Estado = modelo.Estado;
                galeria.IdEvento = tipo == "Evento" ? idContenido : (int?)null;
                galeria.IdEmprendimiento = tipo == "Emprendimiento" ? idContenido : (int?)null;
                galeria.IdProducto = tipo == "Producto" ? idContenido : (int?)null;

                foreach (GaleriaImagene imagen in galeria.GaleriaImagenes
                    .Where(i => idsEliminar.Contains(i.IdImagen))
                    .ToList())
                {
                    urlsEliminadas.Add(imagen.UrlImagen);
                    _context.GaleriaImagenes.Remove(imagen);
                }

                int orden = galeria.GaleriaImagenes
                    .Select(i => (int)i.OrdenVisual)
                    .DefaultIfEmpty(0)
                    .Max();

                foreach (IFormFile imagen in nuevas)
                {
                    string url = await GuardarImagenGaleriaAsync(imagen);
                    urlsNuevas.Add(url);

                    string nombreOriginal = Path.GetFileName(imagen.FileName);
                    orden = Math.Min(orden + 1, 255);

                    _context.GaleriaImagenes.Add(new GaleriaImagene
                    {
                        IdGaleria = galeria.IdGaleria,
                        UrlImagen = url,
                        NombreArchivo = nombreOriginal.Length > 255
                            ? nombreOriginal[..255]
                            : nombreOriginal,
                        EsPortada = false,
                        OrdenVisual = (byte)orden
                    });
                }

                await _context.SaveChangesAsync();

                List<GaleriaImagene> restantes = await _context.GaleriaImagenes
                    .Where(i => i.IdGaleria == galeria.IdGaleria)
                    .OrderBy(i => i.OrdenVisual)
                    .ThenBy(i => i.IdImagen)
                    .ToListAsync();

                if (restantes.Count > 0 && !restantes.Any(i => i.EsPortada))
                {
                    restantes[0].EsPortada = true;
                    await _context.SaveChangesAsync();
                }

                await transaccion.CommitAsync();
            }
            catch
            {
                await transaccion.RollbackAsync();

                foreach (string url in urlsNuevas)
                {
                    EliminarArchivoGaleria(url);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al actualizar la galería. Intente nuevamente.");

                return await MostrarFormularioGaleriaAsync(modelo);
            }

            foreach (string url in urlsEliminadas)
            {
                try
                {
                    EliminarArchivoGaleria(url);
                }
                catch
                {
                    // El archivo huérfano no impide que la actualización sea correcta.
                }
            }

            TempData["MensajeAdmin"] =
                $"La galería \"{modelo.Nombre.Trim()}\" fue actualizada correctamente.";

            return RedirectToAction(nameof(Galerias));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarGaleria(int id)
        {
            Galeria? galeria = await _context.Galerias
                .Include(g => g.GaleriaImagenes)
                .FirstOrDefaultAsync(g => g.IdGaleria == id);

            if (galeria is null)
            {
                TempData["MensajeAdmin"] = "La galería ya no existe.";
                return RedirectToAction(nameof(Galerias));
            }

            string nombre = galeria.Nombre;

            List<string> urls = galeria.GaleriaImagenes
                .Select(i => i.UrlImagen)
                .ToList();

            try
            {
                _context.GaleriaImagenes.RemoveRange(galeria.GaleriaImagenes);
                _context.Galerias.Remove(galeria);

                await _context.SaveChangesAsync();
            }
            catch
            {
                TempData["MensajeAdmin"] =
                    "No fue posible eliminar la galería. Intente nuevamente.";

                return RedirectToAction(nameof(Galerias));
            }

            foreach (string url in urls)
            {
                try
                {
                    EliminarArchivoGaleria(url);
                }
                catch
                {
                    // Un archivo huérfano no impide que la eliminación sea correcta.
                }
            }

            TempData["MensajeAdmin"] =
                $"La galería \"{nombre}\" se eliminó exitosamente.";

            return RedirectToAction(nameof(Galerias));
        }
        private async Task<IActionResult> MostrarFormularioGaleriaAsync(
    GaleriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = modelo.Id == 0 ? "Crear galería" : "Editar galería";

            GaleriaAdminViewModel? actual =
                (await ObtenerGaleriasAsync(modelo.Id)).FirstOrDefault();

            modelo.Imagenes = actual?.Imagenes ?? [];

            await CargarContenidosGaleriaAsync(modelo);

            return View("FormularioGaleria", modelo);
        }
        private static bool TryObtenerContenidoGaleria(
    string? valor,
    out string tipo,
    out int id)
        {
            tipo = string.Empty;
            id = 0;

            string[] partes = (valor ?? string.Empty).Split(':');

            if (partes.Length != 2 ||
                !int.TryParse(partes[1], out id) ||
                id <= 0 ||
                (partes[0] != "Evento" &&
                 partes[0] != "Emprendimiento" &&
                 partes[0] != "Producto"))
            {
                return false;
            }

            tipo = partes[0];
            return true;
        }

        private async Task<bool> ExisteContenidoGaleriaAsync(string tipo, int id)
        {
            return tipo switch
            {
                "Evento" => await _context.Eventos.AnyAsync(e => e.IdEvento == id),
                "Producto" => await _context.Productos.AnyAsync(p => p.IdProducto == id),
                _ => await _context.Emprendimientos.AnyAsync(e => e.IdEmprendimiento == id)
            };
        }

        private async Task CargarContenidosGaleriaAsync(GaleriaAdminViewModel modelo)
        {
            var eventos = await _context.Eventos
                .AsNoTracking()
                .OrderByDescending(e => e.FechaInicio)
                .Select(e => new { e.IdEvento, e.Nombre })
                .ToListAsync();

            var emprendimientos = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.EstadoAprobacion == "Aprobado")
                .OrderBy(e => e.NombreComercial)
                .Select(e => new { e.IdEmprendimiento, e.NombreComercial })
                .ToListAsync();

            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Estado == "Publicado")
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.IdProducto, p.Nombre })
                .ToListAsync();

            SelectListGroup grupoEventos = new() { Name = "Eventos" };
            SelectListGroup grupoEmprendimientos = new() { Name = "Emprendimientos" };
            SelectListGroup grupoProductos = new() { Name = "Productos" };

            List<SelectListItem> contenidos = new();

            contenidos.AddRange(eventos.Select(e => new SelectListItem
            {
                Value = $"Evento:{e.IdEvento}",
                Text = e.Nombre,
                Group = grupoEventos
            }));

            contenidos.AddRange(emprendimientos.Select(e => new SelectListItem
            {
                Value = $"Emprendimiento:{e.IdEmprendimiento}",
                Text = e.NombreComercial,
                Group = grupoEmprendimientos
            }));

            contenidos.AddRange(productos.Select(p => new SelectListItem
            {
                Value = $"Producto:{p.IdProducto}",
                Text = p.Nombre,
                Group = grupoProductos
            }));

            modelo.Contenidos = contenidos;
        }

        // Devuelve los errores de validación de las imágenes (formato, tamaño y duplicados).
        // Si tipo es null, no se comparan con las imágenes ya registradas.
        private async Task<List<string>> ValidarImagenesGaleriaAsync(
            List<IFormFile> imagenes,
                       string? tipo,
            int idContenido,
            List<int>? excluirImagenes = null)
        {
            List<string> errores = new();

            if (imagenes.Count > 20)
            {
                errores.Add("Puede cargar un máximo de 20 imágenes por vez.");
                return errores;
            }

            foreach (IFormFile imagen in imagenes)
            {
                string extension =
                    Path.GetExtension(imagen.FileName).ToLowerInvariant();

                if (!ExtensionesImagenEvento.Contains(extension))
                {
                    errores.Add(
                        $"El archivo \"{Path.GetFileName(imagen.FileName)}\" debe ser JPG, PNG o WEBP.");
                }
                else if (imagen.Length > 5 * 1024 * 1024)
                {
                    errores.Add(
                        $"El archivo \"{Path.GetFileName(imagen.FileName)}\" supera los 5 MB.");
                }
            }

            HashSet<string> existentes = new(StringComparer.OrdinalIgnoreCase);

            if (tipo is not null)
            {
                IQueryable<GaleriaImagene> consulta =
                    _context.GaleriaImagenes.AsNoTracking();

                consulta = tipo switch
                {
                    "Evento" => consulta.Where(i =>
                        i.IdGaleriaNavigation.IdEvento == idContenido),
                    "Producto" => consulta.Where(i =>
                        i.IdGaleriaNavigation.IdProducto == idContenido),
                    _ => consulta.Where(i =>
                        i.IdGaleriaNavigation.IdEmprendimiento == idContenido)
                };

                if (excluirImagenes is { Count: > 0 })
                {
                    consulta = consulta.Where(i =>
                        !excluirImagenes.Contains(i.IdImagen));
                }

                List<string?> nombres = await consulta
                    .Select(i => i.NombreArchivo)
                    .ToListAsync();

                foreach (string? nombre in nombres)
                {
                    if (!string.IsNullOrWhiteSpace(nombre))
                    {
                        existentes.Add(nombre);
                    }
                }
            }

            HashSet<string> vistos = new(StringComparer.OrdinalIgnoreCase);
            List<string> duplicadas = new();

            foreach (IFormFile imagen in imagenes)
            {
                string nombre = Path.GetFileName(imagen.FileName);

                if (existentes.Contains(nombre) || !vistos.Add(nombre))
                {
                    if (!duplicadas.Contains(nombre, StringComparer.OrdinalIgnoreCase))
                    {
                        duplicadas.Add(nombre);
                    }
                }
            }

            if (duplicadas.Count > 0)
            {
                errores.Add(
                    "Se detectaron imágenes duplicadas para este contenido: " +
                    string.Join(", ", duplicadas) +
                    ". Revise la información antes de continuar.");
            }

            return errores;
        }

        private async Task<string> GuardarImagenGaleriaAsync(IFormFile imagen)
        {
            string carpeta = Path.Combine(
                _entornoWeb.WebRootPath, "uploads", "galerias");

            Directory.CreateDirectory(carpeta);

            string nombreArchivo =
                $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName).ToLowerInvariant()}";

            using (var flujo = new FileStream(
                Path.Combine(carpeta, nombreArchivo), FileMode.Create))
            {
                await imagen.CopyToAsync(flujo);
            }

            return $"/uploads/galerias/{nombreArchivo}";
        }
        
        private void EliminarArchivoGaleria(string url)
        {
            if (!url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            string ruta = Path.Combine(
                _entornoWeb.WebRootPath,
                url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(ruta))
            {
                System.IO.File.Delete(ruta);
            }
        }

        private async Task<List<GaleriaAdminViewModel>> ObtenerGaleriasAsync(
    int? idGaleria = null)
        {
            var datos = await _context.Galerias
                .AsNoTracking()
                .Where(g => idGaleria == null || g.IdGaleria == idGaleria)
                .OrderByDescending(g => g.FechaCreacion)
                .Select(g => new
                {
                    g.IdGaleria,
                    g.Nombre,
                    g.Descripcion,
                    g.Estado,
                    g.FechaCreacion,
                    g.IdEvento,
                    g.IdProducto,
                    g.IdEmprendimiento,
                    Imagenes = g.GaleriaImagenes
                        .OrderByDescending(i => i.EsPortada)
                        .ThenBy(i => i.OrdenVisual)
                        .ThenBy(i => i.IdImagen)
                        .Select(i => new { i.IdImagen, i.UrlImagen, i.NombreArchivo })
                        .ToList()
                })
                .ToListAsync();

            return datos
                .Select(g => new GaleriaAdminViewModel
                {
                    Id = g.IdGaleria,
                    Nombre = g.Nombre,
                    Descripcion = g.Descripcion ?? string.Empty,
                    Estado = g.Estado,
                    Categoria =
                        g.IdEvento != null ? "Evento" :
                        g.IdProducto != null ? "Producto" :
                        g.IdEmprendimiento != null ? "Emprendimiento" : "General",
                    Contenido =
                        g.IdEvento != null ? $"Evento:{g.IdEvento}" :
                        g.IdProducto != null ? $"Producto:{g.IdProducto}" :
                        g.IdEmprendimiento != null ? $"Emprendimiento:{g.IdEmprendimiento}" :
                        string.Empty,
                    Portada = g.Imagenes.FirstOrDefault()?.UrlImagen ?? "/images/logo.jpg",
                    FechaCreacion = g.FechaCreacion.ToString("dd/MM/yyyy"),
                    Imagenes = g.Imagenes
                        .Select(i => new ImagenGaleriaViewModel
                        {
                            Id = i.IdImagen,
                            Url = i.UrlImagen,
                            NombreArchivo = i.NombreArchivo ?? string.Empty
                        })
                        .ToList()
                })
                .ToList();
        }
        

        [HttpGet]
        public async Task<IActionResult> Noticias(string estado = "Todas")
        {
            IQueryable<Noticia> consulta = _context.Noticias.AsNoTracking();

            if (estado != "Todas")
            {
                consulta = consulta.Where(n => n.Estado == estado);
            }

            var datos = await consulta
                .OrderByDescending(n => n.FechaPublicacion ?? n.FechaCreacion)
                .Select(n => new
                {
                    n.IdNoticia,
                    n.Titulo,
                    n.Contenido,
                    n.Tipo,
                    n.Estado,
                    n.ImagenUrl,
                    n.FechaPublicacion,
                    Autor = n.IdAutorNavigation.Nombre + " " + n.IdAutorNavigation.ApellidoPaterno
                })
                .ToListAsync();

            return View(new ListadoNoticiasAdminViewModel
            {
                FiltroEstado = estado,
                Noticias = datos
                    .Select(n => new NoticiaAdminViewModel
                    {
                        Id = n.IdNoticia,
                        Titulo = n.Titulo,
                        Contenido = n.Contenido,
                        Categoria = n.Tipo,
                        Estado = n.Estado,
                        Imagen = n.ImagenUrl ?? "/images/logo.jpg",
                        Autor = n.Autor,
                        FechaPublicacion = n.FechaPublicacion?.ToString("dd/MM/yyyy") ?? string.Empty
                    })
                    .ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearNoticia()
        {
            ViewData["Titulo"] = "Crear publicación";

            return View("FormularioNoticia", new NoticiaAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearNoticia(
            NoticiaAdminViewModel modelo,
            string accion = "publicar")
        {
            ViewData["Titulo"] = "Crear publicación";

            LimpiarCamposNoEnviadosNoticia();

            await ValidarNoticiaAsync(modelo);

            if (!ModelState.IsValid)
            {
                return View("FormularioNoticia", modelo);
            }

            int? idAutor = HttpContext.Session.GetInt32("IdUsuario");

            if (idAutor is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            bool publicar = accion != "guardar";

            Noticia noticia = new()
            {
                IdAutor = idAutor.Value,
                Titulo = modelo.Titulo,
                Contenido = modelo.Contenido,
                Tipo = modelo.Categoria,
                Estado = publicar ? "Publicada" : "Borrador",
                FechaCreacion = DateTime.Now,
                FechaPublicacion = publicar ? DateTime.Now : null
            };

            if (modelo.ImagenArchivo is { Length: > 0 })
            {
                noticia.ImagenUrl = await GuardarImagenNoticiaAsync(modelo.ImagenArchivo);
            }

            _context.Noticias.Add(noticia);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = publicar
                ? $"La publicación \"{noticia.Titulo}\" fue publicada correctamente."
                : $"La publicación \"{noticia.Titulo}\" se guardó como borrador.";

            return RedirectToAction(nameof(Noticias));
        }

        [HttpGet]
        public async Task<IActionResult> EditarNoticia(int id)
        {
            ViewData["Titulo"] = "Editar publicación";

            Noticia? noticia = await _context.Noticias
                .AsNoTracking()
                .Include(n => n.IdAutorNavigation)
                .FirstOrDefaultAsync(n => n.IdNoticia == id);

            if (noticia is null)
            {
                TempData["ErrorAdmin"] =
                    "La publicación no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Noticias));
            }

            return View("FormularioNoticia", new NoticiaAdminViewModel
            {
                Id = noticia.IdNoticia,
                Titulo = noticia.Titulo,
                Contenido = noticia.Contenido,
                Categoria = noticia.Tipo,
                Estado = noticia.Estado,
                Imagen = noticia.ImagenUrl ?? "/images/logo.jpg",
                Autor = noticia.IdAutorNavigation.Nombre + " " + noticia.IdAutorNavigation.ApellidoPaterno,
                FechaPublicacion = noticia.FechaPublicacion?.ToString("dd/MM/yyyy") ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarNoticia(NoticiaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar publicación";

            Noticia? noticia = await _context.Noticias
                .FirstOrDefaultAsync(n => n.IdNoticia == modelo.Id);

            if (noticia is null)
            {
                TempData["ErrorAdmin"] =
                    "La publicación no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Noticias));
            }

            LimpiarCamposNoEnviadosNoticia();

            await ValidarNoticiaAsync(modelo);

            if (!ModelState.IsValid)
            {
                modelo.Imagen = noticia.ImagenUrl ?? "/images/logo.jpg";

                return View("FormularioNoticia", modelo);
            }

            noticia.Titulo = modelo.Titulo;
            noticia.Contenido = modelo.Contenido;
            noticia.Tipo = modelo.Categoria;
            noticia.Estado = modelo.Estado;

            if (noticia.Estado == "Publicada" && noticia.FechaPublicacion is null)
            {
                noticia.FechaPublicacion = DateTime.Now;
            }

            if (modelo.ImagenArchivo is { Length: > 0 })
            {
                string? imagenAnterior = noticia.ImagenUrl;

                noticia.ImagenUrl = await GuardarImagenNoticiaAsync(modelo.ImagenArchivo);

                EliminarImagenNoticia(imagenAnterior);
            }

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"La publicación \"{noticia.Titulo}\" fue actualizada correctamente.";

            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublicarNoticia(int id)
        {
            Noticia? noticia = await _context.Noticias
                .FirstOrDefaultAsync(n => n.IdNoticia == id);

            if (noticia is null)
            {
                TempData["ErrorAdmin"] = "La publicación no está disponible.";

                return RedirectToAction(nameof(Noticias));
            }

            noticia.Estado = "Publicada";
            noticia.FechaPublicacion ??= DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"La publicación \"{noticia.Titulo}\" fue publicada correctamente.";

            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesactivarNoticia(int id)
        {
            Noticia? noticia = await _context.Noticias
                .FirstOrDefaultAsync(n => n.IdNoticia == id);

            if (noticia is null)
            {
                TempData["ErrorAdmin"] = "La publicación no está disponible.";

                return RedirectToAction(nameof(Noticias));
            }

            noticia.Estado = "Inactiva";

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"La publicación \"{noticia.Titulo}\" fue desactivada.";

            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarNoticia(int id)
        {
            Noticia? noticia = await _context.Noticias
                .FirstOrDefaultAsync(n => n.IdNoticia == id);

            if (noticia is null)
            {
                TempData["ErrorAdmin"] =
                    "La publicación no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Noticias));
            }

            string titulo = noticia.Titulo;
            string? imagen = noticia.ImagenUrl;

            _context.Noticias.Remove(noticia);

            await _context.SaveChangesAsync();

            EliminarImagenNoticia(imagen);

            TempData["MensajeAdmin"] =
                $"La publicación \"{titulo}\" fue eliminada correctamente.";

            return RedirectToAction(nameof(Noticias));
        }

        private void LimpiarCamposNoEnviadosNoticia()
        {
            ModelState.Remove(nameof(NoticiaAdminViewModel.Autor));
            ModelState.Remove(nameof(NoticiaAdminViewModel.Imagen));
            ModelState.Remove(nameof(NoticiaAdminViewModel.FechaPublicacion));
            ModelState.Remove(nameof(NoticiaAdminViewModel.Estado));
        }

        private async Task ValidarNoticiaAsync(NoticiaAdminViewModel modelo)
        {
            modelo.Titulo = (modelo.Titulo ?? string.Empty).Trim();
            modelo.Contenido = (modelo.Contenido ?? string.Empty).Trim();

            List<string> faltantes = [];

            if (modelo.Titulo.Length == 0)
            {
                faltantes.Add("título");
            }

            if (modelo.Contenido.Length == 0)
            {
                faltantes.Add("contenido");
            }

            if (string.IsNullOrWhiteSpace(modelo.Categoria))
            {
                faltantes.Add("categoría");
            }

            if (faltantes.Count > 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Complete los campos obligatorios antes de guardar: {string.Join(", ", faltantes)}.");
            }

            if (!string.IsNullOrWhiteSpace(modelo.Categoria) &&
                modelo.Categoria != "Noticia" &&
                modelo.Categoria != "Anuncio")
            {
                ModelState.AddModelError(nameof(modelo.Categoria), "Seleccione una categoría válida.");
            }

            if (modelo.Estado != "Borrador" &&
                modelo.Estado != "Publicada" &&
                modelo.Estado != "Inactiva")
            {
                modelo.Estado = "Borrador";
            }

            if (modelo.ImagenArchivo is { Length: > 0 })
            {
                string[] extensiones = [".jpg", ".jpeg", ".png", ".webp"];
                string extension = Path.GetExtension(modelo.ImagenArchivo.FileName).ToLowerInvariant();

                if (!extensiones.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.ImagenArchivo),
                        "La imagen debe ser JPG, PNG o WEBP.");
                }
                else if (modelo.ImagenArchivo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(modelo.ImagenArchivo),
                        "La imagen no puede superar los 5 MB.");
                }
            }

            if (modelo.Titulo.Length > 0 && !string.IsNullOrWhiteSpace(modelo.Categoria))
            {
                string titulo = modelo.Titulo;
                string categoria = modelo.Categoria;
                int idActual = modelo.Id;

                bool duplicada = await _context.Noticias
                    .AnyAsync(n =>
                        n.IdNoticia != idActual &&
                        n.Titulo == titulo &&
                        n.Tipo == categoria);

                if (duplicada)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Titulo),
                        "Ya existe una publicación con este título y categoría. Revise la información.");
                }
            }
        }

        private async Task<string> GuardarImagenNoticiaAsync(IFormFile imagen)
        {
            string carpeta = Path.Combine(_entornoWeb.WebRootPath, "uploads", "noticias");

            Directory.CreateDirectory(carpeta);

            string nombreArchivo =
                $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName).ToLowerInvariant()}";

            using (FileStream flujo = new(Path.Combine(carpeta, nombreArchivo), FileMode.Create))
            {
                await imagen.CopyToAsync(flujo);
            }

            return $"/uploads/noticias/{nombreArchivo}";
        }

        private void EliminarImagenNoticia(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/noticias/"))
            {
                return;
            }

            string ruta = Path.Combine(
                _entornoWeb.WebRootPath,
                "uploads",
                "noticias",
                Path.GetFileName(url));

            if (System.IO.File.Exists(ruta))
            {
                System.IO.File.Delete(ruta);
            }
        }

        private static readonly string[] TiposReporte =
            ["Ventas", "Usuarios", "Emprendimientos", "Productos", "Eventos"];

        private static readonly System.Globalization.CultureInfo CulturaCr = new("es-CR");

        private static List<string> ValidarFiltroReporte(FiltroReporteViewModel filtro)
        {
            List<string> errores = [];

            if (string.IsNullOrWhiteSpace(filtro.TipoReporte) ||
                !TiposReporte.Contains(filtro.TipoReporte))
            {
                errores.Add("Seleccione el tipo de reporte que desea generar.");
            }

            if (filtro.FechaInicio.HasValue != filtro.FechaFin.HasValue)
            {
                errores.Add("Indique la fecha inicial y la fecha final del período.");
            }
            else if (filtro.FechaInicio > filtro.FechaFin)
            {
                errores.Add("La fecha inicial no puede ser posterior a la fecha final.");
            }

            return errores;
        }

        [HttpGet]
        public async Task<IActionResult> Reportes(FiltroReporteViewModel filtro, bool generar = false)
        {
            filtro ??= new FiltroReporteViewModel();

            if (!generar)
            {
                return View(new ReporteAdminViewModel
                {
                    Filtro = filtro,
                    Titulo = "Reportes y estadísticas"
                });
            }

            List<string> errores = ValidarFiltroReporte(filtro);

            if (errores.Count > 0)
            {
                return View(new ReporteAdminViewModel
                {
                    Filtro = filtro,
                    Titulo = "Reportes y estadísticas",
                    Errores = errores
                });
            }

            return View(await ConstruirReporteAsync(filtro, 300));
        }

        [HttpGet]
        public async Task<IActionResult> ExportarReporte(FiltroReporteViewModel filtro, string formato = "csv")
        {
            filtro ??= new FiltroReporteViewModel();

            bool solicitudAsincrona = Request.Headers["X-Requested-With"] == "fetch";
            string? error = ValidarFiltroReporte(filtro).FirstOrDefault();
            ReporteAdminViewModel? modelo = null;

            if (error is null)
            {
                modelo = await ConstruirReporteAsync(filtro, null);

                if (modelo.Filas.Count == 0)
                {
                    error = "No hay información para exportar con los criterios seleccionados.";
                }
            }

            if (error is not null || modelo is null)
            {
                if (solicitudAsincrona)
                {
                    return BadRequest(new { mensaje = error });
                }

                TempData["ErrorAdmin"] = error;

                return RedirectToAction(nameof(Reportes), new
                {
                    filtro.TipoReporte,
                    FechaInicio = filtro.FechaInicio?.ToString("yyyy-MM-dd"),
                    FechaFin = filtro.FechaFin?.ToString("yyyy-MM-dd"),
                    filtro.Categoria,
                    generar = true
                });
            }

            string nombreBase = $"reporte-{filtro.TipoReporte.ToLowerInvariant()}-{DateTime.Now:yyyyMMdd-HHmm}";

            if (formato == "xlsx")
            {
                using XLWorkbook libro = new();
                IXLWorksheet hoja = libro.Worksheets.Add(filtro.TipoReporte);

                for (int i = 0; i < modelo.Columnas.Count; i++)
                {
                    hoja.Cell(1, i + 1).Value = modelo.Columnas[i];
                }

                hoja.Row(1).Style.Font.Bold = true;

                for (int f = 0; f < modelo.Filas.Count; f++)
                {
                    FilaReporteViewModel fila = modelo.Filas[f];

                    hoja.Cell(f + 2, 1).Value = fila.Columna1;
                    hoja.Cell(f + 2, 2).Value = fila.Columna2;
                    hoja.Cell(f + 2, 3).Value = fila.Columna3;
                    hoja.Cell(f + 2, 4).Value = fila.Columna4;
                }

                hoja.Columns().AdjustToContents();

                using MemoryStream salida = new();
                libro.SaveAs(salida);

                return File(
                    salida.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{nombreBase}.xlsx");
            }

            System.Text.StringBuilder constructor = new();
            constructor.AppendLine(string.Join(",", modelo.Columnas.Select(EscaparCsv)));

            foreach (FilaReporteViewModel fila in modelo.Filas)
            {
                constructor.AppendLine(string.Join(",", new[]
                {
                    fila.Columna1, fila.Columna2, fila.Columna3, fila.Columna4
                }.Select(EscaparCsv)));
            }

            System.Text.UTF8Encoding codificacion = new(true);
            byte[] contenido = codificacion.GetPreamble()
                .Concat(codificacion.GetBytes(constructor.ToString()))
                .ToArray();

            return File(contenido, "text/csv", $"{nombreBase}.csv");
        }

        private static string EscaparCsv(string valor)
        {
            if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n'))
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }

            return valor;
        }

        private static List<PuntoGraficoViewModel> AgruparPorMes(
            List<DateTime> fechas,
            DateTime? inicio,
            DateTime? fin)
        {
            DateTime hasta = fin?.Date
                ?? (fechas.Count > 0
                    ? (fechas.Max().Date > DateTime.Today ? fechas.Max().Date : DateTime.Today)
                    : DateTime.Today);

            DateTime desde = inicio?.Date
                ?? (fechas.Count > 0 ? fechas.Min().Date : hasta.AddMonths(-5));

            DateTime primerMes = new(desde.Year, desde.Month, 1);
            DateTime ultimoMes = new(hasta.Year, hasta.Month, 1);

            while ((ultimoMes.Year - primerMes.Year) * 12 + ultimoMes.Month - primerMes.Month >= 12)
            {
                primerMes = primerMes.AddMonths(1);
            }

            List<PuntoGraficoViewModel> puntos = [];

            for (DateTime mes = primerMes; mes <= ultimoMes; mes = mes.AddMonths(1))
            {
                puntos.Add(new PuntoGraficoViewModel
                {
                    Etiqueta = mes.ToString("MMM yy", CulturaCr),
                    Valor = fechas.Count(f => f.Year == mes.Year && f.Month == mes.Month)
                });
            }

            return puntos;
        }

        private static List<PuntoGraficoViewModel> AgruparPorEtiqueta(IEnumerable<string> valores)
        {
            return valores
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new PuntoGraficoViewModel { Etiqueta = g.Key, Valor = g.Count() })
                .ToList();
        }

        private async Task<ReporteAdminViewModel> ConstruirReporteAsync(
            FiltroReporteViewModel filtro,
            int? limite)
        {
            DateTime? inicio = filtro.FechaInicio?.Date;
            DateTime? finExclusivo = filtro.FechaFin?.Date.AddDays(1);
            string? categoria = string.IsNullOrWhiteSpace(filtro.Categoria)
                ? null
                : filtro.Categoria.Trim();

            ReporteAdminViewModel modelo = new() { Filtro = filtro, Generado = true };
            List<DateTime> fechas = [];
            List<string> distribucion = [];
            List<FilaReporteViewModel> filas = [];

            switch (filtro.TipoReporte)
            {
                case "Usuarios":
                {
                    IQueryable<Usuario> consulta = _context.Usuarios.AsNoTracking();

                    if (inicio.HasValue) consulta = consulta.Where(u => u.FechaRegistro >= inicio.Value);
                    if (finExclusivo.HasValue) consulta = consulta.Where(u => u.FechaRegistro < finExclusivo.Value);
                    if (categoria is not null)
                    {
                        consulta = consulta.Where(u => u.UsuarioRoles.Any(r => r.IdRolNavigation.Nombre.Contains(categoria)));
                    }

                    var datos = await consulta
                        .OrderByDescending(u => u.FechaRegistro)
                        .Select(u => new
                        {
                            Nombre = u.Nombre + " " + u.ApellidoPaterno,
                            Rol = u.UsuarioRoles.Select(r => r.IdRolNavigation.Nombre).FirstOrDefault(),
                            u.FechaRegistro,
                            u.Estado
                        })
                        .ToListAsync();

                    modelo.Titulo = "Reporte de usuarios";
                    modelo.Columnas = ["Usuario", "Rol", "Registro", "Estado"];
                    fechas = datos.Select(d => d.FechaRegistro).ToList();
                    distribucion = datos.Select(d => d.Rol ?? "Sin rol").ToList();
                    filas = datos.Select(d => new FilaReporteViewModel
                    {
                        Columna1 = d.Nombre,
                        Columna2 = d.Rol ?? "Sin rol",
                        Columna3 = d.FechaRegistro.ToString("dd/MM/yyyy"),
                        Columna4 = d.Estado
                    }).ToList();
                    break;
                }

                case "Emprendimientos":
                {
                    IQueryable<Emprendimiento> consulta = _context.Emprendimientos.AsNoTracking();

                    if (inicio.HasValue)
                    {
                        consulta = consulta.Where(e => e.EmprendimientoRevisione != null && e.EmprendimientoRevisione.FechaSolicitud >= inicio.Value);
                    }

                    if (finExclusivo.HasValue)
                    {
                        consulta = consulta.Where(e => e.EmprendimientoRevisione != null && e.EmprendimientoRevisione.FechaSolicitud < finExclusivo.Value);
                    }

                    if (categoria is not null)
                    {
                        consulta = consulta.Where(e => e.IdCategoriaNavigation != null && e.IdCategoriaNavigation.Nombre.Contains(categoria));
                    }

                    var datos = await consulta
                        .OrderBy(e => e.NombreComercial)
                        .Select(e => new
                        {
                            e.NombreComercial,
                            Categoria = e.IdCategoriaNavigation != null ? e.IdCategoriaNavigation.Nombre : null,
                            Solicitud = e.EmprendimientoRevisione != null
                                ? (DateTime?)e.EmprendimientoRevisione.FechaSolicitud
                                : null,
                            e.EstadoAprobacion
                        })
                        .ToListAsync();

                    modelo.Titulo = "Reporte de emprendimientos";
                    modelo.Columnas = ["Emprendimiento", "Categoría", "Solicitud", "Estado"];
                    fechas = datos.Where(d => d.Solicitud.HasValue).Select(d => d.Solicitud!.Value).ToList();
                    distribucion = datos.Select(d => d.EstadoAprobacion).ToList();
                    filas = datos.Select(d => new FilaReporteViewModel
                    {
                        Columna1 = d.NombreComercial,
                        Columna2 = d.Categoria ?? "Sin categoría",
                        Columna3 = d.Solicitud?.ToString("dd/MM/yyyy") ?? "—",
                        Columna4 = d.EstadoAprobacion
                    }).ToList();
                    break;
                }

                case "Productos":
                {
                    IQueryable<Producto> consulta = _context.Productos.AsNoTracking();

                    if (inicio.HasValue) consulta = consulta.Where(p => p.FechaRegistro >= inicio.Value);
                    if (finExclusivo.HasValue) consulta = consulta.Where(p => p.FechaRegistro < finExclusivo.Value);
                    if (categoria is not null)
                    {
                        consulta = consulta.Where(p => p.IdCategoriaNavigation != null && p.IdCategoriaNavigation.Nombre.Contains(categoria));
                    }

                    var datos = await consulta
                        .OrderByDescending(p => p.FechaRegistro)
                        .Select(p => new
                        {
                            p.Nombre,
                            Emprendimiento = p.IdEmprendimientoNavigation.NombreComercial,
                            Categoria = p.IdCategoriaNavigation != null ? p.IdCategoriaNavigation.Nombre : null,
                            p.Estado,
                            p.FechaRegistro
                        })
                        .ToListAsync();

                    modelo.Titulo = "Reporte de productos";
                    modelo.Columnas = ["Producto", "Emprendimiento", "Categoría", "Estado"];
                    fechas = datos.Select(d => d.FechaRegistro).ToList();
                    distribucion = datos.Select(d => d.Categoria ?? "Sin categoría").ToList();
                    filas = datos.Select(d => new FilaReporteViewModel
                    {
                        Columna1 = d.Nombre,
                        Columna2 = d.Emprendimiento,
                        Columna3 = d.Categoria ?? "Sin categoría",
                        Columna4 = d.Estado
                    }).ToList();
                    break;
                }

                case "Eventos":
                {
                    IQueryable<Evento> consulta = _context.Eventos
                        .AsNoTracking()
                        .Where(e => e.Estado != "Eliminado");

                    if (inicio.HasValue) consulta = consulta.Where(e => e.FechaInicio >= inicio.Value);
                    if (finExclusivo.HasValue) consulta = consulta.Where(e => e.FechaInicio < finExclusivo.Value);
                    if (categoria is not null)
                    {
                        consulta = consulta.Where(e => e.IdCategoriaNavigation != null && e.IdCategoriaNavigation.Nombre.Contains(categoria));
                    }

                    var datos = await consulta
                        .OrderByDescending(e => e.FechaInicio)
                        .Select(e => new
                        {
                            e.Nombre,
                            e.Ubicacion,
                            e.FechaInicio,
                            e.Estado,
                            Inscritos = e.InscripcionesEventos.Count(i => i.Estado != "Cancelada")
                        })
                        .ToListAsync();

                    modelo.Titulo = "Reporte de eventos";
                    modelo.Columnas = ["Evento", "Ubicación", "Fecha", "Inscritos"];
                    fechas = datos.Select(d => d.FechaInicio).ToList();
                    distribucion = datos.Select(d => d.Estado).ToList();
                    filas = datos.Select(d => new FilaReporteViewModel
                    {
                        Columna1 = d.Nombre,
                        Columna2 = d.Ubicacion,
                        Columna3 = d.FechaInicio.ToString("dd/MM/yyyy"),
                        Columna4 = d.Inscritos.ToString()
                    }).ToList();
                    break;
                }

                default:
                {
                    IQueryable<Venta> consulta = _context.Ventas.AsNoTracking();

                    if (inicio.HasValue) consulta = consulta.Where(v => v.FechaVenta >= inicio.Value);
                    if (finExclusivo.HasValue) consulta = consulta.Where(v => v.FechaVenta < finExclusivo.Value);
                    if (categoria is not null)
                    {
                        consulta = consulta.Where(v => v.VentaDetalles.Any(d =>
                            d.IdProductoNavigation.IdCategoriaNavigation != null &&
                            d.IdProductoNavigation.IdCategoriaNavigation.Nombre.Contains(categoria)));
                    }

                    var datos = await consulta
                        .OrderByDescending(v => v.FechaVenta)
                        .Select(v => new
                        {
                            v.NumeroOrden,
                            Cliente = v.IdUsuarioNavigation.Nombre + " " + v.IdUsuarioNavigation.ApellidoPaterno,
                            v.FechaVenta,
                            v.Total,
                            v.Estado
                        })
                        .ToListAsync();

                    modelo.Titulo = "Reporte de ventas";
                    modelo.Columnas = ["Orden", "Cliente", "Fecha", "Total"];
                    fechas = datos.Select(d => d.FechaVenta).ToList();
                    distribucion = datos.Select(d => d.Estado).ToList();
                    filas = datos.Select(d => new FilaReporteViewModel
                    {
                        Columna1 = d.NumeroOrden,
                        Columna2 = d.Cliente,
                        Columna3 = d.FechaVenta.ToString("dd/MM/yyyy"),
                        Columna4 = "₡" + d.Total.ToString("N0", CulturaCr)
                    }).ToList();
                    break;
                }
            }

            modelo.GraficoBarras = fechas.Count > 0
                ? AgruparPorMes(fechas, filtro.FechaInicio, filtro.FechaFin)
                : [];
            modelo.GraficoDona = AgruparPorEtiqueta(distribucion);
            modelo.Filas = limite.HasValue ? filas.Take(limite.Value).ToList() : filas;

            return modelo;
        }

        [HttpGet]
        public async Task<IActionResult> EstadisticasHechoEnCr(FiltroEstadisticasViewModel filtro)
        {
            filtro ??= new FiltroEstadisticasViewModel();

            EstadisticasHechoEnCrViewModel modelo = new()
            {
                Filtro = filtro,
                FechaConsulta = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt")
            };

            if (filtro.FechaInicio.HasValue != filtro.FechaFin.HasValue)
            {
                modelo.Errores.Add("Indique la fecha inicial y la fecha final del período.");
            }
            else if (filtro.FechaInicio > filtro.FechaFin)
            {
                modelo.Errores.Add("La fecha inicial no puede ser posterior a la fecha final.");
            }

            if (modelo.Errores.Count > 0)
            {
                return View(modelo);
            }

            DateTime desde = filtro.FechaInicio?.Date ?? new DateTime(1900, 1, 1);
            DateTime hasta = filtro.FechaFin?.Date.AddDays(1) ?? new DateTime(2999, 12, 31);

            var emprendimientos = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.ParticipaHechoEnCr && e.EstadoAprobacion == "Aprobado" && e.Activo)
                .Select(e => new
                {
                    e.IdEmprendimiento,
                    e.NombreComercial,
                    Productos = e.Productos.Count(p => p.PublicadoHechoEnCr),
                    Participaciones = e.ParticipacionesEventos.Count(p =>
                        p.Estado == "Aprobada" &&
                        p.FechaSolicitud >= desde &&
                        p.FechaSolicitud < hasta)
                })
                .ToListAsync();

            var lineas = await _context.VentaDetalles
                .AsNoTracking()
                .Where(d =>
                    d.IdProductoNavigation.PublicadoHechoEnCr &&
                    d.IdProductoNavigation.IdEmprendimientoNavigation.ParticipaHechoEnCr &&
                    d.IdVentaNavigation.Estado != "Cancelada" &&
                    d.IdVentaNavigation.FechaVenta >= desde &&
                    d.IdVentaNavigation.FechaVenta < hasta)
                .Select(d => new
                {
                    d.IdProductoNavigation.IdEmprendimiento,
                    d.Cantidad,
                    Importe = d.Cantidad * d.PrecioUnitario,
                    d.IdVentaNavigation.FechaVenta
                })
                .ToListAsync();

            modelo.EmprendimientosParticipantes = emprendimientos.Count;
            modelo.ProductosPublicados = emprendimientos.Sum(e => e.Productos);
            modelo.ParticipacionesEventos = emprendimientos.Sum(e => e.Participaciones);
            modelo.UnidadesVendidas = lineas.Sum(l => l.Cantidad);
            modelo.IngresosTotales = lineas.Sum(l => l.Importe);
            modelo.HayDatos = lineas.Count > 0 || modelo.ParticipacionesEventos > 0;

            if (!modelo.HayDatos)
            {
                return View(modelo);
            }

            modelo.Emprendimientos = emprendimientos
                .Select(e => new EmprendimientoEstadisticaViewModel
                {
                    Nombre = e.NombreComercial,
                    Productos = e.Productos,
                    UnidadesVendidas = lineas.Where(l => l.IdEmprendimiento == e.IdEmprendimiento).Sum(l => l.Cantidad),
                    Ingresos = lineas.Where(l => l.IdEmprendimiento == e.IdEmprendimiento).Sum(l => l.Importe),
                    Participaciones = e.Participaciones
                })
                .OrderByDescending(e => e.Ingresos)
                .ThenByDescending(e => e.Participaciones)
                .ToList();

            modelo.UnidadesPorEmprendimiento = modelo.Emprendimientos
                .Where(e => e.UnidadesVendidas > 0)
                .Take(5)
                .Select(e => new PuntoGraficoViewModel { Etiqueta = e.Nombre, Valor = e.UnidadesVendidas })
                .ToList();

            modelo.VentasPorMes = lineas.Count > 0
                ? AgruparPorMes(lineas.Select(l => l.FechaVenta).ToList(), filtro.FechaInicio, filtro.FechaFin)
                : [];

            return View(modelo);
        }


        [HttpGet]
        public async Task<IActionResult> Comentarios(
            string? busqueda,
            string estado = "Todos",
            string origen = "Todos")
        {
            // HU-67: Consultar comentarios reales de la base de datos.
            var consulta = _context.ComentariosResenas
                .AsNoTracking()
                .AsQueryable();

            // Filtrar por contenido o nombre del autor.
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string termino = busqueda.Trim();

                consulta = consulta.Where(c =>
                    c.Contenido.Contains(termino) ||
                    c.IdUsuarioNavigation.Nombre.Contains(termino) ||
                    c.IdUsuarioNavigation.ApellidoPaterno.Contains(termino));
            }

            // Filtrar por estado.
            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                consulta = consulta.Where(c => c.Estado == estado);
            }

            // Filtrar por origen.
            if (!string.IsNullOrWhiteSpace(origen) && origen != "Todos")
            {
                consulta = origen switch
                {
                    "Producto" => consulta.Where(c => c.IdProducto != null),
                    "Emprendimiento" => consulta.Where(c => c.IdEmprendimiento != null),
                    "Evento" => consulta.Where(c => c.IdEvento != null),
                    _ => consulta
                };
            }

            var comentarios = await consulta
                .OrderByDescending(c => c.FechaPublicacion)
                .Select(c => new
                {
                    c.IdComentario,
                    c.Contenido,
                    c.Estado,
                    c.CantidadReportes,
                    c.FechaPublicacion,
                    Autor = c.IdUsuarioNavigation.Nombre + " " +
                            c.IdUsuarioNavigation.ApellidoPaterno,
                    Origen = c.IdProducto != null ? "Producto" :
                             c.IdEmprendimiento != null ? "Emprendimiento" :
                             "Evento",
                    ElementoRelacionado = c.IdProductoNavigation != null
                        ? c.IdProductoNavigation.Nombre
                        : c.IdEmprendimientoNavigation != null
                            ? c.IdEmprendimientoNavigation.NombreComercial
                            : c.IdEventoNavigation != null
                                ? c.IdEventoNavigation.Nombre
                                : "Sin información"
                })
                .ToListAsync();

            var modelo = new ListadoComentariosAdminViewModel
            {
                Busqueda = busqueda,
                FiltroEstado = estado,
                FiltroOrigen = origen,
                Comentarios = comentarios.Select(c => new ComentarioAdminViewModel
                {
                    Id = c.IdComentario,
                    Autor = c.Autor,
                    Contenido = c.Contenido,
                    Origen = c.Origen,
                    ElementoRelacionado = c.ElementoRelacionado,
                    Fecha = c.FechaPublicacion.ToString("dd/MM/yyyy"),
                    Estado = c.Estado,
                    Reportes = c.CantidadReportes
                }).ToList()
            };

            return View(modelo);
        }



        [HttpGet]
        public async Task<IActionResult> VerComentario(long id)
        {
            // HU-67: Consultar el comentario real seleccionado.
            var comentario = await _context.ComentariosResenas
                .AsNoTracking()
                .Where(c => c.IdComentario == id)
                .Select(c => new
                {
                    c.IdComentario,
                    c.Contenido,
                    c.Estado,
                    c.CantidadReportes,
                    c.FechaPublicacion,

                    Autor = c.IdUsuarioNavigation.Nombre + " " +
                            c.IdUsuarioNavigation.ApellidoPaterno,

                    Origen = c.IdProducto != null ? "Producto" :
                             c.IdEmprendimiento != null ? "Emprendimiento" :
                             "Evento",

                    ElementoRelacionado = c.IdProductoNavigation != null
                        ? c.IdProductoNavigation.Nombre
                        : c.IdEmprendimientoNavigation != null
                            ? c.IdEmprendimientoNavigation.NombreComercial
                            : c.IdEventoNavigation != null
                                ? c.IdEventoNavigation.Nombre
                                : "Sin información"
                })
                .FirstOrDefaultAsync();

            if (comentario == null)
            {
                TempData["MensajeAdmin"] = "El comentario seleccionado no existe.";
                return RedirectToAction(nameof(Comentarios));
            }

            var modelo = new ComentarioAdminViewModel
            {
                Id = comentario.IdComentario,
                Autor = comentario.Autor,
                Contenido = comentario.Contenido,
                Origen = comentario.Origen,
                ElementoRelacionado = comentario.ElementoRelacionado,
                Fecha = comentario.FechaPublicacion.ToString("dd/MM/yyyy"),
                Estado = comentario.Estado,
                Reportes = comentario.CantidadReportes
            };

            return View(modelo);
        }


        // HU-67: Eliminar comentarios inapropiados y registrar la acción.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarComentario(long id)
        {
            int? idAdministrador = HttpContext.Session.GetInt32("IdUsuario");

            if (!idAdministrador.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var comentario = await _context.ComentariosResenas
                .FirstOrDefaultAsync(c => c.IdComentario == id);

            if (comentario == null)
            {
                TempData["MensajeAdmin"] = "El comentario seleccionado no existe.";
                return RedirectToAction(nameof(Comentarios));
            }

            if (comentario.Estado == "Eliminado")
            {
                TempData["MensajeAdmin"] = "Este comentario ya fue eliminado.";
                return RedirectToAction(nameof(Comentarios));
            }

            comentario.Estado = "Eliminado";

            var auditoria = new Auditorium
            {
                IdUsuario = idAdministrador.Value,
                Modulo = "Comentarios",
                Accion = "Eliminar",
                Entidad = "ComentariosResenas",
                IdRegistro = comentario.IdComentario.ToString(),
                Descripcion = $"El administrador eliminó el comentario con ID {comentario.IdComentario} por contenido inapropiado.",
                DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Nivel = "Informativo",
                FechaHora = DateTime.Now
            };

            _context.Auditoria.Add(auditoria);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                "El comentario fue eliminado correctamente y la acción quedó registrada.";

            return RedirectToAction(nameof(Comentarios));
        }


        [HttpGet]
        public async Task<IActionResult> Categorias(string tipo = "Categoria")
        {
            List<CategoriaAdminViewModel> categorias = new();

            if (tipo == "Etiqueta")
            {
                categorias = await _context.Etiquetas
                    .AsNoTracking()
                    .OrderBy(e => e.Nombre)
                    .Select(e => new CategoriaAdminViewModel
                    {
                        Id = e.IdEtiqueta,
                        Nombre = e.Nombre,
                        Tipo = "Etiqueta",
                        Modulo = "",
                        Descripcion = "",
                        ElementosAsociados =
                            e.IdEmprendimientos.Count() +
                            e.IdProductos.Count() +
                            e.IdEventos.Count() +
                            e.IdTallers.Count()
                    })
                    .ToListAsync();
            }
            else
            {
                tipo = "Categoria";

                categorias = await _context.Categorias
                    .AsNoTracking()
                    .OrderBy(c => c.Nombre)
                    .Select(c => new CategoriaAdminViewModel
                    {
                        Id = c.IdCategoria,
                        Nombre = c.Nombre,
                        Descripcion = c.Descripcion ?? "",
                        Tipo = "Categoria",
                        Modulo = c.Modulo,
                        ElementosAsociados =
                            c.Emprendimientos.Count() +
                            c.Productos.Count() +
                            c.Eventos.Count() +
                            c.Talleres.Count() +
                            c.Noticia.Count() +
                            c.Galeria.Count()
                    })
                    .ToListAsync();
            }

            return View(new ListadoCategoriasAdminViewModel
            {
                FiltroTipo = tipo,
                Categorias = categorias
            });
        }

        [HttpGet]
        public IActionResult CrearCategoria(string tipo = "Categoria")
        {
            ViewData["Titulo"] = tipo == "Etiqueta" ? "Crear etiqueta" : "Crear categoría";
            return View("FormularioCategoria", new CategoriaAdminViewModel { Tipo = tipo });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCategoria(CategoriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = modelo.Tipo == "Etiqueta"
                ? "Crear etiqueta"
                : "Crear categoría";

            // Validar el tipo seleccionado.
            if (modelo.Tipo != "Categoria" && modelo.Tipo != "Etiqueta")
            {
                ModelState.AddModelError(nameof(modelo.Tipo),
                    "Seleccione un tipo válido.");
            }

            // Validar que el nombre no esté vacío.
            string nombre = modelo.Nombre?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError(nameof(modelo.Nombre),
                    "El nombre es obligatorio.");
            }

            // Validar el módulo únicamente para categorías.
            if (modelo.Tipo == "Categoria")
            {
                string[] modulosValidos =
                {
            "Emprendimientos",
            "Productos",
            "Eventos",
            "Talleres",
            "Noticias"
        };

                if (!modulosValidos.Contains(modelo.Modulo))
                {
                    ModelState.AddModelError(nameof(modelo.Modulo),
                        "Seleccione un módulo válido.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("FormularioCategoria", modelo);
            }

            // Registrar una categoría.
            if (modelo.Tipo == "Categoria")
            {
                bool existe = await _context.Categorias.AnyAsync(c =>
                    c.Nombre == nombre &&
                    c.Modulo == modelo.Modulo);

                if (existe)
                {
                    ModelState.AddModelError(nameof(modelo.Nombre),
                        "Ya existe una categoría con ese nombre en el módulo seleccionado.");

                    return View("FormularioCategoria", modelo);
                }

                Categoria categoria = new()
                {
                    Nombre = nombre,
                    Descripcion = modelo.Descripcion?.Trim(),
                    Modulo = modelo.Modulo,
                    Activa = true
                };

                _context.Categorias.Add(categoria);
            }
            else
            {
                // Registrar una etiqueta.
                bool existe = await _context.Etiquetas.AnyAsync(e =>
                    e.Nombre == nombre);

                if (existe)
                {
                    ModelState.AddModelError(nameof(modelo.Nombre),
                        "Ya existe una etiqueta con ese nombre.");

                    return View("FormularioCategoria", modelo);
                }

                Etiqueta etiqueta = new()
                {
                    Nombre = nombre,
                    Activa = true
                };

                _context.Etiquetas.Add(etiqueta);
            }

            // Guardar realmente en SQL Server.
            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"\"{nombre}\" fue creada correctamente.";

            return RedirectToAction(nameof(Categorias),
                new { tipo = modelo.Tipo });
        }

        [HttpGet]
        public async Task<IActionResult> EditarCategoria(int id, string tipo = "Categoria")
        {
            CategoriaAdminViewModel? modelo = null;

            if (tipo == "Etiqueta")
            {
                var etiqueta = await _context.Etiquetas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdEtiqueta == id);

                if (etiqueta == null)
                    return NotFound();

                modelo = new CategoriaAdminViewModel
                {
                    Id = etiqueta.IdEtiqueta,
                    Nombre = etiqueta.Nombre,
                    Tipo = "Etiqueta",
                    Descripcion = null
                };
            }
            else if (tipo == "Categoria")
            {
                var categoria = await _context.Categorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdCategoria == id);

                if (categoria == null)
                    return NotFound();

                modelo = new CategoriaAdminViewModel
                {
                    Id = categoria.IdCategoria,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    Modulo = categoria.Modulo,
                    Tipo = "Categoria"
                };
            }
            else
            {
                return BadRequest();
            }

            ViewData["Titulo"] = tipo == "Etiqueta"
                ? "Editar etiqueta"
                : "Editar categoría";

            return View("FormularioCategoria", modelo);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCategoria(CategoriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = modelo.Tipo == "Etiqueta"
                ? "Editar etiqueta"
                : "Editar categoría";

            string nombre = modelo.Nombre?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError(nameof(modelo.Nombre),
                    "El nombre es obligatorio.");
            }

            if (modelo.Tipo != "Categoria" && modelo.Tipo != "Etiqueta")
            {
                ModelState.AddModelError(nameof(modelo.Tipo),
                    "Seleccione un tipo válido.");
            }

            if (modelo.Tipo == "Categoria")
            {
                string[] modulosValidos =
                {
            "Emprendimientos",
            "Productos",
            "Eventos",
            "Talleres",
            "Noticias"
        };

                if (!modulosValidos.Contains(modelo.Modulo))
                {
                    ModelState.AddModelError(nameof(modelo.Modulo),
                        "Seleccione un módulo válido.");
                }
            }

            if (!ModelState.IsValid)
                return View("FormularioCategoria", modelo);

            if (modelo.Tipo == "Categoria")
            {
                var categoria = await _context.Categorias
                    .FirstOrDefaultAsync(c => c.IdCategoria == modelo.Id);

                if (categoria == null)
                    return NotFound();

                bool existe = await _context.Categorias.AnyAsync(c =>
                    c.IdCategoria != modelo.Id &&
                    c.Nombre == nombre &&
                    c.Modulo == modelo.Modulo);

                if (existe)
                {
                    ModelState.AddModelError(nameof(modelo.Nombre),
                        "Ya existe una categoría con ese nombre en el módulo seleccionado.");

                    return View("FormularioCategoria", modelo);
                }

                categoria.Nombre = nombre;
                categoria.Descripcion = modelo.Descripcion?.Trim();
                categoria.Modulo = modelo.Modulo;
            }
            else
            {
                var etiqueta = await _context.Etiquetas
                    .FirstOrDefaultAsync(e => e.IdEtiqueta == modelo.Id);

                if (etiqueta == null)
                    return NotFound();

                bool existe = await _context.Etiquetas.AnyAsync(e =>
                    e.IdEtiqueta != modelo.Id &&
                    e.Nombre == nombre);

                if (existe)
                {
                    ModelState.AddModelError(nameof(modelo.Nombre),
                        "Ya existe una etiqueta con ese nombre.");

                    return View("FormularioCategoria", modelo);
                }

                etiqueta.Nombre = nombre;
            }

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"\"{nombre}\" fue actualizada correctamente.";

            return RedirectToAction(nameof(Categorias),
                new { tipo = modelo.Tipo });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCategoria(
            int id, string tipo = "Categoria")
        {
            if (tipo != "Categoria" && tipo != "Etiqueta")
            {
                return BadRequest();
            }

            if (tipo == "Categoria")
            {
                var categoria = await _context.Categorias
                    .FirstOrDefaultAsync(c => c.IdCategoria == id);

                if (categoria == null)
                {
                    return NotFound();
                }

                bool tieneAsociaciones =
                    await _context.Emprendimientos.AnyAsync(e => e.IdCategoria == id) ||
                    await _context.Productos.AnyAsync(p => p.IdCategoria == id) ||
                    await _context.Eventos.AnyAsync(e => e.IdCategoria == id) ||
                    await _context.Talleres.AnyAsync(t => t.IdCategoria == id) ||
                    await _context.Noticias.AnyAsync(n => n.IdCategoria == id) ||
                    await _context.Galerias.AnyAsync(g => g.IdCategoria == id);

                if (tieneAsociaciones)
                {
                    TempData["MensajeError"] =
                        "No se puede eliminar la categoría porque tiene elementos asociados.";

                    return RedirectToAction(nameof(Categorias), new { tipo });
                }

                _context.Categorias.Remove(categoria);
            }
            else
            {
                var etiqueta = await _context.Etiquetas
                    .FirstOrDefaultAsync(e => e.IdEtiqueta == id);

                if (etiqueta == null)
                {
                    return NotFound();
                }

                bool tieneAsociaciones = await _context.Etiquetas
                    .AnyAsync(e => e.IdEtiqueta == id &&
                        (e.IdEmprendimientos.Any() ||
                         e.IdProductos.Any() ||
                         e.IdEventos.Any() ||
                         e.IdTallers.Any()));

                if (tieneAsociaciones)
                {
                    TempData["MensajeError"] =
                        "No se puede eliminar la etiqueta porque tiene elementos asociados.";

                    return RedirectToAction(nameof(Categorias), new { tipo });
                }

                _context.Etiquetas.Remove(etiqueta);
            }

            try
            {
                await _context.SaveChangesAsync();

                TempData["MensajeAdmin"] =
                    "El elemento fue eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["MensajeError"] =
                    "No se puede eliminar el elemento porque está siendo utilizado por otros registros.";
            }

            return RedirectToAction(nameof(Categorias), new { tipo });
        }


        [HttpGet]
        public async Task<IActionResult> ElementosCategoria(int id, string tipo = "Categoria")
        {
            CategoriaAdminViewModel? categoria;
            List<ElementoAsociadoViewModel> elementos = [];

            if (tipo == "Etiqueta")
            {
                Etiqueta? etiqueta = await _context.Etiquetas
                    .AsNoTracking()
                    .Include(e => e.IdEmprendimientos)
                    .Include(e => e.IdProductos)
                    .Include(e => e.IdEventos)
                    .Include(e => e.IdTallers)
                    .FirstOrDefaultAsync(e => e.IdEtiqueta == id);

                if (etiqueta is null)
                {
                    TempData["ErrorAdmin"] = "La etiqueta no está disponible.";

                    return RedirectToAction(nameof(Categorias), new { tipo });
                }

                categoria = new CategoriaAdminViewModel
                {
                    Id = etiqueta.IdEtiqueta,
                    Nombre = etiqueta.Nombre,
                    Tipo = "Etiqueta",
                    Modulo = string.Empty
                };

                elementos.AddRange(etiqueta.IdEmprendimientos.Select(e => new ElementoAsociadoViewModel
                {
                    Nombre = e.NombreComercial,
                    Tipo = "Emprendimiento",
                    Estado = e.EstadoAprobacion
                }));

                elementos.AddRange(etiqueta.IdProductos.Select(p => new ElementoAsociadoViewModel
                {
                    Nombre = p.Nombre,
                    Tipo = "Producto",
                    Estado = p.Estado
                }));

                elementos.AddRange(etiqueta.IdEventos.Select(e => new ElementoAsociadoViewModel
                {
                    Nombre = e.Nombre,
                    Tipo = "Evento",
                    Estado = e.Estado
                }));

                elementos.AddRange(etiqueta.IdTallers.Select(t => new ElementoAsociadoViewModel
                {
                    Nombre = t.Nombre,
                    Tipo = "Taller",
                    Estado = t.Estado
                }));
            }
            else
            {
                Categoria? entidad = await _context.Categorias
                    .AsNoTracking()
                    .Include(c => c.Emprendimientos)
                    .Include(c => c.Productos)
                    .Include(c => c.Eventos)
                    .Include(c => c.Talleres)
                    .Include(c => c.Noticia)
                    .Include(c => c.Galeria)
                    .FirstOrDefaultAsync(c => c.IdCategoria == id);

                if (entidad is null)
                {
                    TempData["ErrorAdmin"] = "La categoría no está disponible.";

                    return RedirectToAction(nameof(Categorias), new { tipo });
                }

                categoria = new CategoriaAdminViewModel
                {
                    Id = entidad.IdCategoria,
                    Nombre = entidad.Nombre,
                    Descripcion = entidad.Descripcion ?? string.Empty,
                    Tipo = "Categoria",
                    Modulo = entidad.Modulo
                };

                elementos.AddRange(entidad.Emprendimientos.Select(e => new ElementoAsociadoViewModel
                {
                    Nombre = e.NombreComercial,
                    Tipo = "Emprendimiento",
                    Estado = e.EstadoAprobacion
                }));

                elementos.AddRange(entidad.Productos.Select(p => new ElementoAsociadoViewModel
                {
                    Nombre = p.Nombre,
                    Tipo = "Producto",
                    Estado = p.Estado
                }));

                elementos.AddRange(entidad.Eventos.Select(e => new ElementoAsociadoViewModel
                {
                    Nombre = e.Nombre,
                    Tipo = "Evento",
                    Estado = e.Estado
                }));

                elementos.AddRange(entidad.Talleres.Select(t => new ElementoAsociadoViewModel
                {
                    Nombre = t.Nombre,
                    Tipo = "Taller",
                    Estado = t.Estado
                }));

                elementos.AddRange(entidad.Noticia.Select(n => new ElementoAsociadoViewModel
                {
                    Nombre = n.Titulo,
                    Tipo = n.Tipo,
                    Estado = n.Estado
                }));

                elementos.AddRange(entidad.Galeria.Select(g => new ElementoAsociadoViewModel
                {
                    Nombre = g.Nombre,
                    Tipo = "Galería",
                    Estado = g.Estado
                }));
            }

            categoria.ElementosAsociados = elementos.Count;

            return View(new ElementosCategoriaViewModel
            {
                Categoria = categoria,
                Elementos = elementos.OrderBy(e => e.Tipo).ThenBy(e => e.Nombre).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Promociones(string estado = "Todas")
        {
            await _promociones.SincronizarEstadosAsync();

            IQueryable<Promocione> consulta = _context.Promociones.AsNoTracking();

            if (estado != "Todas")
            {
                consulta = consulta.Where(p => p.Estado == estado);
            }

            var datos = await consulta
                .OrderByDescending(p => p.FechaInicio)
                .Select(p => new
                {
                    p.IdPromocion,
                    p.Nombre,
                    p.Descripcion,
                    p.TipoDescuento,
                    p.ValorDescuento,
                    p.FechaInicio,
                    p.FechaFin,
                    p.Estado,
                    p.LimiteUsos,
                    Usos = p.UsosPromocions.Count(),
                    Emprendimientos = p.IdEmprendimientos.Select(e => e.NombreComercial).ToList(),
                    Productos = p.IdProductos.Select(x => x.Nombre).ToList(),
                    Eventos = p.IdEventos.Select(x => x.Nombre).ToList()
                })
                .ToListAsync();

            return View(new ListadoPromocionesAdminViewModel
            {
                FiltroEstado = estado,
                Promociones = datos
                    .Select(p => new PromocionAdminViewModel
                    {
                        Id = p.IdPromocion,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion,
                        TipoDescuento = p.TipoDescuento,
                        ValorDescuento = p.ValorDescuento,
                        FechaInicio = p.FechaInicio,
                        FechaFin = p.FechaFin,
                        Estado = p.Estado,
                        LimiteUsos = p.LimiteUsos,
                        Usos = p.Usos,
                        Emprendimiento = DescribirAplicacionPromocion(p.Emprendimientos, p.Productos, p.Eventos)
                    })
                    .ToList()
            });
        }

        private static string DescribirAplicacionPromocion(
            List<string> emprendimientos,
            List<string> productos,
            List<string> eventos)
        {
            List<string> partes = [];

            if (emprendimientos.Count > 0)
            {
                partes.Add(string.Join(", ", emprendimientos));
            }

            if (productos.Count > 0)
            {
                partes.Add("Producto: " + string.Join(", ", productos));
            }

            if (eventos.Count > 0)
            {
                partes.Add("Evento: " + string.Join(", ", eventos));
            }

            return partes.Count == 0 ? "Todos" : string.Join(" · ", partes);
        }

        private async Task CargarOpcionesPromocionAsync()
        {
            ViewBag.EmprendimientosPromocion = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.EstadoAprobacion == "Aprobado" && e.Activo)
                .OrderBy(e => e.NombreComercial)
                .Select(e => new { e.IdEmprendimiento, e.NombreComercial })
                .ToListAsync();

            ViewBag.ProductosPromocion = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Estado == "Publicado")
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.IdProducto, p.Nombre })
                .ToListAsync();

            ViewBag.EventosPromocion = await _context.Eventos
                .AsNoTracking()
                .Where(e => e.Estado != "Eliminado")
                .OrderByDescending(e => e.FechaInicio)
                .Select(e => new { e.IdEvento, e.Nombre })
                .ToListAsync();
        }

        private void ValidarPromocion(PromocionAdminViewModel modelo)
        {
            modelo.Nombre = (modelo.Nombre ?? string.Empty).Trim();

            List<string> faltantes = [];

            if (modelo.Nombre.Length == 0)
            {
                faltantes.Add("nombre");
            }

            if (!modelo.ValorDescuento.HasValue)
            {
                faltantes.Add("valor del descuento");
            }

            if (!modelo.FechaInicio.HasValue)
            {
                faltantes.Add("fecha de inicio");
            }

            if (!modelo.FechaFin.HasValue)
            {
                faltantes.Add("fecha de fin");
            }

            if (faltantes.Count > 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Complete los campos pendientes antes de guardar: {string.Join(", ", faltantes)}.");
            }

            if (modelo.TipoDescuento != "Porcentaje" && modelo.TipoDescuento != "MontoFijo")
            {
                ModelState.AddModelError(nameof(modelo.TipoDescuento), "Seleccione un tipo de descuento válido.");
            }

            if (modelo.TipoDescuento == "Porcentaje" &&
                modelo.ValorDescuento.HasValue &&
                modelo.ValorDescuento.Value > 100)
            {
                ModelState.AddModelError(nameof(modelo.ValorDescuento), "El porcentaje no puede superar el 100%.");
            }

            if (modelo.FechaInicio.HasValue &&
                modelo.FechaFin.HasValue &&
                modelo.FechaFin.Value.Date < modelo.FechaInicio.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaFin),
                    "El rango de fechas no es válido: la fecha de finalización es anterior a la de inicio.");
            }
        }

        private async Task AsignarAsociacionesPromocionAsync(Promocione promocion, PromocionAdminViewModel modelo)
        {
            promocion.IdEmprendimientos.Clear();
            promocion.IdProductos.Clear();
            promocion.IdEventos.Clear();

            if (modelo.IdEmprendimiento.HasValue)
            {
                Emprendimiento? emprendimiento = await _context.Emprendimientos
                    .FirstOrDefaultAsync(e => e.IdEmprendimiento == modelo.IdEmprendimiento.Value);

                if (emprendimiento is not null)
                {
                    promocion.IdEmprendimientos.Add(emprendimiento);
                }
            }

            if (modelo.IdProducto.HasValue)
            {
                Producto? producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == modelo.IdProducto.Value);

                if (producto is not null)
                {
                    promocion.IdProductos.Add(producto);
                }
            }

            if (modelo.IdEvento.HasValue)
            {
                Evento? evento = await _context.Eventos
                    .FirstOrDefaultAsync(e => e.IdEvento == modelo.IdEvento.Value);

                if (evento is not null)
                {
                    promocion.IdEventos.Add(evento);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> CrearPromocion()
        {
            ViewData["Titulo"] = "Crear promoción";

            await CargarOpcionesPromocionAsync();

            return View("FormularioPromocion", new PromocionAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPromocion(PromocionAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear promoción";

            ModelState.Remove(nameof(PromocionAdminViewModel.Estado));
            ModelState.Remove(nameof(PromocionAdminViewModel.Emprendimiento));

            ValidarPromocion(modelo);

            if (!ModelState.IsValid)
            {
                await CargarOpcionesPromocionAsync();

                return View("FormularioPromocion", modelo);
            }

            DateTime inicio = modelo.FechaInicio!.Value.Date;
            DateTime fin = modelo.FechaFin!.Value.Date.AddDays(1).AddSeconds(-1);

            Promocione promocion = new()
            {
                Nombre = modelo.Nombre,
                Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim(),
                TipoDescuento = modelo.TipoDescuento,
                ValorDescuento = modelo.ValorDescuento!.Value,
                FechaInicio = inicio,
                FechaFin = fin,
                LimiteUsos = modelo.LimiteUsos,
                Estado = PromocionesService.CalcularEstado(inicio, fin, DateTime.Now),
                FechaCreacion = DateTime.Now
            };

            await AsignarAsociacionesPromocionAsync(promocion, modelo);

            _context.Promociones.Add(promocion);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"La promoción \"{promocion.Nombre}\" se registró correctamente del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}.";

            return RedirectToAction(nameof(Promociones));
        }

        [HttpGet]
        public async Task<IActionResult> EditarPromocion(int id)
        {
            ViewData["Titulo"] = "Editar promoción";

            Promocione? promocion = await _context.Promociones
                .AsNoTracking()
                .Include(p => p.IdEmprendimientos)
                .Include(p => p.IdProductos)
                .Include(p => p.IdEventos)
                .Include(p => p.UsosPromocions)
                .FirstOrDefaultAsync(p => p.IdPromocion == id);

            if (promocion is null)
            {
                TempData["ErrorAdmin"] = "La promoción no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Promociones));
            }

            await CargarOpcionesPromocionAsync();

            return View("FormularioPromocion", new PromocionAdminViewModel
            {
                Id = promocion.IdPromocion,
                Nombre = promocion.Nombre,
                Descripcion = promocion.Descripcion,
                TipoDescuento = promocion.TipoDescuento,
                ValorDescuento = promocion.ValorDescuento,
                FechaInicio = promocion.FechaInicio.Date,
                FechaFin = promocion.FechaFin.Date,
                LimiteUsos = promocion.LimiteUsos,
                Estado = promocion.Estado,
                Usos = promocion.UsosPromocions.Count,
                IdEmprendimiento = promocion.IdEmprendimientos.Select(e => (int?)e.IdEmprendimiento).FirstOrDefault(),
                IdProducto = promocion.IdProductos.Select(p => (int?)p.IdProducto).FirstOrDefault(),
                IdEvento = promocion.IdEventos.Select(e => (int?)e.IdEvento).FirstOrDefault()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPromocion(PromocionAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar promoción";

            ModelState.Remove(nameof(PromocionAdminViewModel.Estado));
            ModelState.Remove(nameof(PromocionAdminViewModel.Emprendimiento));

            Promocione? promocion = await _context.Promociones
                .Include(p => p.IdEmprendimientos)
                .Include(p => p.IdProductos)
                .Include(p => p.IdEventos)
                .FirstOrDefaultAsync(p => p.IdPromocion == modelo.Id);

            if (promocion is null)
            {
                TempData["ErrorAdmin"] = "La promoción no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Promociones));
            }

            ValidarPromocion(modelo);

            if (!ModelState.IsValid)
            {
                modelo.Estado = promocion.Estado;

                await CargarOpcionesPromocionAsync();

                return View("FormularioPromocion", modelo);
            }

            DateTime inicio = modelo.FechaInicio!.Value.Date;
            DateTime fin = modelo.FechaFin!.Value.Date.AddDays(1).AddSeconds(-1);

            promocion.Nombre = modelo.Nombre;
            promocion.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim();
            promocion.TipoDescuento = modelo.TipoDescuento;
            promocion.ValorDescuento = modelo.ValorDescuento!.Value;
            promocion.FechaInicio = inicio;
            promocion.FechaFin = fin;
            promocion.LimiteUsos = modelo.LimiteUsos;

            if (promocion.Estado != PromocionesService.Desactivada)
            {
                promocion.Estado = PromocionesService.CalcularEstado(inicio, fin, DateTime.Now);
            }

            await AsignarAsociacionesPromocionAsync(promocion, modelo);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"La promoción \"{promocion.Nombre}\" fue actualizada correctamente.";

            return RedirectToAction(nameof(Promociones));
        }

        [HttpGet]
        public async Task<IActionResult> DetallePromocion(int id)
        {
            await _promociones.SincronizarEstadosAsync();

            var promocion = await _context.Promociones
                .AsNoTracking()
                .Where(p => p.IdPromocion == id)
                .Select(p => new
                {
                    p.IdPromocion,
                    p.Nombre,
                    p.Descripcion,
                    p.TipoDescuento,
                    p.ValorDescuento,
                    p.FechaInicio,
                    p.FechaFin,
                    p.Estado,
                    p.LimiteUsos,
                    Usos = p.UsosPromocions.Count(),
                    Emprendimientos = p.IdEmprendimientos.Select(e => e.NombreComercial).ToList(),
                    Productos = p.IdProductos.Select(x => x.Nombre).ToList(),
                    Eventos = p.IdEventos.Select(x => x.Nombre).ToList()
                })
                .FirstOrDefaultAsync();

            if (promocion is null)
            {
                TempData["ErrorAdmin"] = "La promoción no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Promociones));
            }

            return View(new PromocionAdminViewModel
            {
                Id = promocion.IdPromocion,
                Nombre = promocion.Nombre,
                Descripcion = promocion.Descripcion,
                TipoDescuento = promocion.TipoDescuento,
                ValorDescuento = promocion.ValorDescuento,
                FechaInicio = promocion.FechaInicio,
                FechaFin = promocion.FechaFin,
                Estado = promocion.Estado,
                LimiteUsos = promocion.LimiteUsos,
                Usos = promocion.Usos,
                Emprendimiento = DescribirAplicacionPromocion(
                    promocion.Emprendimientos,
                    promocion.Productos,
                    promocion.Eventos)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesactivarPromocion(int id)
        {
            Promocione? promocion = await _context.Promociones
                .FirstOrDefaultAsync(p => p.IdPromocion == id);

            if (promocion is null)
            {
                TempData["ErrorAdmin"] = "La promoción no está disponible.";

                return RedirectToAction(nameof(Promociones));
            }

            promocion.Estado = PromocionesService.Desactivada;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"La promoción \"{promocion.Nombre}\" fue desactivada.";

            return RedirectToAction(nameof(Promociones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPromocion(int id)
        {
            Promocione? promocion = await _context.Promociones
                .Include(p => p.IdEmprendimientos)
                .Include(p => p.IdProductos)
                .Include(p => p.IdEventos)
                .Include(p => p.UsosPromocions)
                .FirstOrDefaultAsync(p => p.IdPromocion == id);

            if (promocion is null)
            {
                TempData["ErrorAdmin"] = "La promoción no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Promociones));
            }

            if (promocion.UsosPromocions.Count > 0)
            {
                TempData["ErrorAdmin"] =
                    $"La promoción \"{promocion.Nombre}\" ya tiene usos registrados y no se puede eliminar. Puede desactivarla.";

                return RedirectToAction(nameof(Promociones));
            }

            string nombre = promocion.Nombre;

            promocion.IdEmprendimientos.Clear();
            promocion.IdProductos.Clear();
            promocion.IdEventos.Clear();

            _context.Promociones.Remove(promocion);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"La promoción \"{nombre}\" fue eliminada correctamente.";

            return RedirectToAction(nameof(Promociones));
        }

        [HttpGet]
        public async Task<IActionResult> Encuestas(string estado = "Todas")
        {
            IQueryable<Encuesta> consulta = _context.Encuestas.AsNoTracking();

            if (estado != "Todas")
            {
                consulta = consulta.Where(e => e.Estado == estado);
            }

            var datos = await consulta
                .OrderByDescending(e => e.FechaCreacion)
                .Select(e => new
                {
                    e.IdEncuesta,
                    e.Titulo,
                    e.Descripcion,
                    e.Estado,
                    e.FechaCreacion,
                    Preguntas = e.PreguntasEncuesta.Count(),
                    Respuestas = e.RespuestasEncuesta.Count(),
                    Eventos = e.IdEventos.Select(x => x.Nombre).ToList(),
                    Talleres = e.IdTallers.Select(x => x.Nombre).ToList()
                })
                .ToListAsync();

            return View(new ListadoEncuestasAdminViewModel
            {
                FiltroEstado = estado,
                Encuestas = datos
                    .Select(e => new EncuestaAdminViewModel
                    {
                        Id = e.IdEncuesta,
                        Titulo = e.Titulo,
                        Descripcion = e.Descripcion,
                        Estado = e.Estado,
                        FechaCreacion = e.FechaCreacion.ToString("dd/MM/yyyy"),
                        TotalPreguntas = e.Preguntas,
                        TotalRespuestas = e.Respuestas,
                        Asociada = string.Join(", ", e.Eventos.Concat(e.Talleres))
                    })
                    .ToList()
            });
        }

        private async Task CargarOpcionesEncuestaAsync()
        {
            ViewBag.EventosEncuesta = await _context.Eventos
                .AsNoTracking()
                .Where(e => e.Estado != "Eliminado")
                .OrderByDescending(e => e.FechaInicio)
                .Select(e => new { e.IdEvento, e.Nombre })
                .ToListAsync();

            ViewBag.TalleresEncuesta = await _context.Talleres
                .AsNoTracking()
                .Where(t => t.Estado != "Eliminado")
                .OrderByDescending(t => t.FechaInicio)
                .Select(t => new { t.IdTaller, t.Nombre })
                .ToListAsync();
        }

        private static List<string> SepararOpciones(string? texto)
        {
            return (texto ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(o => o.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ValidarEncuesta(EncuestaAdminViewModel modelo, bool publicar)
        {
            modelo.Titulo = (modelo.Titulo ?? string.Empty).Trim();
            modelo.IdEventos ??= [];
            modelo.IdTalleres ??= [];

            modelo.Preguntas = (modelo.Preguntas ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p.Texto) || !string.IsNullOrWhiteSpace(p.OpcionesTexto))
                .ToList();

            List<string> faltantes = [];

            if (modelo.Titulo.Length == 0)
            {
                faltantes.Add("título");
            }

            if (modelo.Preguntas.Count == 0)
            {
                faltantes.Add("al menos una pregunta");
            }

            for (int i = 0; i < modelo.Preguntas.Count; i++)
            {
                PreguntaFormViewModel pregunta = modelo.Preguntas[i];
                int numero = i + 1;

                if (string.IsNullOrWhiteSpace(pregunta.Texto))
                {
                    faltantes.Add($"texto de la pregunta {numero}");
                    continue;
                }

                if (pregunta.Texto.Trim().Length > 800)
                {
                    ModelState.AddModelError(string.Empty, $"La pregunta {numero} supera los 800 caracteres.");
                }

                if (pregunta.Tipo != ReglasEncuestas.TipoOpcion &&
                    pregunta.Tipo != ReglasEncuestas.TipoEscala &&
                    pregunta.Tipo != ReglasEncuestas.TipoTexto)
                {
                    ModelState.AddModelError(string.Empty, $"La pregunta {numero} tiene un tipo no válido.");
                }

                if (pregunta.Tipo == ReglasEncuestas.TipoOpcion &&
                    SepararOpciones(pregunta.OpcionesTexto).Count < 2)
                {
                    ModelState.AddModelError(string.Empty, $"La pregunta {numero} necesita al menos dos opciones de respuesta.");
                }
            }

            if (faltantes.Count > 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Complete lo pendiente antes de guardar: {string.Join(", ", faltantes)}.");
            }

            if (publicar && modelo.IdEventos.Count == 0 && modelo.IdTalleres.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Asocie la encuesta al menos a un evento o taller para poder publicarla.");
            }
        }

        private async Task AsignarAsociacionesEncuestaAsync(Encuesta encuesta, EncuestaAdminViewModel modelo)
        {
            encuesta.IdEventos.Clear();
            encuesta.IdTallers.Clear();

            List<Evento> eventos = await _context.Eventos
                .Where(e => modelo.IdEventos.Contains(e.IdEvento))
                .ToListAsync();

            foreach (Evento evento in eventos)
            {
                encuesta.IdEventos.Add(evento);
            }

            List<Tallere> talleres = await _context.Talleres
                .Where(t => modelo.IdTalleres.Contains(t.IdTaller))
                .ToListAsync();

            foreach (Tallere taller in talleres)
            {
                encuesta.IdTallers.Add(taller);
            }
        }

        private static List<PreguntasEncuestum> ConstruirPreguntas(EncuestaAdminViewModel modelo)
        {
            List<PreguntasEncuestum> preguntas = [];

            for (int i = 0; i < modelo.Preguntas.Count; i++)
            {
                PreguntaFormViewModel origen = modelo.Preguntas[i];

                PreguntasEncuestum pregunta = new()
                {
                    Texto = origen.Texto!.Trim(),
                    TipoPregunta = origen.Tipo,
                    Obligatoria = origen.Obligatoria,
                    OrdenVisual = (short)(i + 1)
                };

                if (origen.Tipo == ReglasEncuestas.TipoOpcion)
                {
                    List<string> opciones = SepararOpciones(origen.OpcionesTexto);

                    for (int j = 0; j < opciones.Count; j++)
                    {
                        pregunta.OpcionesPregunta.Add(new OpcionesPreguntum
                        {
                            Texto = opciones[j],
                            OrdenVisual = (short)(j + 1)
                        });
                    }
                }

                preguntas.Add(pregunta);
            }

            return preguntas;
        }

        [HttpGet]
        public async Task<IActionResult> CrearEncuesta()
        {
            ViewData["Titulo"] = "Crear encuesta";

            await CargarOpcionesEncuestaAsync();

            return View("FormularioEncuesta", new EncuestaAdminViewModel
            {
                Preguntas = [new PreguntaFormViewModel()]
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEncuesta(EncuestaAdminViewModel modelo, string accion = "guardar")
        {
            ViewData["Titulo"] = "Crear encuesta";

            bool publicar = accion == "publicar";

            ModelState.Clear();
            ValidarEncuesta(modelo, publicar);

            if (!ModelState.IsValid)
            {
                await CargarOpcionesEncuestaAsync();

                if (modelo.Preguntas.Count == 0)
                {
                    modelo.Preguntas = [new PreguntaFormViewModel()];
                }

                return View("FormularioEncuesta", modelo);
            }

            int? idCreador = HttpContext.Session.GetInt32("IdUsuario");

            if (idCreador is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Encuesta encuesta = new()
            {
                IdCreador = idCreador.Value,
                Titulo = modelo.Titulo,
                Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim(),
                Estado = publicar ? ReglasEncuestas.Publicada : ReglasEncuestas.Borrador,
                FechaCreacion = DateTime.Now,
                FechaPublicacion = publicar ? DateTime.Now : null
            };

            foreach (PreguntasEncuestum pregunta in ConstruirPreguntas(modelo))
            {
                encuesta.PreguntasEncuesta.Add(pregunta);
            }

            await AsignarAsociacionesEncuestaAsync(encuesta, modelo);

            _context.Encuestas.Add(encuesta);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = publicar
                ? $"La encuesta \"{encuesta.Titulo}\" se creó con {encuesta.PreguntasEncuesta.Count} pregunta(s) y quedó publicada."
                : $"La encuesta \"{encuesta.Titulo}\" se guardó con {encuesta.PreguntasEncuesta.Count} pregunta(s) como borrador.";

            return RedirectToAction(nameof(Encuestas));
        }

        [HttpGet]
        public async Task<IActionResult> EditarEncuesta(int id)
        {
            ViewData["Titulo"] = "Editar encuesta";

            Encuesta? encuesta = await _context.Encuestas
                .AsNoTracking()
                .Include(e => e.PreguntasEncuesta).ThenInclude(p => p.OpcionesPregunta)
                .Include(e => e.IdEventos)
                .Include(e => e.IdTallers)
                .FirstOrDefaultAsync(e => e.IdEncuesta == id);

            if (encuesta is null)
            {
                TempData["ErrorAdmin"] = "La encuesta no está disponible.";

                return RedirectToAction(nameof(Encuestas));
            }

            int respuestas = await _context.RespuestasEncuesta.CountAsync(r => r.IdEncuesta == id);

            if (respuestas > 0)
            {
                TempData["ErrorAdmin"] =
                    "La encuesta ya tiene respuestas y no se puede modificar. Puede cerrarla o crear una nueva.";

                return RedirectToAction(nameof(Encuestas));
            }

            await CargarOpcionesEncuestaAsync();

            return View("FormularioEncuesta", new EncuestaAdminViewModel
            {
                Id = encuesta.IdEncuesta,
                Titulo = encuesta.Titulo,
                Descripcion = encuesta.Descripcion,
                Estado = encuesta.Estado,
                IdEventos = encuesta.IdEventos.Select(e => e.IdEvento).ToList(),
                IdTalleres = encuesta.IdTallers.Select(t => t.IdTaller).ToList(),
                Preguntas = encuesta.PreguntasEncuesta
                    .OrderBy(p => p.OrdenVisual)
                    .Select(p => new PreguntaFormViewModel
                    {
                        Texto = p.Texto,
                        Tipo = p.TipoPregunta,
                        Obligatoria = p.Obligatoria,
                        OpcionesTexto = string.Join("\n", p.OpcionesPregunta.OrderBy(o => o.OrdenVisual).Select(o => o.Texto))
                    })
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEncuesta(EncuestaAdminViewModel modelo, string accion = "guardar")
        {
            ViewData["Titulo"] = "Editar encuesta";

            Encuesta? encuesta = await _context.Encuestas
                .Include(e => e.PreguntasEncuesta).ThenInclude(p => p.OpcionesPregunta)
                .Include(e => e.IdEventos)
                .Include(e => e.IdTallers)
                .FirstOrDefaultAsync(e => e.IdEncuesta == modelo.Id);

            if (encuesta is null)
            {
                TempData["ErrorAdmin"] = "La encuesta no está disponible.";

                return RedirectToAction(nameof(Encuestas));
            }

            if (await _context.RespuestasEncuesta.AnyAsync(r => r.IdEncuesta == modelo.Id))
            {
                TempData["ErrorAdmin"] =
                    "La encuesta ya tiene respuestas y no se puede modificar. Puede cerrarla o crear una nueva.";

                return RedirectToAction(nameof(Encuestas));
            }

            bool publicar = accion == "publicar" && encuesta.Estado == ReglasEncuestas.Borrador;

            ModelState.Clear();
            ValidarEncuesta(modelo, publicar || encuesta.Estado == ReglasEncuestas.Publicada);

            if (!ModelState.IsValid)
            {
                await CargarOpcionesEncuestaAsync();

                modelo.Estado = encuesta.Estado;

                if (modelo.Preguntas.Count == 0)
                {
                    modelo.Preguntas = [new PreguntaFormViewModel()];
                }

                return View("FormularioEncuesta", modelo);
            }

            encuesta.Titulo = modelo.Titulo;
            encuesta.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim();

            List<OpcionesPreguntum> opcionesAnteriores = encuesta.PreguntasEncuesta
                .SelectMany(p => p.OpcionesPregunta)
                .ToList();

            _context.OpcionesPregunta.RemoveRange(opcionesAnteriores);
            _context.PreguntasEncuesta.RemoveRange(encuesta.PreguntasEncuesta.ToList());

            foreach (PreguntasEncuestum pregunta in ConstruirPreguntas(modelo))
            {
                pregunta.IdEncuestaNavigation = encuesta;
                _context.PreguntasEncuesta.Add(pregunta);
            }

            await AsignarAsociacionesEncuestaAsync(encuesta, modelo);

            if (publicar)
            {
                encuesta.Estado = ReglasEncuestas.Publicada;
                encuesta.FechaPublicacion = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = publicar
                ? $"La encuesta \"{encuesta.Titulo}\" fue actualizada y publicada correctamente."
                : $"La encuesta \"{encuesta.Titulo}\" fue actualizada correctamente.";

            return RedirectToAction(nameof(Encuestas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublicarEncuesta(int id)
        {
            Encuesta? encuesta = await _context.Encuestas
                .Include(e => e.IdEventos)
                .Include(e => e.IdTallers)
                .FirstOrDefaultAsync(e => e.IdEncuesta == id);

            if (encuesta is null)
            {
                TempData["ErrorAdmin"] = "La encuesta no está disponible.";

                return RedirectToAction(nameof(Encuestas));
            }

            if (encuesta.Estado != ReglasEncuestas.Borrador)
            {
                TempData["ErrorAdmin"] = "Solo se pueden publicar encuestas en borrador.";

                return RedirectToAction(nameof(Encuestas));
            }

            if (!await _context.PreguntasEncuesta.AnyAsync(p => p.IdEncuesta == id))
            {
                TempData["ErrorAdmin"] = "La encuesta no tiene preguntas. Agregue al menos una antes de publicarla.";

                return RedirectToAction(nameof(Encuestas));
            }

            if (encuesta.IdEventos.Count == 0 && encuesta.IdTallers.Count == 0)
            {
                TempData["ErrorAdmin"] =
                    "Asocie la encuesta al menos a un evento o taller para poder publicarla.";

                return RedirectToAction(nameof(Encuestas));
            }

            encuesta.Estado = ReglasEncuestas.Publicada;
            encuesta.FechaPublicacion = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"La encuesta \"{encuesta.Titulo}\" fue publicada y ya está disponible para los asistentes.";

            return RedirectToAction(nameof(Encuestas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarEncuesta(int id)
        {
            Encuesta? encuesta = await _context.Encuestas
                .FirstOrDefaultAsync(e => e.IdEncuesta == id);

            if (encuesta is null || encuesta.Estado != ReglasEncuestas.Publicada)
            {
                TempData["ErrorAdmin"] = "Solo se pueden cerrar encuestas publicadas.";

                return RedirectToAction(nameof(Encuestas));
            }

            encuesta.Estado = ReglasEncuestas.Cerrada;
            encuesta.FechaCierre = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = "La encuesta fue cerrada. Ya no se aceptan más respuestas.";

            return RedirectToAction(nameof(Encuestas));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEncuesta(int id)
        {
            Encuesta? encuesta = await _context.Encuestas
                .Include(e => e.PreguntasEncuesta).ThenInclude(p => p.OpcionesPregunta)
                .Include(e => e.IdEventos)
                .Include(e => e.IdTallers)
                .FirstOrDefaultAsync(e => e.IdEncuesta == id);

            if (encuesta is null)
            {
                TempData["ErrorAdmin"] = "La encuesta no está disponible. Es posible que ya haya sido eliminada.";

                return RedirectToAction(nameof(Encuestas));
            }

            if (await _context.RespuestasEncuesta.AnyAsync(r => r.IdEncuesta == id))
            {
                TempData["ErrorAdmin"] =
                    $"La encuesta \"{encuesta.Titulo}\" ya tiene respuestas y no se puede eliminar. Puede cerrarla para conservar los resultados.";

                return RedirectToAction(nameof(Encuestas));
            }

            string titulo = encuesta.Titulo;

            encuesta.IdEventos.Clear();
            encuesta.IdTallers.Clear();

            _context.OpcionesPregunta.RemoveRange(
                encuesta.PreguntasEncuesta.SelectMany(p => p.OpcionesPregunta).ToList());
            _context.PreguntasEncuesta.RemoveRange(encuesta.PreguntasEncuesta.ToList());
            _context.Encuestas.Remove(encuesta);

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] = $"La encuesta \"{titulo}\" fue eliminada correctamente.";

            return RedirectToAction(nameof(Encuestas));
        }
        [HttpGet]
        public async Task<IActionResult> ResultadosEncuesta(int id)
        {
            Encuesta? encuesta = await _context.Encuestas
                .AsNoTracking()
                .Include(e => e.PreguntasEncuesta).ThenInclude(p => p.OpcionesPregunta)
                .FirstOrDefaultAsync(e => e.IdEncuesta == id);

            if (encuesta is null)
            {
                TempData["ErrorAdmin"] = "La encuesta no está disponible.";

                return RedirectToAction(nameof(Encuestas));
            }

            var detalles = await _context.RespuestaDetalles
                .AsNoTracking()
                .Where(d => d.IdRespuestaEncuestaNavigation.IdEncuesta == id)
                .Select(d => new { d.IdPregunta, d.IdOpcion, d.RespuestaTexto, d.ValorNumerico })
                .ToListAsync();

            int totalRespuestas = await _context.RespuestasEncuesta.CountAsync(r => r.IdEncuesta == id);

            List<PreguntaEncuestaViewModel> resultados = [];

            foreach (PreguntasEncuestum pregunta in encuesta.PreguntasEncuesta.OrderBy(p => p.OrdenVisual))
            {
                var respuestas = detalles.Where(d => d.IdPregunta == pregunta.IdPregunta).ToList();

                PreguntaEncuestaViewModel resultado = new()
                {
                    Texto = pregunta.Texto,
                    Tipo = pregunta.TipoPregunta,
                    Respuestas = respuestas.Count
                };

                if (pregunta.TipoPregunta == ReglasEncuestas.TipoOpcion)
                {
                    resultado.Opciones = pregunta.OpcionesPregunta
                        .OrderBy(o => o.OrdenVisual)
                        .Select(o => new OpcionPreguntaViewModel
                        {
                            Texto = o.Texto,
                            Votos = respuestas.Count(r => r.IdOpcion == o.IdOpcion)
                        })
                        .ToList();
                }
                else if (pregunta.TipoPregunta == ReglasEncuestas.TipoEscala)
                {
                    for (int valor = ReglasEncuestas.EscalaMinima; valor <= ReglasEncuestas.EscalaMaxima; valor++)
                    {
                        resultado.Opciones.Add(new OpcionPreguntaViewModel
                        {
                            Texto = valor.ToString(),
                            Votos = respuestas.Count(r => r.ValorNumerico == valor)
                        });
                    }

                    List<decimal> valores = respuestas
                        .Where(r => r.ValorNumerico.HasValue)
                        .Select(r => r.ValorNumerico!.Value)
                        .ToList();

                    resultado.Promedio = valores.Count > 0 ? Math.Round(valores.Average(), 1) : null;
                }
                else
                {
                    resultado.RespuestasTexto = respuestas
                        .Where(r => !string.IsNullOrWhiteSpace(r.RespuestaTexto))
                        .Select(r => r.RespuestaTexto!)
                        .ToList();
                }

                resultados.Add(resultado);
            }

            return View(new EncuestaAdminViewModel
            {
                Id = encuesta.IdEncuesta,
                Titulo = encuesta.Titulo,
                Descripcion = encuesta.Descripcion,
                Estado = encuesta.Estado,
                FechaCreacion = encuesta.FechaCreacion.ToString("dd/MM/yyyy"),
                TotalRespuestas = totalRespuestas,
                TotalPreguntas = resultados.Count,
                Resultados = resultados
            });
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Bitacora(FiltroBitacoraViewModel filtro)
        {
            filtro ??= new FiltroBitacoraViewModel();

            IQueryable<Auditorium> consulta = _context.Auditoria
                .AsNoTracking()
                .Include(a => a.IdUsuarioNavigation);

            if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            {
                string usuario = filtro.Usuario.Trim();

                consulta = consulta.Where(a =>
                    a.IdUsuarioNavigation != null &&
                    (a.IdUsuarioNavigation.Nombre + " " + a.IdUsuarioNavigation.ApellidoPaterno)
                        .Contains(usuario));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Accion) && filtro.Accion != "Todas")
            {
                consulta = consulta.Where(a => a.Accion == filtro.Accion);
            }

            if (filtro.FechaInicio is not null)
            {
                DateTime desde = filtro.FechaInicio.Value.Date;

                consulta = consulta.Where(a => a.FechaHora >= desde);
            }

            if (filtro.FechaFin is not null)
            {
                DateTime hasta = filtro.FechaFin.Value.Date.AddDays(1);

                consulta = consulta.Where(a => a.FechaHora < hasta);
            }

            List<Auditorium> registros = await consulta
                .OrderByDescending(a => a.FechaHora)
                .Take(200)
                .ToListAsync();

            ViewBag.AccionesBitacora = await _context.Auditoria
                .AsNoTracking()
                .Select(a => a.Accion)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            List<Auditorium> alertasSeguridad = await _context.Auditoria
    .AsNoTracking()
    .Where(a => a.Modulo == "Seguridad")
    .OrderByDescending(a => a.FechaHora)
    .Take(10)
    .ToListAsync();

            DateTime hace24Horas = DateTime.Now.AddHours(-24);

            var intentosFallidos = await _context.IntentosAccesos
                .AsNoTracking()
                .Where(i => !i.Exitoso && i.FechaHora >= hace24Horas)
                .GroupBy(i => i.Correo)
                .Select(g => new
                {
                    Correo = g.Key,
                    Intentos = g.Count(),
                    Fecha = g.Max(i => i.FechaHora),
                    DireccionIp = g.Max(i => i.DireccionIp)
                })
                .OrderByDescending(g => g.Fecha)
                .Take(10)
                .ToListAsync();

            int idAdministrador = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            List<Notificacione> notificacionesSeguridad = await _context.Notificaciones
                .Where(n =>
                    n.IdUsuario == idAdministrador &&
                    n.Tipo == "Seguridad" &&
                    !n.Leida)
                .ToListAsync();

            foreach (Notificacione notificacion in notificacionesSeguridad)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.Now;
            }

            if (notificacionesSeguridad.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return View(new BitacoraAdminViewModel
            {
                Filtro = filtro,
                Movimientos = registros.Select(ConvertirMovimiento).ToList(),
                Alertas = alertasSeguridad.Select(a => new AlertaAdminViewModel
                {
                    Tipo = "Seguridad",
                    Mensaje = a.Descripcion ?? a.Accion,
                    Fecha = a.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    Icono = a.Accion == "AccesoNoAutorizado" ? "bi-lock-fill" : "bi-shield-exclamation",
                    Nivel = a.Nivel == "Critico" ? "critica" : "advertencia"
                }).ToList(),
                IntentosFallidos = intentosFallidos.Select(i => new IntentoFallidoViewModel
                {
                    Correo = i.Correo,
                    DireccionIp = i.DireccionIp ?? "",
                    Fecha = i.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    Intentos = i.Intentos,
                    Bloqueado = false
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> DetalleMovimiento(int id)
        {
            Auditorium? registro = await _context.Auditoria
                .AsNoTracking()
                .Include(a => a.IdUsuarioNavigation)
                .FirstOrDefaultAsync(a => a.IdAuditoria == id);

            if (registro is null)
            {
                TempData["ErrorAdmin"] = "El registro de auditoría no está disponible.";

                return RedirectToAction(nameof(Bitacora));
            }

            return View(ConvertirMovimiento(registro));
        }

        private static MovimientoBitacoraViewModel ConvertirMovimiento(Auditorium registro)
        {
            return new MovimientoBitacoraViewModel
            {
                Id = (int)registro.IdAuditoria,
                Usuario = registro.IdUsuarioNavigation is null
                    ? "Sistema"
                    : $"{registro.IdUsuarioNavigation.Nombre} {registro.IdUsuarioNavigation.ApellidoPaterno}",
                Accion = registro.Accion,
                Modulo = registro.Modulo,
                Fecha = registro.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                DireccionIp = registro.DireccionIp ?? "",
                Detalle = registro.Descripcion ?? "",
                Nivel = registro.Nivel switch
                {
                    "Critico" => "critica",
                    "Advertencia" => "advertencia",
                    _ => "informativa"
                }
            };
        }
        
    }
}
