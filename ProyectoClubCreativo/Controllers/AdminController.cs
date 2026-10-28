using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models;
using ProyectoClubCreativo.Models.Entities;
using ProyectoClubCreativo.Models.ViewModels.Admin;
using ProyectoClubCreativo.Services;

namespace ProyectoClubCreativo.Controllers
{
    public class AdminController : Controller
    {

        private readonly IWebHostEnvironment _entornoWeb;
        private readonly ClubCreativoDbContext _context;
        private readonly CorreoService _correoService;

        public AdminController(
            IWebHostEnvironment entornoWeb,
            ClubCreativoDbContext context,
            CorreoService correoService)
        {
            _entornoWeb = entornoWeb;
            _context = context;
            _correoService = correoService;
        }

        public override void OnActionExecuting(
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

            if (idUsuario is null || rolUsuario != "Administrador")
            {
                context.Result = RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );

                return;
            }

            base.OnActionExecuting(context);
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

            var eventos = await _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    (e.Estado == "Programado" || e.Estado == "Activo") &&
                    e.FechaInicio >= ahora)
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

            var registrosEvento = await _context.InscripcionesEventos
                .AsNoTracking()
                .OrderByDescending(i => i.FechaInscripcion)
                .Take(4)
                .Select(i => new
                {
                    i.FechaInscripcion,
                    i.Estado,
                    Persona = i.IdUsuarioNavigation.Nombre + " " + i.IdUsuarioNavigation.ApellidoPaterno,
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

            List<ActividadAdminViewModel> actividad = registrosTaller
                .Select(r => (r.FechaInscripcion, r.Estado, r.Persona, r.Actividad, Tipo: "taller"))
                .Concat(registrosEvento
                    .Select(r => (r.FechaInscripcion, r.Estado, r.Persona, r.Actividad, Tipo: "evento")))
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

            DashboardAdminViewModel modelo = new()
            {
                Filtro = filtro,
                TotalUsuarios = await _context.Usuarios.CountAsync(),

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


            GestionSuscripcionesViewModel modelo = new()
            {
                Planes = planes,
                Activas = activas,
                TotalPorVencer =
                    activas.Count(s =>
                        s.Estado == "Por vencer")
            };


            return View(modelo);
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
            plan.Activo = modelo.Activo;

            await _context.SaveChangesAsync();

            TempData["MensajeAdmin"] =
                $"El plan \"{plan.Nombre}\" fue actualizado correctamente.";

            return RedirectToAction(nameof(Suscripciones));
        }

        [HttpGet]
        public IActionResult Productos(string? busqueda, string? categoria, string? estado)
        {
            List<ProductoAdminViewModel> productos = ObtenerProductosDemo();

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
        public IActionResult RevisarProducto(int id = 1)
        {
            ProductoAdminViewModel modelo =
                ObtenerProductosDemo().FirstOrDefault(p => p.Id == id)
                ?? ObtenerProductosDemo().First();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstadoProducto(int id, string estado)
        {
            TempData["MensajeAdmin"] = $"El estado del producto se actualizó a \"{estado}\".";
            return RedirectToAction(nameof(RevisarProducto), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProducto(int id)
        {
            TempData["MensajeAdmin"] = "El producto fue eliminado por contenido inapropiado.";
            return RedirectToAction(nameof(Productos));
        }

        private static List<ProductoAdminViewModel> ObtenerProductosDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Aretes Orquídea", Emprendimiento = "Orquídea", Categoria = "Accesorios", Precio = 12000, Estado = "Activo", Imagen = "/images/producto-1.jpg", FechaPublicacion = "10/06/2026", Reportes = 0, Descripcion = "Aretes tejidos a mano con hilo encerado." },
                new() { Id = 2, Nombre = "Vela Aromática Lavanda", Emprendimiento = "Luz Natural", Categoria = "Velas y aromas", Precio = 6500, Estado = "Activo", Imagen = "/images/producto-3.jpg", FechaPublicacion = "02/07/2026", Reportes = 0, Descripcion = "Vela de cera de soya con esencia de lavanda." },
                new() { Id = 3, Nombre = "Cuadro decorativo Boho", Emprendimiento = "Artesanías MiVo", Categoria = "Hogar y decoración", Precio = 18500, Estado = "En revisión", Imagen = "/images/producto-1.jpg", FechaPublicacion = "20/07/2026", Reportes = 2, Descripcion = "Cuadro tejido estilo boho para decoración de interiores." },
                new() { Id = 4, Nombre = "Ilustración digital personalizada", Emprendimiento = "Trazo Libre", Categoria = "Arte e ilustración", Precio = 9000, Estado = "Pausado", Imagen = "/images/producto-3.jpg", FechaPublicacion = "15/05/2026", Reportes = 1, Descripcion = "Retrato ilustrado digital por encargo." }
            ];
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
                FechaRegistro = DateTime.Now
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

            bool cambioFecha =
                evento.FechaInicio != modelo.FechaInicio!.Value ||
                evento.FechaFin != modelo.FechaFin;

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
                    _context.Notificaciones.Add(new Notificacione
                    {
                        IdUsuario = idUsuario,
                        Titulo = "Cambio en un evento",
                        Mensaje = mensaje,
                        Tipo = "Evento",
                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    });
                }

                notificados = idsUsuarios.Count;
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

                    _context.Notificaciones.Add(new Notificacione
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

        private static List<EventoAdminViewModel> ObtenerEventosDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Arte Inarrivo San Pedro", Fecha = "15 y 16 de agosto", Ubicacion = "San Pedro", CupoTotal = 150, Inscritos = 120, Estado = "Programado", QrGenerado = true,
            EmprendimientosAsignados = ["Orquídea", "Luz Natural"],
            Participantes = [ new() { Nombre = "Maria Montero Cruz", Emprendimiento = "Orquídea", Estado = "Confirmado" }, new() { Nombre = "Carla Ramírez Solís", Emprendimiento = "Luz Natural", Estado = "Pendiente" } ] },
        new() { Id = 2, Nombre = "Feria Creativa Santa Ana", Fecha = "5 al 7 de septiembre", Ubicacion = "Santa Ana", CupoTotal = 100, Inscritos = 86, Estado = "Programado", QrGenerado = false,
            EmprendimientosAsignados = ["Artesanías MiVo"], Participantes = [] },
        new() { Id = 3, Nombre = "Feria Creativa Heredia", Fecha = "20 de septiembre", Ubicacion = "Heredia", CupoTotal = 60, Inscritos = 40, Estado = "Cancelado", QrGenerado = false, Participantes = [] }
            ];
        }

        private async Task CargarOpcionesTallerAsync()
        {
            ViewBag.CategoriasTaller = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Modulo == "Talleres" && c.Activa)
                .OrderBy(c => c.Nombre)
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
                FechaRegistro = DateTime.Now
            };

            AplicarDatosTaller(taller, modelo);

            if (modelo.Imagen is { Length: > 0 })
            {
                taller.ImagenUrl = await GuardarImagenTallerAsync(modelo.Imagen);
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

            AplicarDatosTaller(taller, modelo);

            if (eraBorrador && publicar)
            {
                taller.Estado = ReglasTalleres.Programado;
            }

            if (modelo.Imagen is { Length: > 0 })
            {
                taller.ImagenUrl = await GuardarImagenTallerAsync(modelo.Imagen);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerarQrTaller(int id)
        {
            TempData["MensajeAdmin"] = "El código QR del taller fue generado correctamente.";
            return RedirectToAction(nameof(InscritosTaller), new { id });
        }

        [HttpGet]
        public IActionResult Ventas(string estado = "Todas")
        {
            List<VentaAdminViewModel> ventas = ObtenerVentasDemo();

            ListadoVentasAdminViewModel modelo = new()
            {
                FiltroEstado = estado,
                Ventas = estado == "Todas" ? ventas : ventas.Where(v => v.Estado == estado).ToList(),
                TotalVendido = ventas.Where(v => v.Estado == "Pagada").Sum(v => v.Total),
                TotalOrdenes = ventas.Count,
                TotalPendientes = ventas.Count(v => v.Estado == "Pendiente"),
                TotalCanceladas = ventas.Count(v => v.Estado == "Cancelada")
            };

            return View(modelo);
        }

        [HttpGet]
        public IActionResult DetalleVenta(int id = 1)
        {
            VentaAdminViewModel modelo =
                ObtenerVentasDemo().FirstOrDefault(v => v.Id == id)
                ?? ObtenerVentasDemo().First();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelarVenta(int id, string motivo)
        {
            TempData["MensajeAdmin"] = $"La orden fue cancelada. Motivo: {motivo}";
            return RedirectToAction(nameof(DetalleVenta), new { id });
        }

        private static List<VentaAdminViewModel> ObtenerVentasDemo()
        {
            return
            [
                new() { Id = 1, NumeroOrden = "ORD-1042", Cliente = "Maria Montero Cruz", Emprendimiento = "Orquídea", Fecha = "02/08/2026", Total = 24000, Estado = "Pagada", MetodoPago = "Sinpe Móvil",
            Items = [ new() { Producto = "Aretes Orquídea", Cantidad = 2, PrecioUnitario = 12000 } ] },
        new() { Id = 2, NumeroOrden = "ORD-1043", Cliente = "Carla Ramírez Solís", Emprendimiento = "Luz Natural", Fecha = "03/08/2026", Total = 13000, Estado = "Pendiente", MetodoPago = "Tarjeta",
            Items = [ new() { Producto = "Vela Aromática Lavanda", Cantidad = 2, PrecioUnitario = 6500 } ] },
        new() { Id = 3, NumeroOrden = "ORD-1044", Cliente = "Luis Fernández Rojas", Emprendimiento = "Artesanías MiVo", Fecha = "01/08/2026", Total = 18500, Estado = "Cancelada", MetodoPago = "Tarjeta",
            Items = [ new() { Producto = "Cuadro decorativo Boho", Cantidad = 1, PrecioUnitario = 18500 } ], MotivoCancelacion = "El cliente solicitó la cancelación por cambio de dirección." },
        new() { Id = 4, NumeroOrden = "ORD-1045", Cliente = "Andrea Castro Vega", Emprendimiento = "Trazo Libre", Fecha = "04/08/2026", Total = 9000, Estado = "Pagada", MetodoPago = "Sinpe Móvil",
            Items = [ new() { Producto = "Ilustración digital personalizada", Cantidad = 1, PrecioUnitario = 9000 } ] }
            ];
        }
        [HttpGet]
        public IActionResult PuntosRecompensas(string pestana = "Recompensas")
        {
            var configuracion = _context.ConfiguracionPuntos
                .OrderByDescending(c => c.IdConfiguracion)
                .FirstOrDefault();

            return View(new PuntosRecompensasAdminViewModel
            {
                Pestana = pestana,
                Recompensas = ObtenerRecompensasDemo(),
                Movimientos = ObtenerMovimientosDemo(),
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearRecompensa(RecompensaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear recompensa";
            if (!ModelState.IsValid) return View("FormularioRecompensa", modelo);
            TempData["MensajeAdmin"] = $"La recompensa \"{modelo.Nombre}\" fue creada correctamente.";
            return RedirectToAction(nameof(PuntosRecompensas));
        }

        [HttpGet]
        public IActionResult EditarRecompensa(int id = 1)
        {
            ViewData["Titulo"] = "Editar recompensa";
            var r = ObtenerRecompensasDemo().FirstOrDefault(x => x.Id == id) ?? new RecompensaAdminViewModel { Id = id };
            return View("FormularioRecompensa", r);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarRecompensa(RecompensaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar recompensa";
            if (!ModelState.IsValid) return View("FormularioRecompensa", modelo);
            TempData["MensajeAdmin"] = $"La recompensa \"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(PuntosRecompensas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AjustarPuntos(AjustarPuntosViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                TempData["MensajeAdmin"] = "Completá correo, puntos y motivo para ajustar puntos.";
                return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
            }

            TempData["MensajeAdmin"] = $"Se ajustaron {modelo.Puntos} puntos para {modelo.Correo}. Motivo: {modelo.Motivo}";
            return RedirectToAction(nameof(PuntosRecompensas), new { pestana = "Movimientos" });
        }

        private static List<RecompensaAdminViewModel> ObtenerRecompensasDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Descuento 10% en catálogo", Descripcion = "Aplica a cualquier producto activo.", CostoPuntos = 500, Activa = true, CantidadCanjes = 34 },
        new() { Id = 2, Nombre = "Entrada gratis a un taller", Descripcion = "Un cupo en cualquier taller con disponibilidad.", CostoPuntos = 800, Activa = true, CantidadCanjes = 12 },
        new() { Id = 3, Nombre = "Kit de bienvenida Club Creativo", Descripcion = "Merchandising físico del club.", CostoPuntos = 1200, Activa = false, CantidadCanjes = 5 }
            ];
        }

        private static List<MovimientoPuntosAdminViewModel> ObtenerMovimientosDemo()
        {
            return
            [
                new() { Usuario = "Maria Montero Cruz", Tipo = "Ganados", Puntos = 50, Motivo = "Compra ORD-1042", Fecha = "02/08/2026" },
        new() { Usuario = "Carla Ramírez Solís", Tipo = "Canjeados", Puntos = -500, Motivo = "Descuento 10% en catálogo", Fecha = "01/08/2026" },
        new() { Usuario = "Luis Fernández Rojas", Tipo = "Ajuste", Puntos = 200, Motivo = "Compensación por soporte", Fecha = "30/07/2026" }
            ];
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
                        QrGenerado = false
                    })
                    .ToListAsync();

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
                QrGenerado = false,
                Asistentes = asistentes
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerarQrAsistencia(int id)
        {
            TempData["MensajeAdmin"] = "El código QR de asistencia fue generado correctamente.";
            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegenerarQrAsistencia(int id)
        {
            TempData["MensajeAdmin"] = "El código QR fue regenerado. El anterior ya no es válido.";
            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }

        [HttpGet]
        public IActionResult DescargarQrAsistencia(int id)
        {
            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarAsistencia(int id, string codigoQr)
        {
            // 1. Comprobar el formato del código QR
            if (!Guid.TryParse(codigoQr, out Guid codigo))
            {
                TempData["ErrorAdmin"] = "El código QR ingresado no es válido.";

                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // 2. Buscar el código QR en la base de datos
            var qrUsuario = await _context.CodigosQrusuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Codigo == codigo && q.Activo);

            // 3. Comprobar que exista y esté activo
            if (qrUsuario == null)
            {
                TempData["ErrorAdmin"] =
                    "El código QR no existe o se encuentra inactivo.";

                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // 4. Comprobar que el usuario esté inscrito al evento
            bool inscripcionConfirmada = await _context.InscripcionesEventos
                .AsNoTracking()
                .AnyAsync(i =>
                    i.IdUsuario == qrUsuario.IdUsuario &&
                    i.IdEvento == id &&
                    i.Estado == "Confirmada");

            if (!inscripcionConfirmada)
            {
                TempData["ErrorAdmin"] =
                    "El usuario no tiene una inscripción confirmada para este evento.";

                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // 5. Comprobar que el evento exista y esté disponible
            var evento = await _context.Eventos
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEvento == id);

            if (evento == null || evento.Estado == "Eliminado")
            {
                TempData["ErrorAdmin"] =
                    "El evento no existe o no está disponible.";

                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // 6. Comprobar que el evento esté dentro de su fecha y horario
            DateTime fechaActual = DateTime.Now;

            if (fechaActual < evento.FechaInicio ||
                (evento.FechaFin.HasValue && fechaActual > evento.FechaFin.Value))
            {
                TempData["ErrorAdmin"] =
                    "No se puede registrar la asistencia porque el evento está fuera de su fecha u horario.";

                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // 7. Comprobar que el usuario no tenga una asistencia registrada
            bool asistenciaExistente = await _context.RegistrosAsistencia
                .AsNoTracking()
                .AnyAsync(a =>
                    a.IdUsuario == qrUsuario.IdUsuario &&
                    a.IdEvento == id);

            if (asistenciaExistente)
            {
                TempData["ErrorAdmin"] =
                    "Este usuario ya tiene una asistencia registrada para este evento.";

                return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });
            }

            // 8. Preparar el registro de asistencia del usuario
            var nuevaAsistencia = new RegistrosAsistencium
            {
                IdUsuario = qrUsuario.IdUsuario,
                IdEvento = id,
                IdTaller = null,
                TipoAsistencia = "Evento",
                FechaHora = fechaActual,
                RegistradoPorQr = true,
                Observacion = "Asistencia validada mediante código QR."
            };

            // 9. Preparar los 10 puntos por asistencia al evento
            var nuevoMovimiento = new MovimientosPunto
            {
                IdUsuario = qrUsuario.IdUsuario,
                TipoMovimiento = "Acumulacion",
                Puntos = 10,
                Motivo = "Asistencia al evento: " + evento.Nombre,
                IdVenta = null,
                FechaMovimiento = fechaActual
            };

            // 10. Guardar la asistencia y los puntos en una misma transacción
            await using var transaccion = await _context.Database.BeginTransactionAsync();

            try
            {
                // Guardar primero la asistencia
                _context.RegistrosAsistencia.Add(nuevaAsistencia);
                await _context.SaveChangesAsync();

                // Relacionar los puntos con la asistencia registrada
                nuevoMovimiento.IdAsistencia = nuevaAsistencia.IdAsistencia;

                // Guardar los 10 puntos
                _context.Set<MovimientosPunto>().Add(nuevoMovimiento);
                await _context.SaveChangesAsync();

                // Confirmar ambas operaciones
                await transaccion.CommitAsync();

                TempData["MensajeAdmin"] =
                    "Asistencia registrada correctamente. Se otorgaron 10 puntos al usuario.";
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();

                TempData["ErrorAdmin"] =
                    "No se pudo registrar la asistencia. Es posible que el usuario ya tenga una asistencia registrada para este evento.";
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();

                TempData["ErrorAdmin"] =
                    "Ocurrió un error al registrar la asistencia y los puntos.";
            }

            return RedirectToAction(nameof(DetalleAsistenciaQr), new { id });

        }

        [HttpGet]
        public IActionResult Notificaciones(string pestana = "Enviar")
        {
            return View(new NotificacionesAdminViewModel
            {
                Pestana = pestana,
                Historial = ObtenerNotificacionesDemo()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnviarNotificacion(NotificacionFormViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Notificaciones), new NotificacionesAdminViewModel
                {
                    Pestana = "Enviar",
                    Nueva = modelo,
                    Historial = ObtenerNotificacionesDemo()
                });
            }

            string destinatario = modelo.TipoDestinatario switch
            {
                "Individual" => $"Usuario: {modelo.UsuarioDestino}",
                "Grupo" => $"Grupo: {modelo.GrupoDestino}",
                _ => "Todos los usuarios"
            };

            TempData["MensajeAdmin"] = $"La notificación \"{modelo.Titulo}\" fue enviada a {destinatario}.";
            return RedirectToAction(nameof(Notificaciones), new { pestana = "Historial" });
        }

        private static List<NotificacionEnviadaViewModel> ObtenerNotificacionesDemo()
        {
            return
            [
                new() { Id = 1, Titulo = "Nueva feria en San Pedro", Mensaje = "Inscribite antes del 10 de agosto.", Destinatario = "Todos los usuarios", Fecha = "04/08/2026", TotalDestinatarios = 486 },
                new() { Id = 2, Titulo = "Recordatorio de suscripción", Mensaje = "Tu plan vence pronto, renová para seguir disfrutando de tus beneficios.", Destinatario = "Grupo: Emprendedores", Fecha = "02/08/2026", TotalDestinatarios = 57 },
                new() { Id = 3, Titulo = "Bienvenida al club", Mensaje = "Gracias por unirte a Club Creativo.", Destinatario = "Usuario: maria@ejemplo.com", Fecha = "30/07/2026", TotalDestinatarios = 1 }
            ];
        }

        [HttpGet]
        public IActionResult Galerias()
        {
            return View(ObtenerGaleriasDemo());
        }

        [HttpGet]
        public IActionResult CrearGaleria()
        {
            ViewData["Titulo"] = "Crear galería";
            return View("FormularioGaleria", new GaleriaAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearGaleria(GaleriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear galería";
            if (!ModelState.IsValid) return View("FormularioGaleria", modelo);
            TempData["MensajeAdmin"] = $"La galería \"{modelo.Nombre}\" fue creada correctamente.";
            return RedirectToAction(nameof(Galerias));
        }

        [HttpGet]
        public IActionResult EditarGaleria(int id = 1)
        {
            ViewData["Titulo"] = "Editar galería";
            var galeria = ObtenerGaleriasDemo().FirstOrDefault(g => g.Id == id) ?? new GaleriaAdminViewModel { Id = id };
            return View("FormularioGaleria", galeria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarGaleria(GaleriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar galería";
            if (!ModelState.IsValid) return View("FormularioGaleria", modelo);
            TempData["MensajeAdmin"] = $"La galería \"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Galerias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargarImagenesGaleria(int id, List<IFormFile> imagenes)
        {
            if (imagenes is { Count: > 0 })
            {
                string carpeta = Path.Combine(_entornoWeb.WebRootPath, "uploads", "galerias");
                Directory.CreateDirectory(carpeta);

                foreach (var archivo in imagenes)
                {
                    if (archivo.Length <= 0) continue;
                    string nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                    string rutaCompleta = Path.Combine(carpeta, nombreArchivo);
                    using var flujo = new FileStream(rutaCompleta, FileMode.Create);
                    await archivo.CopyToAsync(flujo);
                }

                TempData["MensajeAdmin"] = $"Se cargaron {imagenes.Count} imagen(es) a la galería correctamente.";
            }
            else
            {
                TempData["MensajeAdmin"] = "Seleccioná al menos una imagen para cargar.";
            }

            return RedirectToAction(nameof(EditarGaleria), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarImagenGaleria(int id, int imagenId)
        {
            TempData["MensajeAdmin"] = "La imagen fue eliminada de la galería.";
            return RedirectToAction(nameof(EditarGaleria), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarGaleria(int id)
        {
            TempData["MensajeAdmin"] = "La galería fue eliminada correctamente.";
            return RedirectToAction(nameof(Galerias));
        }

        private static List<GaleriaAdminViewModel> ObtenerGaleriasDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Arte Inarrivo San Pedro 2026", Descripcion = "Fotografías del evento realizado en San Pedro.", Categoria = "Eventos", Estado = "Publicada", Portada = "/images/logo.jpg", FechaCreacion = "16/08/2026",
                    Imagenes = [ new() { Id = 1, Url = "/images/logo.jpg", NombreArchivo = "portada.jpg" }, new() { Id = 2, Url = "/images/logo.jpg", NombreArchivo = "stand-1.jpg" } ] },
                new() { Id = 2, Nombre = "Taller de cerámica", Descripcion = "Registro fotográfico del taller de cerámica para principiantes.", Categoria = "Talleres", Estado = "Publicada", Portada = "/images/logo.jpg", FechaCreacion = "24/08/2026", Imagenes = [] },
                new() { Id = 3, Nombre = "Emprendimientos destacados", Descripcion = "Galería con productos de emprendimientos aliados.", Categoria = "Emprendimientos", Estado = "Oculta", Portada = "/images/logo.jpg", FechaCreacion = "10/07/2026", Imagenes = [] }
            ];
        }

        [HttpGet]
        public IActionResult Noticias(string estado = "Todas")
        {
            var noticias = ObtenerNoticiasDemo();
            return View(new ListadoNoticiasAdminViewModel
            {
                FiltroEstado = estado,
                Noticias = estado == "Todas" ? noticias : noticias.Where(n => n.Estado == estado).ToList()
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
        public IActionResult CrearNoticia(NoticiaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear publicación";
            if (!ModelState.IsValid) return View("FormularioNoticia", modelo);
            TempData["MensajeAdmin"] = $"La publicación \"{modelo.Titulo}\" fue creada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpGet]
        public IActionResult EditarNoticia(int id = 1)
        {
            ViewData["Titulo"] = "Editar publicación";
            var noticia = ObtenerNoticiasDemo().FirstOrDefault(n => n.Id == id) ?? new NoticiaAdminViewModel { Id = id };
            return View("FormularioNoticia", noticia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarNoticia(NoticiaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar publicación";
            if (!ModelState.IsValid) return View("FormularioNoticia", modelo);
            TempData["MensajeAdmin"] = $"La publicación \"{modelo.Titulo}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PublicarNoticia(int id)
        {
            TempData["MensajeAdmin"] = "La publicación fue publicada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DesactivarNoticia(int id)
        {
            TempData["MensajeAdmin"] = "La publicación fue desactivada.";
            return RedirectToAction(nameof(Noticias));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarNoticia(int id)
        {
            TempData["MensajeAdmin"] = "La publicación fue eliminada correctamente.";
            return RedirectToAction(nameof(Noticias));
        }

        private static List<NoticiaAdminViewModel> ObtenerNoticiasDemo()
        {
            return
            [
                new() { Id = 1, Titulo = "Nueva alianza con artesanos locales", Contenido = "Club Creativo firma una alianza para fortalecer el catálogo de productos artesanales.", Categoria = "Noticia", Estado = "Publicada", Autor = "Jossete Sánchez", FechaPublicacion = "01/08/2026" },
                new() { Id = 2, Titulo = "Cambios en el horario de atención", Contenido = "A partir de setiembre el horario de atención se extiende hasta las 7:00 p. m.", Categoria = "Anuncio", Estado = "Publicada", Autor = "Jossete Sánchez", FechaPublicacion = "28/07/2026" },
                new() { Id = 3, Titulo = "Próxima feria en Heredia", Contenido = "Se viene una nueva edición de la Feria Creativa en Heredia este 20 de setiembre.", Categoria = "Anuncio", Estado = "Borrador", Autor = "Jossete Sánchez", FechaPublicacion = "" },
                new() { Id = 4, Titulo = "Resultados de la encuesta 2025", Contenido = "Compartimos los resultados de la última encuesta de satisfacción de los usuarios.", Categoria = "Noticia", Estado = "Inactiva", Autor = "Jossete Sánchez", FechaPublicacion = "15/03/2026" }
            ];
        }

        [HttpGet]
        public IActionResult Reportes(FiltroReporteViewModel filtro)
        {
            filtro ??= new FiltroReporteViewModel();
            return View(ConstruirReporte(filtro));
        }

        [HttpGet]
        public IActionResult ExportarReporte(FiltroReporteViewModel filtro)
        {
            ReporteAdminViewModel modelo = ConstruirReporte(filtro ?? new FiltroReporteViewModel());

            var constructor = new System.Text.StringBuilder();
            constructor.AppendLine(string.Join(",", modelo.Columnas));
            foreach (var fila in modelo.Filas)
            {
                constructor.AppendLine(string.Join(",", fila.Columna1, fila.Columna2, fila.Columna3, fila.Columna4));
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(constructor.ToString());
            string nombreArchivo = $"reporte-{modelo.Filtro.TipoReporte.ToLower()}.csv";
            return File(bytes, "text/csv", nombreArchivo);
        }

        private static ReporteAdminViewModel ConstruirReporte(FiltroReporteViewModel filtro)
        {
            (List<PuntoGraficoViewModel> barras, List<PuntoGraficoViewModel> dona, string[] columnas, List<FilaReporteViewModel> filas, string titulo) = filtro.TipoReporte switch
            {
                "Usuarios" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 40 }, new() { Etiqueta = "Abr", Valor = 55 }, new() { Etiqueta = "May", Valor = 48 }, new() { Etiqueta = "Jun", Valor = 63 }, new() { Etiqueta = "Jul", Valor = 70 }, new() { Etiqueta = "Ago", Valor = 82 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Usuario", Valor = 340 }, new() { Etiqueta = "Emprendedor", Valor = 120 }, new() { Etiqueta = "Administrador", Valor = 6 } },
                    new[] { "Usuario", "Rol", "Registro", "Estado" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Maria Montero Cruz", Columna2 = "Usuario", Columna3 = "12/01/2026", Columna4 = "Activo" },
                        new() { Columna1 = "Valentín Arce Mora", Columna2 = "Emprendedor", Columna3 = "20/05/2026", Columna4 = "Activo" }
                    },
                    "Reporte de usuarios"
                ),
                "Emprendimientos" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 4 }, new() { Etiqueta = "Abr", Valor = 6 }, new() { Etiqueta = "May", Valor = 5 }, new() { Etiqueta = "Jun", Valor = 9 }, new() { Etiqueta = "Jul", Valor = 8 }, new() { Etiqueta = "Ago", Valor = 12 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Aprobados", Valor = 42 }, new() { Etiqueta = "Pendientes", Valor = 8 }, new() { Etiqueta = "Rechazados", Valor = 7 } },
                    new[] { "Emprendimiento", "Categoría", "Solicitud", "Estado" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Artesanías MiVo", Columna2 = "Arte e ilustración", Columna3 = "01/08/2026", Columna4 = "Pendiente" },
                        new() { Columna1 = "Orquídea", Columna2 = "Accesorios", Columna3 = "10/07/2026", Columna4 = "Aprobado" }
                    },
                    "Reporte de emprendimientos"
                ),
                "Productos" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 30 }, new() { Etiqueta = "Abr", Valor = 42 }, new() { Etiqueta = "May", Valor = 38 }, new() { Etiqueta = "Jun", Valor = 55 }, new() { Etiqueta = "Jul", Valor = 60 }, new() { Etiqueta = "Ago", Valor = 72 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Arte e ilustración", Valor = 34 }, new() { Etiqueta = "Accesorios", Valor = 26 }, new() { Etiqueta = "Hogar y decoración", Valor = 20 } },
                    new[] { "Producto", "Emprendimiento", "Categoría", "Estado" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Aretes Orquídea", Columna2 = "Orquídea", Columna3 = "Accesorios", Columna4 = "Activo" },
                        new() { Columna1 = "Vela Aromática Lavanda", Columna2 = "Luz Natural", Columna3 = "Velas y aromas", Columna4 = "Activo" }
                    },
                    "Reporte de productos"
                ),
                "Eventos" => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 1 }, new() { Etiqueta = "Abr", Valor = 2 }, new() { Etiqueta = "May", Valor = 1 }, new() { Etiqueta = "Jun", Valor = 3 }, new() { Etiqueta = "Jul", Valor = 2 }, new() { Etiqueta = "Ago", Valor = 3 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Programados", Valor = 2 }, new() { Etiqueta = "Finalizados", Valor = 6 }, new() { Etiqueta = "Cancelados", Valor = 1 } },
                    new[] { "Evento", "Ubicación", "Fecha", "Inscritos" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "Arte Inarrivo San Pedro", Columna2 = "San Pedro", Columna3 = "15 y 16 de agosto", Columna4 = "120" },
                        new() { Columna1 = "Feria Creativa Santa Ana", Columna2 = "Santa Ana", Columna3 = "5 al 7 de setiembre", Columna4 = "86" }
                    },
                    "Reporte de eventos"
                ),
                _ => (
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Mar", Valor = 45 }, new() { Etiqueta = "Abr", Valor = 58 }, new() { Etiqueta = "May", Valor = 50 }, new() { Etiqueta = "Jun", Valor = 72 }, new() { Etiqueta = "Jul", Valor = 65 }, new() { Etiqueta = "Ago", Valor = 90 } },
                    new List<PuntoGraficoViewModel> { new() { Etiqueta = "Pagadas", Valor = 58 }, new() { Etiqueta = "Pendientes", Valor = 12 }, new() { Etiqueta = "Canceladas", Valor = 6 } },
                    new[] { "Orden", "Cliente", "Fecha", "Total" },
                    new List<FilaReporteViewModel> {
                        new() { Columna1 = "ORD-1042", Columna2 = "Maria Montero Cruz", Columna3 = "02/08/2026", Columna4 = "₡24 000" },
                        new() { Columna1 = "ORD-1043", Columna2 = "Carla Ramírez Solís", Columna3 = "03/08/2026", Columna4 = "₡13 000" }
                    },
                    "Reporte de ventas"
                )
            };

            return new ReporteAdminViewModel
            {
                Filtro = filtro,
                GraficoBarras = barras,
                GraficoDona = dona,
                Columnas = columnas.ToList(),
                Filas = filas,
                Titulo = titulo
            };
        }

        [HttpGet]
        public IActionResult Comentarios(string? busqueda, string estado = "Todos", string origen = "Todos")
        {
            var comentarios = ObtenerComentariosDemo();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                comentarios = comentarios
                    .Where(c => c.Contenido.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                                c.Autor.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (estado != "Todos") comentarios = comentarios.Where(c => c.Estado == estado).ToList();
            if (origen != "Todos") comentarios = comentarios.Where(c => c.Origen == origen).ToList();

            return View(new ListadoComentariosAdminViewModel
            {
                Busqueda = busqueda,
                FiltroEstado = estado,
                FiltroOrigen = origen,
                Comentarios = comentarios
            });
        }

        [HttpGet]
        public IActionResult VerComentario(int id = 1)
        {
            var comentario = ObtenerComentariosDemo().FirstOrDefault(c => c.Id == id) ?? ObtenerComentariosDemo().First();
            return View(comentario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarComentario(int id)
        {
            TempData["MensajeAdmin"] = "El comentario fue eliminado por contenido inapropiado.";
            return RedirectToAction(nameof(Comentarios));
        }

        private static List<ComentarioAdminViewModel> ObtenerComentariosDemo()
        {
            return
            [
                new() { Id = 1, Autor = "Maria Montero Cruz", Contenido = "¡Excelente producto, llegó muy rápido y en perfecto estado!", Origen = "Producto", ElementoRelacionado = "Aretes Orquídea", Fecha = "03/08/2026", Estado = "Visible", Reportes = 0 },
                new() { Id = 2, Autor = "Usuario anónimo", Contenido = "Contenido inapropiado reportado por varios usuarios de la comunidad.", Origen = "Noticia", ElementoRelacionado = "Cambios en el horario de atención", Fecha = "02/08/2026", Estado = "Reportado", Reportes = 4 },
                new() { Id = 3, Autor = "Luis Fernández Rojas", Contenido = "Muy buen taller, aprendí bastante sobre cerámica.", Origen = "Taller", ElementoRelacionado = "Cerámica para principiantes", Fecha = "24/08/2026", Estado = "Visible", Reportes = 0 },
                new() { Id = 4, Autor = "Carla Ramírez Solís", Contenido = "El evento estuvo muy desorganizado, no lo recomiendo para nada.", Origen = "Evento", ElementoRelacionado = "Feria Creativa Santa Ana", Fecha = "07/09/2026", Estado = "Reportado", Reportes = 2 }
            ];
        }

        [HttpGet]
        public IActionResult Categorias(string tipo = "Categoria")
        {
            var categorias = ObtenerCategoriasDemo();
            return View(new ListadoCategoriasAdminViewModel
            {
                FiltroTipo = tipo,
                Categorias = categorias.Where(c => c.Tipo == tipo).ToList()
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
        public IActionResult CrearCategoria(CategoriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = modelo.Tipo == "Etiqueta" ? "Crear etiqueta" : "Crear categoría";
            if (!ModelState.IsValid) return View("FormularioCategoria", modelo);
            TempData["MensajeAdmin"] = $"\"{modelo.Nombre}\" fue creada correctamente.";
            return RedirectToAction(nameof(Categorias), new { tipo = modelo.Tipo });
        }

        [HttpGet]
        public IActionResult EditarCategoria(int id = 1)
        {
            var categoria = ObtenerCategoriasDemo().FirstOrDefault(c => c.Id == id) ?? new CategoriaAdminViewModel { Id = id };
            ViewData["Titulo"] = categoria.Tipo == "Etiqueta" ? "Editar etiqueta" : "Editar categoría";
            return View("FormularioCategoria", categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarCategoria(CategoriaAdminViewModel modelo)
        {
            ViewData["Titulo"] = modelo.Tipo == "Etiqueta" ? "Editar etiqueta" : "Editar categoría";
            if (!ModelState.IsValid) return View("FormularioCategoria", modelo);
            TempData["MensajeAdmin"] = $"\"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Categorias), new { tipo = modelo.Tipo });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarCategoria(int id, string tipo = "Categoria")
        {
            TempData["MensajeAdmin"] = "El elemento fue eliminado correctamente.";
            return RedirectToAction(nameof(Categorias), new { tipo });
        }

        [HttpGet]
        public IActionResult ElementosCategoria(int id = 1)
        {
            var categoria = ObtenerCategoriasDemo().FirstOrDefault(c => c.Id == id) ?? ObtenerCategoriasDemo().First();
            return View(new ElementosCategoriaViewModel
            {
                Categoria = categoria,
                Elementos = ObtenerElementosAsociadosDemo(categoria.Modulo)
            });
        }

        private static List<CategoriaAdminViewModel> ObtenerCategoriasDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Arte e ilustración", Descripcion = "Productos relacionados con arte e ilustraciones.", Tipo = "Categoria", Modulo = "Productos", ElementosAsociados = 34 },
                new() { Id = 2, Nombre = "Accesorios", Descripcion = "Aretes, collares y accesorios artesanales.", Tipo = "Categoria", Modulo = "Productos", ElementosAsociados = 26 },
                new() { Id = 3, Nombre = "Hogar y decoración", Descripcion = "Artículos decorativos hechos a mano.", Tipo = "Categoria", Modulo = "Productos", ElementosAsociados = 20 },
                new() { Id = 4, Nombre = "Hecho a mano", Descripcion = "Etiqueta para productos artesanales.", Tipo = "Etiqueta", Modulo = "Productos", ElementosAsociados = 58 },
                new() { Id = 5, Nombre = "Nuevo", Descripcion = "Etiqueta para publicaciones recientes.", Tipo = "Etiqueta", Modulo = "Noticias", ElementosAsociados = 12 }
            ];
        }

        private static List<ElementoAsociadoViewModel> ObtenerElementosAsociadosDemo(string modulo)
        {
            return modulo switch
            {
                "Noticias" =>
                [
                    new() { Nombre = "Nueva alianza con artesanos locales", Tipo = "Noticia", Estado = "Publicada" },
                    new() { Nombre = "Próxima feria en Heredia", Tipo = "Anuncio", Estado = "Borrador" }
                ],
                _ =>
                [
                    new() { Nombre = "Aretes Orquídea", Tipo = "Producto", Estado = "Activo" },
                    new() { Nombre = "Vela Aromática Lavanda", Tipo = "Producto", Estado = "Activo" },
                    new() { Nombre = "Cuadro decorativo Boho", Tipo = "Producto", Estado = "Pausado" }
                ]
            };
        }

        [HttpGet]
        public IActionResult Promociones(string estado = "Todas")
        {
            var promociones = ObtenerPromocionesDemo();
            return View(new ListadoPromocionesAdminViewModel
            {
                FiltroEstado = estado,
                Promociones = estado == "Todas" ? promociones : promociones.Where(p => p.Estado == estado).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearPromocion()
        {
            ViewData["Titulo"] = "Crear promoción";
            return View("FormularioPromocion", new PromocionAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearPromocion(PromocionAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear promoción";
            if (!ModelState.IsValid) return View("FormularioPromocion", modelo);
            TempData["MensajeAdmin"] = $"La promoción \"{modelo.Nombre}\" fue programada del {modelo.FechaInicio:dd/MM/yyyy} al {modelo.FechaFin:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Promociones));
        }

        [HttpGet]
        public IActionResult EditarPromocion(int id = 1)
        {
            ViewData["Titulo"] = "Editar promoción";
            var promocion = ObtenerPromocionesDemo().FirstOrDefault(p => p.Id == id) ?? new PromocionAdminViewModel { Id = id };
            return View("FormularioPromocion", promocion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarPromocion(PromocionAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar promoción";
            if (!ModelState.IsValid) return View("FormularioPromocion", modelo);
            TempData["MensajeAdmin"] = $"La promoción \"{modelo.Nombre}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Promociones));
        }

        [HttpGet]
        public IActionResult DetallePromocion(int id = 1)
        {
            var promocion = ObtenerPromocionesDemo().FirstOrDefault(p => p.Id == id) ?? ObtenerPromocionesDemo().First();
            return View(promocion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DesactivarPromocion(int id)
        {
            TempData["MensajeAdmin"] = "La promoción fue desactivada.";
            return RedirectToAction(nameof(Promociones));
        }

        private static List<PromocionAdminViewModel> ObtenerPromocionesDemo()
        {
            return
            [
                new() { Id = 1, Nombre = "Descuento Feria San Pedro", Descripcion = "15% de descuento en productos participantes de la feria.", TipoDescuento = "Porcentaje", ValorDescuento = 15, FechaInicio = new DateTime(2026, 8, 10), FechaFin = new DateTime(2026, 8, 16), Estado = "Activa", Emprendimiento = "Todos", Usos = 48 },
                new() { Id = 2, Nombre = "Envío gratis primera compra", Descripcion = "Envío sin costo para nuevos usuarios del club.", TipoDescuento = "Monto fijo", ValorDescuento = 2500, FechaInicio = new DateTime(2026, 9, 1), FechaFin = new DateTime(2026, 9, 30), Estado = "Programada", Emprendimiento = "Todos", Usos = 0 },
                new() { Id = 3, Nombre = "Aniversario Orquídea", Descripcion = "20% de descuento en toda la tienda Orquídea.", TipoDescuento = "Porcentaje", ValorDescuento = 20, FechaInicio = new DateTime(2026, 6, 1), FechaFin = new DateTime(2026, 6, 15), Estado = "Finalizada", Emprendimiento = "Orquídea", Usos = 76 },
                new() { Id = 4, Nombre = "Descuento talleres agosto", Descripcion = "10% de descuento en inscripciones a talleres.", TipoDescuento = "Porcentaje", ValorDescuento = 10, FechaInicio = new DateTime(2026, 8, 1), FechaFin = new DateTime(2026, 8, 31), Estado = "Desactivada", Emprendimiento = "Todos", Usos = 5 }
            ];
        }

        [HttpGet]
        public IActionResult Encuestas(string estado = "Todas")
        {
            var encuestas = ObtenerEncuestasDemo();
            return View(new ListadoEncuestasAdminViewModel
            {
                FiltroEstado = estado,
                Encuestas = estado == "Todas" ? encuestas : encuestas.Where(e => e.Estado == estado).ToList()
            });
        }

        [HttpGet]
        public IActionResult CrearEncuesta()
        {
            ViewData["Titulo"] = "Crear encuesta";
            return View("FormularioEncuesta", new EncuestaAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearEncuesta(EncuestaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Crear encuesta";
            if (!ModelState.IsValid) return View("FormularioEncuesta", modelo);
            int totalPreguntas = modelo.PreguntasTexto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
            TempData["MensajeAdmin"] = $"La encuesta \"{modelo.Titulo}\" fue creada con {totalPreguntas} pregunta(s).";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpGet]
        public IActionResult EditarEncuesta(int id = 1)
        {
            ViewData["Titulo"] = "Editar encuesta";
            var encuesta = ObtenerEncuestasDemo().FirstOrDefault(e => e.Id == id) ?? new EncuestaAdminViewModel { Id = id };
            return View("FormularioEncuesta", encuesta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarEncuesta(EncuestaAdminViewModel modelo)
        {
            ViewData["Titulo"] = "Editar encuesta";
            if (!ModelState.IsValid) return View("FormularioEncuesta", modelo);
            TempData["MensajeAdmin"] = $"La encuesta \"{modelo.Titulo}\" fue actualizada correctamente.";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PublicarEncuesta(int id)
        {
            TempData["MensajeAdmin"] = "La encuesta fue publicada correctamente.";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarEncuesta(int id)
        {
            TempData["MensajeAdmin"] = "La encuesta fue cerrada. Ya no se aceptan más respuestas.";
            return RedirectToAction(nameof(Encuestas));
        }

        [HttpGet]
        public IActionResult ResultadosEncuesta(int id = 1)
        {
            var encuesta = ObtenerEncuestasDemo().FirstOrDefault(e => e.Id == id) ?? ObtenerEncuestasDemo().First();
            return View(encuesta);
        }

        private static List<EncuestaAdminViewModel> ObtenerEncuestasDemo()
        {
            return
            [
                new() { Id = 1, Titulo = "Satisfacción con la feria de agosto", Descripcion = "Ayudanos a mejorar futuras ediciones del evento.", Estado = "Publicada", FechaCreacion = "16/08/2026", TotalRespuestas = 128,
                    PreguntasTexto = "¿Cómo calificarías la organización del evento?\n¿Qué tan probable es que asistas a la próxima edición?",
                    Preguntas =
                    [
                        new() { Texto = "¿Cómo calificarías la organización del evento?", Opciones = [ new() { Texto = "Excelente", Votos = 62 }, new() { Texto = "Buena", Votos = 48 }, new() { Texto = "Regular", Votos = 14 }, new() { Texto = "Mala", Votos = 4 } ] },
                        new() { Texto = "¿Qué tan probable es que asistas a la próxima edición?", Opciones = [ new() { Texto = "Muy probable", Votos = 80 }, new() { Texto = "Probable", Votos = 35 }, new() { Texto = "Poco probable", Votos = 13 } ] }
                    ] },
                new() { Id = 2, Titulo = "Nuevas categorías de productos", Descripcion = "Consulta sobre categorías de interés para el catálogo.", Estado = "Borrador", FechaCreacion = "01/08/2026", TotalRespuestas = 0,
                    PreguntasTexto = "¿Qué categoría te gustaría ver en el catálogo?",
                    Preguntas = [ new() { Texto = "¿Qué categoría te gustaría ver en el catálogo?", Opciones = [ new() { Texto = "Ropa artesanal", Votos = 0 }, new() { Texto = "Juguetes de madera", Votos = 0 } ] } ] },
                new() { Id = 3, Titulo = "Evaluación del taller de cerámica", Descripcion = "Retroalimentación de los participantes del taller.", Estado = "Cerrada", FechaCreacion = "10/06/2026", TotalRespuestas = 45,
                    PreguntasTexto = "¿Recomendarías este taller a otras personas?",
                    Preguntas = [ new() { Texto = "¿Recomendarías este taller a otras personas?", Opciones = [ new() { Texto = "Sí", Votos = 41 }, new() { Texto = "No", Votos = 4 } ] } ] }
            ];
        }

        [HttpGet]
        public IActionResult Bitacora(FiltroBitacoraViewModel filtro)
        {
            filtro ??= new FiltroBitacoraViewModel();
            var movimientos = ObtenerMovimientosBitacoraDemo();

            if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            {
                movimientos = movimientos.Where(m => m.Usuario.Contains(filtro.Usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filtro.Accion) && filtro.Accion != "Todas")
            {
                movimientos = movimientos.Where(m => m.Accion == filtro.Accion).ToList();
            }

            if (filtro.FechaInicio is not null)
            {
                movimientos = movimientos.Where(m =>
                    DateTime.TryParseExact(m.Fecha.Split(' ')[0], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha) &&
                    fecha >= filtro.FechaInicio).ToList();
            }

            if (filtro.FechaFin is not null)
            {
                movimientos = movimientos.Where(m =>
                    DateTime.TryParseExact(m.Fecha.Split(' ')[0], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha) &&
                    fecha <= filtro.FechaFin).ToList();
            }

            return View(new BitacoraAdminViewModel
            {
                Filtro = filtro,
                Movimientos = movimientos,
                Alertas = ObtenerAlertasSeguridadDemo(),
                IntentosFallidos = ObtenerIntentosFallidosDemo()
            });
        }

        [HttpGet]
        public IActionResult DetalleMovimiento(int id = 1)
        {
            var movimiento = ObtenerMovimientosBitacoraDemo().FirstOrDefault(m => m.Id == id) ?? ObtenerMovimientosBitacoraDemo().First();
            return View(movimiento);
        }

        private static List<MovimientoBitacoraViewModel> ObtenerMovimientosBitacoraDemo()
        {
            return
            [
                new() { Id = 1, Usuario = "Jossete Sánchez", Accion = "Inicio de sesión", Modulo = "Acceso", Fecha = "05/08/2026 08:15", DireccionIp = "190.113.20.4", Detalle = "Inicio de sesión exitoso desde el panel de administración.", Nivel = "informativa" },
                new() { Id = 2, Usuario = "Jossete Sánchez", Accion = "Aprobación", Modulo = "Emprendimientos", Fecha = "05/08/2026 09:02", DireccionIp = "190.113.20.4", Detalle = "Se aprobó la solicitud de emprendimiento \"Luz Natural\".", Nivel = "informativa" },
                new() { Id = 3, Usuario = "Carla Ramírez Solís", Accion = "Eliminación", Modulo = "Comentarios", Fecha = "05/08/2026 10:40", DireccionIp = "201.203.14.88", Detalle = "Se eliminó un comentario reportado por contenido inapropiado.", Nivel = "advertencia" },
                new() { Id = 4, Usuario = "Desconocido", Accion = "Intento fallido", Modulo = "Acceso", Fecha = "04/08/2026 22:18", DireccionIp = "45.66.12.9", Detalle = "Múltiples intentos fallidos de inicio de sesión para la cuenta admin@clubcreativomivo.com.", Nivel = "critica" },
                new() { Id = 5, Usuario = "Jossete Sánchez", Accion = "Edición", Modulo = "Promociones", Fecha = "04/08/2026 15:22", DireccionIp = "190.113.20.4", Detalle = "Se editaron las fechas de la promoción \"Descuento Feria San Pedro\".", Nivel = "informativa" }
            ];
        }

        private static List<AlertaAdminViewModel> ObtenerAlertasSeguridadDemo()
        {
            return
            [
                new() { Tipo = "Seguridad", Mensaje = "Se detectaron 5 intentos fallidos de inicio de sesión desde una misma dirección IP.", Fecha = "Hoy", Icono = "bi-shield-exclamation", Nivel = "critica" },
                new() { Tipo = "Cuentas", Mensaje = "La cuenta admin@clubcreativomivo.com fue bloqueada temporalmente.", Fecha = "Hoy", Icono = "bi-lock-fill", Nivel = "advertencia" }
            ];
        }

        private static List<IntentoFallidoViewModel> ObtenerIntentosFallidosDemo()
        {
            return
            [
                new() { Correo = "admin@clubcreativomivo.com", DireccionIp = "45.66.12.9", Fecha = "04/08/2026 22:18", Intentos = 5, Bloqueado = true },
                new() { Correo = "carla@ejemplo.com", DireccionIp = "201.203.14.88", Fecha = "03/08/2026 19:40", Intentos = 2, Bloqueado = false }
            ];
        }
    }
}
