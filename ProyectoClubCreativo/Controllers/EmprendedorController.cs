using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;
using ProyectoClubCreativo.Models.ViewModels;
using ProyectoClubCreativo.Services;
using System.Globalization;

namespace ProyectoClubCreativo.Controllers
{
    public class EmprendedorController : Controller
    {
        private readonly ClubCreativoDbContext _context;
        private readonly IWebHostEnvironment _entornoWeb;
        private readonly PagoService _pagoService;
        private readonly CorreoService _correoService;
        private readonly NotificacionService _notificaciones;
        private readonly ActualizacionSuscripcionesService _actualizacionSuscripciones;
        private readonly AuditoriaService _auditoria;

        public EmprendedorController(
            ClubCreativoDbContext context,
            IWebHostEnvironment entornoWeb,
            PagoService pagoService,
            CorreoService correoService,
            NotificacionService notificaciones,
           ActualizacionSuscripcionesService actualizacionSuscripciones,
            AuditoriaService auditoria)
        {
            _auditoria = auditoria;
            _context = context;
            _entornoWeb = entornoWeb;
            _pagoService = pagoService;
            _correoService = correoService;
            _notificaciones = notificaciones;
            _actualizacionSuscripciones = actualizacionSuscripciones;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
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

            if (rolUsuario != "Emprendedor")
            {
                string nombreCuenta = HttpContext.Session.GetString("NombreUsuario") ?? "Una cuenta";
                string rutaCuenta = $"{context.RouteData.Values["controller"]}/{context.RouteData.Values["action"]}";

                try
                {
                    await _auditoria.RegistrarAlertaAsync(
                        "AccesoNoAutorizado",
                        $"{nombreCuenta} (rol {rolUsuario ?? "sin rol"}) intentó acceder a la sección restringida {rutaCuenta}.",
                        "Advertencia");
                }
                catch (Exception)
                {
                }

                context.Result = RedirectToAction(
                    "AccesoDenegado",
                    "Cuenta"
                );

                return;
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            ViewBag.EstadoEmprendimiento =
                emprendimiento?.EstadoAprobacion ?? "Pendiente";
            ViewBag.TieneEmprendimiento = emprendimiento is not null;

            // Cantidad de productos que necesitan atención
            int productosConAlerta = 0;

            if (emprendimiento is not null)
            {
                productosConAlerta = await _context.Productos
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        p.TipoPublicacion == "Producto" &&
                        p.Estado == "Publicado" &&
                        p.StockActual <= 3);
            }

            ViewBag.ProductosConAlertaInventario = productosConAlerta;
            ViewBag.ProductosConAlertaInventario = productosConAlerta;

            ActionExecutedContext ejecutado = await next();

            if (ejecutado.Exception is not null ||
                !HttpMethods.IsPost(Request.Method) ||
                ejecutado.Result is not (RedirectToActionResult or RedirectResult) ||
                TempData.Peek("MensajeError") is not null)
            {
                return;
            }

            string accion = context.ActionDescriptor.RouteValues["action"] ?? "Desconocida";

            string? idRegistro = context.RouteData.Values["id"]?.ToString();

            if (string.IsNullOrEmpty(idRegistro) && Request.HasFormContentType)
            {
                idRegistro = Request.Form["id"].FirstOrDefault();
            }

            string nombre = HttpContext.Session.GetString("NombreUsuario") ?? "El emprendedor";

            string descripcion = string.IsNullOrEmpty(idRegistro)
                ? $"{nombre} ejecutó la acción {accion}."
                : $"{nombre} ejecutó la acción {accion} sobre el registro {idRegistro}.";

            try
            {
                await _auditoria.RegistrarAsync(
                    "Emprendedor",
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

        public async Task<IActionResult> Panel()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                ViewBag.PlanSuscripcion = "Sin suscripción";
                return View();
            }

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .Where(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa")
                    .OrderByDescending(s => s.FechaInicio)
                    .FirstOrDefaultAsync();

            ViewBag.PlanSuscripcion =
                suscripcion?.IdPlanNavigation.Nombre
                ?? "Sin suscripción activa";


            var productosInventarioBajo = await _context.Productos
                .AsNoTracking()
                .Include(p => p.ProductoImagene)
                .Where(p =>
                    p.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    p.TipoPublicacion == "Producto" &&
                    p.Estado == "Publicado" &&
                    p.StockActual > 0 &&
                    p.StockActual <= 3)
                .OrderBy(p => p.StockActual)
                .ToListAsync();

            ViewBag.ProductosInventarioBajo = productosInventarioBajo;

            // Cargar las ventas reales del emprendimiento
            await CargarVentasEmprendimientoAsync();

            // Contar los productos publicados del emprendimiento
            ViewBag.TotalProductosPublicados = await _context.Productos
                .AsNoTracking()
                .CountAsync(p =>
                    p.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    p.TipoPublicacion == "Producto" &&
                    p.Estado == "Publicado");

            // Cargar los próximos eventos reales
            DateTime ahora = DateTime.Now;

            ViewBag.ProximosEventos = await _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    (e.Estado == "Programado" ||
                     e.Estado == "Activo") &&
                    (e.FechaFin ?? e.FechaInicio) >= ahora)
                .OrderBy(e => e.FechaInicio)
                .Take(2)
                .Select(e => new EventoPublicoViewModel
                {
                    IdEvento = e.IdEvento,
                    Nombre = e.Nombre,
                    Ubicacion = e.Ubicacion,
                    ImagenUrl = e.ImagenUrl,
                    FechaInicio = e.FechaInicio,
                    FechaFin = e.FechaFin
                })
                .ToListAsync();

            // Cargar las últimas 3 notificaciones reales del emprendedor
            ViewBag.ActividadReciente = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.IdUsuario == idUsuario.Value)
                .OrderByDescending(n => n.FechaEnvio)
                .Take(3)
                .ToListAsync();

            return View();

        }


        [HttpGet]
        public async Task<IActionResult> Notificaciones()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var datos = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.IdUsuario == idUsuario.Value)
                .OrderByDescending(n => n.FechaEnvio)
                .ToListAsync();

            var modelo = datos.Select(n => new NotificacionListadoViewModel
            {
                Id = (int)n.IdNotificacion,
                Tipo = n.Tipo,
                Titulo = n.Titulo,
                Mensaje = n.Mensaje,
                Fecha = n.FechaEnvio.ToString("dd/MM/yyyy HH:mm"),
                Icono = n.Tipo == "Compra"
                    ? "bi-bag-check-fill"
                    : "bi-bell-fill",
                Leida = n.Leida
            }).ToList();

            return View(modelo);
        }


        [HttpGet]
        public async Task<IActionResult> SolicitudEmprendimiento(
    int? idCategoria)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Usuario? usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo"
                );

            if (usuario == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            SolicitudEmprendimientoViewModel modelo = new()
            {
                NombreComercial = string.Empty,
                Descripcion = string.Empty,

                IdCategoria = idCategoria,

                Cedula = string.Empty,

                Telefono =
                    usuario.Telefono ?? string.Empty,

                Correo = usuario.Correo,

                SitioWeb = null,
                Instagram = null,
                Facebook = null,

                ParticipaClubCreativo = false,
                ParticipaHechoEnCr = false,

                InformacionParticipacion = string.Empty,
                ConfirmaInformacion = false
            };

            modelo.Categorias = await _context.Categorias
                .Where(c =>
                    c.Modulo == "Emprendimientos" &&
                    c.Activa
                )
                .OrderBy(c => c.Nombre)
                .Select(c =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.IdCategoria.ToString(),
                        Text = c.Nombre
                    })
                .ToListAsync();

            modelo.EtiquetasDisponibles = await _context.Etiquetas
    .AsNoTracking()
    .Where(e => e.Activa)
    .OrderBy(e => e.Nombre)
    .Select(e => new SelectListItem
    {
        Value = e.IdEtiqueta.ToString(),
        Text = e.Nombre
    })
    .ToListAsync();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitudEmprendimiento(
            SolicitudEmprendimientoViewModel modelo)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información suministrada es correcta."
                );
            }

            if (!modelo.ParticipaClubCreativo &&
                !modelo.ParticipaHechoEnCr)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debe seleccionar al menos una opción de participación."
                );
            }

            if (modelo.IdCategoria.HasValue)
            {
                bool categoriaExiste =
                    await _context.Categorias.AnyAsync(c =>
                        c.IdCategoria == modelo.IdCategoria.Value &&
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    );

                if (!categoriaExiste)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdCategoria),
                        "La categoría seleccionada no es válida."
                    );
                }
            }

            List<int> idsEtiquetas = (modelo.IdEtiquetas ?? new List<int>())
    .Distinct()
    .ToList();

            if (idsEtiquetas.Count > 0)
            {
                int cantidadEtiquetasValidas = await _context.Etiquetas
                    .CountAsync(e =>
                        idsEtiquetas.Contains(e.IdEtiqueta) &&
                        e.Activa
                    );

                if (cantidadEtiquetasValidas != idsEtiquetas.Count)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdEtiquetas),
                        "Una o más etiquetas seleccionadas no son válidas."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                modelo.Categorias = await _context.Categorias
                    .Where(c =>
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    )
                    .OrderBy(c => c.Nombre)
                    .Select(c =>
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.IdCategoria.ToString(),
                            Text = c.Nombre
                        })
                    .ToListAsync();

                modelo.EtiquetasDisponibles = await _context.Etiquetas
    .AsNoTracking()
    .Where(e => e.Activa)
    .OrderBy(e => e.Nombre)
    .Select(e => new SelectListItem
    {
        Value = e.IdEtiqueta.ToString(),
        Text = e.Nombre
    })
    .ToListAsync();

                return View(modelo);
            }

            Usuario? usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value &&
                    u.Estado == "Activo"
                );

            if (usuario == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            bool yaTieneEmprendimiento =
                await _context.Emprendimientos.AnyAsync(e =>
                    e.IdUsuarioPropietario == usuario.IdUsuario
                );

            if (yaTieneEmprendimiento)
            {
                TempData["MensajeError"] =
                    "Ya existe una solicitud de emprendimiento asociada a tu cuenta.";

                return RedirectToAction(
     "EstadoSolicitud",
     "Emprendedor"
 );
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                Emprendimiento emprendimiento = new()
                {
                    IdUsuarioPropietario = usuario.IdUsuario,
                    IdCategoria = modelo.IdCategoria,

                    NombreComercial =
                        modelo.NombreComercial.Trim(),

                    Descripcion =
                        modelo.Descripcion.Trim(),

                    CedulaJuridica =
                        modelo.Cedula.Trim(),

                    Telefono =
                        modelo.Telefono.Trim(),

                    Correo =
                        modelo.Correo.Trim().ToLowerInvariant(),

                    SitioWeb =
                        string.IsNullOrWhiteSpace(modelo.SitioWeb)
                            ? null
                            : modelo.SitioWeb.Trim(),

                    Instagram =
                        string.IsNullOrWhiteSpace(modelo.Instagram)
                            ? null
                            : modelo.Instagram.Trim(),

                    Facebook =
                        string.IsNullOrWhiteSpace(modelo.Facebook)
                            ? null
                            : modelo.Facebook.Trim(),

                    LogoUrl = null,

                    ParticipaClubCreativo =
                        modelo.ParticipaClubCreativo,

                    ParticipaHechoEnCr =
                        modelo.ParticipaHechoEnCr,

                    EstadoAprobacion = "Pendiente",
                    Activo = true
                };

                await _context.Emprendimientos
                    .AddAsync(emprendimiento);

                await _context.SaveChangesAsync();

                List<Etiqueta> etiquetasSeleccionadas = await _context.Etiquetas
    .Where(e =>
        idsEtiquetas.Contains(e.IdEtiqueta) &&
        e.Activa
    )
    .ToListAsync();

                foreach (Etiqueta etiqueta in etiquetasSeleccionadas)
                {
                    emprendimiento.IdEtiqueta.Add(etiqueta);
                }

                await _context.SaveChangesAsync();

                EmprendimientoRevisione revision = new()
                {
                    IdEmprendimiento =
                        emprendimiento.IdEmprendimiento,

                    FechaSolicitud = DateTime.Now,
                    FechaResolucion = null
                };

                await _context.EmprendimientoRevisiones
                    .AddAsync(revision);

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                TempData["MensajeSolicitud"] =
                    "Tu solicitud de emprendimiento fue enviada correctamente y se encuentra pendiente de revisión.";

                return RedirectToAction(
    "EstadoSolicitud",
    "Emprendedor"
);
            }
            catch
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al enviar la solicitud. Intente nuevamente."
                );

                modelo.Categorias = await _context.Categorias
                    .Where(c =>
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    )
                    .OrderBy(c => c.Nombre)
                    .Select(c =>
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.IdCategoria.ToString(),
                            Text = c.Nombre
                        })
                    .ToListAsync();

                modelo.EtiquetasDisponibles = await _context.Etiquetas
    .AsNoTracking()
    .Where(e => e.Activa)
    .OrderBy(e => e.Nombre)
    .Select(e => new SelectListItem
    {
        Value = e.IdEtiqueta.ToString(),
        Text = e.Nombre
    })
    .ToListAsync();

                return View(modelo);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EstadoSolicitud()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .Include(e => e.IdCategoriaNavigation)
                .Include(e => e.EmprendimientoRevisione)
                .Include(e => e.MotivosRechazos)
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                return RedirectToAction(nameof(SolicitudEmprendimiento));
            }

            EstadoSolicitudViewModel modelo = new()
            {
                NombreComercial = emprendimiento.NombreComercial,
                Categoria = emprendimiento.IdCategoriaNavigation?.Nombre
                    ?? "Sin categoría",
                Correo = emprendimiento.Correo,
                Telefono = emprendimiento.Telefono ?? string.Empty,
                SitioWeb = emprendimiento.SitioWeb,
                Descripcion = emprendimiento.Descripcion,
                Estado = emprendimiento.EstadoAprobacion,
                FechaSolicitud = emprendimiento.EmprendimientoRevisione?.FechaSolicitud,
                FechaResolucion = emprendimiento.EmprendimientoRevisione?.FechaResolucion,
                MotivoRechazo = emprendimiento.MotivosRechazos
                    .OrderByDescending(m => m.FechaRechazo)
                    .FirstOrDefault()?.Motivo
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarNotificacionSolicitud()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .Include(e => e.IdUsuarioPropietarioNavigation)
                .Include(e => e.MotivosRechazos)
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null ||
                (emprendimiento.EstadoAprobacion != "Aprobado" &&
                 emprendimiento.EstadoAprobacion != "Rechazado"))
            {
                TempData["MensajeError"] =
                    "No existe una resolución disponible para reenviar.";

                return RedirectToAction(nameof(EstadoSolicitud));
            }

            string? correoDestino =
                emprendimiento.IdUsuarioPropietarioNavigation?.Correo;

            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                TempData["MensajeError"] =
                    "No se encontró un correo registrado para enviar la notificación.";

                return RedirectToAction(nameof(EstadoSolicitud));
            }

            bool aprobada = emprendimiento.EstadoAprobacion == "Aprobado";

            string? motivo = emprendimiento.MotivosRechazos
                .OrderByDescending(m => m.FechaRechazo)
                .FirstOrDefault()?.Motivo;

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
                ? $@"<p>Hola {PlantillaCorreo.E(nombreDestinatario)},</p>
             <p>¡Buenas noticias! Tu solicitud para incorporar
             «<strong>{PlantillaCorreo.E(emprendimiento.NombreComercial)}</strong>»
             a Club Creativo fue <strong>aprobada</strong>.</p>
             <p>Ya puedes iniciar sesión con tu cuenta de emprendedor para
             administrar tu perfil, productos y participación en la plataforma.</p>"
                : $@"<p>Hola {PlantillaCorreo.E(nombreDestinatario)},</p>
             <p>Tu solicitud para incorporar
             «<strong>{PlantillaCorreo.E(emprendimiento.NombreComercial)}</strong>»
             a Club Creativo fue <strong>rechazada</strong>.</p>
             <p>Motivo indicado por el equipo revisor:</p>
             <p style=""background:#faf7ff;border-left:4px solid #a855c7;padding:12px 16px;border-radius:6px;"">
                 {PlantillaCorreo.E(motivo)}
             </p>
             <p>Puedes iniciar sesión, corregir la información de tu
             emprendimiento y volver a enviarla para revisión.</p>";

            string urlBase = $"{Request.Scheme}://{Request.Host}";

            string contenidoCorreo = PlantillaCorreo.Construir(
                titulo: titulo,
                cuerpoHtml: cuerpo,
                textoBoton: "Iniciar sesión",
                urlBoton: $"{urlBase}/Cuenta/IniciarSesion",
                urlBase: urlBase
            );

            try
            {
                await _correoService.EnviarCorreoAsync(
                    correoDestino,
                    asunto,
                    contenidoCorreo
                );

                TempData["MensajeSolicitud"] =
                    "La notificación fue reenviada correctamente a tu correo electrónico.";
            }
            catch
            {
                TempData["MensajeError"] =
                    "No fue posible reenviar la notificación. Intenta nuevamente.";
            }

            return RedirectToAction(nameof(EstadoSolicitud));
        }

        [HttpGet]
        public async Task<IActionResult> PerfilEmprendimiento()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .Include(e => e.IdCategoriaNavigation)
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                return RedirectToAction(nameof(SolicitudEmprendimiento));
            }

            PerfilEmprendimientoViewModel modelo = new()
            {
                NombreComercial = emprendimiento.NombreComercial,
                Descripcion = emprendimiento.Descripcion,
                Categoria = emprendimiento.IdCategoriaNavigation?.Nombre ?? "Sin categoría",
                Cedula = emprendimiento.CedulaJuridica ?? string.Empty,
                EstadoAprobacion = emprendimiento.EstadoAprobacion,
                Activo = emprendimiento.Activo,
                LogoUrl = emprendimiento.LogoUrl,
                Correo = emprendimiento.Correo,
                Telefono = emprendimiento.Telefono ?? string.Empty,
                SitioWeb = emprendimiento.SitioWeb,
                Instagram = emprendimiento.Instagram,
                Facebook = emprendimiento.Facebook,
                ParticipaClubCreativo = emprendimiento.ParticipaClubCreativo,
                ParticipaHechoEnCr = emprendimiento.ParticipaHechoEnCr
            };

            modelo.Fotografias = await _context.Galerias
                .AsNoTracking()
              .Include(g => g.GaleriaImagenes)
                .Where(g =>
                    g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                  g.GaleriaImagenes.Any())
                .OrderBy(g => g.FechaCreacion)
                .Select(g => g.GaleriaImagenes.OrderBy(i => i.OrdenVisual).Select(i => i.UrlImagen).First())
                .ToListAsync();

            List<Producto> productosDestacados = await _context.Productos
     .AsNoTracking()
     .Include(p => p.IdCategoriaNavigation)
     .Include(p => p.ProductoImagene)
     .Where(p =>
         p.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
         p.EsDestacado)
     .ToListAsync();

            modelo.ProductosDestacados = productosDestacados
                .Select(p => new ProductoPerfilViewModel
                {
                    Nombre = p.Nombre,
                    Categoria = p.IdCategoriaNavigation?.Nombre ?? "Sin categoría",
                    Precio = p.Precio,
                    Estado = p.Estado,
                    ImagenUrl = p.ProductoImagene?.UrlImagen
                })
                .ToList();

            modelo.ProductosPublicados = await _context.Productos
                .CountAsync(p =>
                    p.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    p.Estado == "Activo");

            modelo.VentasRealizadas = await _context.VentaDetalles
                .CountAsync(vd =>
                    vd.IdProductoNavigation.IdEmprendimiento == emprendimiento.IdEmprendimiento);

            modelo.Favoritos = await _context.Favoritos
                .CountAsync(f =>
                    f.IdEmprendimiento == emprendimiento.IdEmprendimiento ||
                    (f.IdProducto != null &&
                     f.IdProductoNavigation!.IdEmprendimiento == emprendimiento.IdEmprendimiento));

            List<byte?> valoraciones = await _context.ComentariosResenas
                .Where(c =>
                    (c.IdEmprendimiento == emprendimiento.IdEmprendimiento ||
                     (c.IdProducto != null &&
                      c.IdProductoNavigation!.IdEmprendimiento == emprendimiento.IdEmprendimiento)) &&
                    c.Valoracion != null)
                .Select(c => c.Valoracion)
                .ToListAsync();

            modelo.ValoracionPromedio = valoraciones.Count > 0
                ? Math.Round(valoraciones.Average(v => v!.Value), 1)
                : null;

            return View(modelo);
        }


        [HttpGet]
        public async Task<IActionResult> EditarEmprendimiento()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
    .AsNoTracking()
    .Include(e => e.IdEtiqueta)
    .FirstOrDefaultAsync(e =>
        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                return RedirectToAction(
                    "SolicitudEmprendimiento",
                    "Emprendedor"
                );
            }

            SolicitudEmprendimientoViewModel modelo = new()
            {
                NombreComercial = emprendimiento.NombreComercial,
                Descripcion = emprendimiento.Descripcion,
                IdCategoria = emprendimiento.IdCategoria,
                IdEtiquetas = emprendimiento.IdEtiqueta
    .Select(e => e.IdEtiqueta)
    .ToList(),
                Cedula = emprendimiento.CedulaJuridica ?? string.Empty,
                Telefono = emprendimiento.Telefono ?? string.Empty,
                Correo = emprendimiento.Correo,
                SitioWeb = emprendimiento.SitioWeb,
                Instagram = emprendimiento.Instagram,
                Facebook = emprendimiento.Facebook,
                ParticipaClubCreativo = emprendimiento.ParticipaClubCreativo,
                ParticipaHechoEnCr = emprendimiento.ParticipaHechoEnCr,
                InformacionParticipacion = string.Empty,
                ConfirmaInformacion = false
            };

            modelo.Categorias = await _context.Categorias
                .Where(c =>
                    c.Modulo == "Emprendimientos" &&
                    c.Activa
                )
                .OrderBy(c => c.Nombre)
                .Select(c =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.IdCategoria.ToString(),
                        Text = c.Nombre
                    })
                .ToListAsync();

            modelo.EtiquetasDisponibles = await _context.Etiquetas
    .AsNoTracking()
    .Where(e => e.Activa)
    .OrderBy(e => e.Nombre)
    .Select(e => new SelectListItem
    {
        Value = e.IdEtiqueta.ToString(),
        Text = e.Nombre
    })
    .ToListAsync();

            ViewBag.LogoActual =
                string.IsNullOrWhiteSpace(emprendimiento.LogoUrl)
                    ? "/images/logo.jpg"
                    : emprendimiento.LogoUrl;

            ViewBag.FotografiasActuales = await _context.Galerias
                .AsNoTracking()
                .Include(g => g.GaleriaImagenes)
                .Where(g =>
                    g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    g.GaleriaImagenes.Any())
                .OrderBy(g => g.FechaCreacion)
                .Select(g => g.GaleriaImagenes.OrderBy(i => i.OrdenVisual).Select(i => i.UrlImagen).First())
                .ToListAsync();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEmprendimiento(
     SolicitudEmprendimientoViewModel modelo)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
     .Include(e => e.EmprendimientoRevisione)
     .Include(e => e.IdEtiqueta)
     .FirstOrDefaultAsync(e =>
         e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                return RedirectToAction(
                    "SolicitudEmprendimiento",
                    "Emprendedor"
                );
            }

            ValidarArchivosSolicitud(modelo);

            if (!modelo.ParticipaClubCreativo &&
                !modelo.ParticipaHechoEnCr)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debe seleccionar al menos una opción de participación."
                );
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información suministrada es correcta."
                );
            }

            if (modelo.IdCategoria.HasValue)
            {
                bool categoriaExiste =
                    await _context.Categorias.AnyAsync(c =>
                        c.IdCategoria == modelo.IdCategoria.Value &&
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    );

                if (!categoriaExiste)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdCategoria),
                        "La categoría seleccionada no es válida."
                    );
                }
            }

            List<int> idsEtiquetas = (modelo.IdEtiquetas ?? new List<int>())
    .Distinct()
    .ToList();

            int cantidadEtiquetasValidas = await _context.Etiquetas
                .CountAsync(e =>
                    idsEtiquetas.Contains(e.IdEtiqueta) &&
                    e.Activa);

            if (cantidadEtiquetasValidas != idsEtiquetas.Count)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdEtiquetas),
                    "Una o varias etiquetas seleccionadas no son válidas."
                );
            }

            if (!ModelState.IsValid)
            {
                modelo.Categorias = await _context.Categorias
                    .Where(c =>
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    )
                    .OrderBy(c => c.Nombre)
                    .Select(c =>
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.IdCategoria.ToString(),
                            Text = c.Nombre
                        })
                    .ToListAsync();

                modelo.EtiquetasDisponibles = await _context.Etiquetas
    .AsNoTracking()
    .Where(e => e.Activa)
    .OrderBy(e => e.Nombre)
    .Select(e => new SelectListItem
    {
        Value = e.IdEtiqueta.ToString(),
        Text = e.Nombre
    })
    .ToListAsync();

                ViewBag.LogoActual =
                    string.IsNullOrWhiteSpace(emprendimiento.LogoUrl)
                        ? "/images/logo.jpg"
                        : emprendimiento.LogoUrl;

                ViewBag.FotografiasActuales = await _context.Galerias
                    .AsNoTracking()
                    .Include(g => g.GaleriaImagenes)
                    .Where(g =>
                        g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                      g.GaleriaImagenes.Any())
                    .OrderBy(g => g.FechaCreacion)
                    .Select(g => g.GaleriaImagenes.OrderBy(i => i.OrdenVisual).Select(i => i.UrlImagen).First())
                    .ToListAsync();

                return View(modelo);
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                emprendimiento.NombreComercial = modelo.NombreComercial.Trim();
                emprendimiento.Descripcion = modelo.Descripcion.Trim();
                emprendimiento.IdCategoria = modelo.IdCategoria;
                List<Etiqueta> etiquetasSeleccionadas = await _context.Etiquetas
    .Where(e => idsEtiquetas.Contains(e.IdEtiqueta) && e.Activa)
    .ToListAsync();

                emprendimiento.IdEtiqueta.Clear();

                foreach (Etiqueta etiqueta in etiquetasSeleccionadas)
                {
                    emprendimiento.IdEtiqueta.Add(etiqueta);
                }
                emprendimiento.CedulaJuridica = modelo.Cedula.Trim();
                emprendimiento.Telefono = modelo.Telefono.Trim();
                emprendimiento.Correo = modelo.Correo.Trim().ToLowerInvariant();

                emprendimiento.SitioWeb =
                    string.IsNullOrWhiteSpace(modelo.SitioWeb)
                        ? null
                        : modelo.SitioWeb.Trim();

                emprendimiento.Instagram =
                    string.IsNullOrWhiteSpace(modelo.Instagram)
                        ? null
                        : modelo.Instagram.Trim();

                emprendimiento.Facebook =
                    string.IsNullOrWhiteSpace(modelo.Facebook)
                        ? null
                        : modelo.Facebook.Trim();

                emprendimiento.ParticipaClubCreativo = modelo.ParticipaClubCreativo;
                emprendimiento.ParticipaHechoEnCr = modelo.ParticipaHechoEnCr;

                emprendimiento.EstadoAprobacion = "Pendiente";

                if (emprendimiento.EmprendimientoRevisione is not null)
                {
                    emprendimiento.EmprendimientoRevisione.FechaSolicitud = DateTime.Now;
                    emprendimiento.EmprendimientoRevisione.FechaResolucion = null;
                }
                else
                {
                    await _context.EmprendimientoRevisiones.AddAsync(
                        new EmprendimientoRevisione
                        {
                            IdEmprendimiento = emprendimiento.IdEmprendimiento,
                            FechaSolicitud = DateTime.Now,
                            FechaResolucion = null
                        });
                }

                if (modelo.Logo is not null)
                {
                    string carpetaLogos = Path.Combine(
                        _entornoWeb.WebRootPath, "uploads", "emprendimientos");

                    Directory.CreateDirectory(carpetaLogos);

                    string nombreArchivoLogo =
                        $"{Guid.NewGuid()}{Path.GetExtension(modelo.Logo.FileName)}";

                    string rutaLogo =
                        Path.Combine(carpetaLogos, nombreArchivoLogo);

                    using (var flujoLogo = new FileStream(rutaLogo, FileMode.Create))
                    {
                        await modelo.Logo.CopyToAsync(flujoLogo);
                    }

                    emprendimiento.LogoUrl =
                        $"/uploads/emprendimientos/{nombreArchivoLogo}";
                }

                if (modelo.Fotografias is { Count: > 0 })
                {
                    string carpetaFotos = Path.Combine(
                        _entornoWeb.WebRootPath, "uploads", "emprendimientos", "galeria");

                    Directory.CreateDirectory(carpetaFotos);

                    foreach (IFormFile fotografia in modelo.Fotografias)
                    {
                        if (fotografia.Length <= 0)
                        {
                            continue;
                        }

                        string nombreArchivoFoto =
                            $"{Guid.NewGuid()}{Path.GetExtension(fotografia.FileName)}";

                        string rutaFoto =
                            Path.Combine(carpetaFotos, nombreArchivoFoto);

                        using (var flujoFoto = new FileStream(rutaFoto, FileMode.Create))
                        {
                            await fotografia.CopyToAsync(flujoFoto);
                        }

                        Galeria galeria = new()
                        {
                            IdEmprendimiento = emprendimiento.IdEmprendimiento,
                            Nombre = $"Foto de {emprendimiento.NombreComercial}",
                            Estado = "Publicada",
                            FechaCreacion = DateTime.Now
                        };

                        await _context.Galerias.AddAsync(galeria);
                        await _context.SaveChangesAsync();

                        await _context.GaleriaImagenes.AddAsync(new GaleriaImagene
                        {
                            IdGaleria = galeria.IdGaleria,
                            UrlImagen = $"/uploads/emprendimientos/galeria/{nombreArchivoFoto}",
                            NombreArchivo = fotografia.FileName,
                            EsPortada = false,
                            OrdenVisual = 1
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                TempData["MensajePerfil"] =
                    "La información del emprendimiento fue actualizada correctamente y se envió a revisión.";

                return RedirectToAction(
                    "PerfilEmprendimiento",
                    "Emprendedor"
                );
            }
            catch
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al actualizar la información. Intente nuevamente."
                );

                modelo.Categorias = await _context.Categorias
                    .Where(c =>
                        c.Modulo == "Emprendimientos" &&
                        c.Activa
                    )
                    .OrderBy(c => c.Nombre)
                    .Select(c =>
                        new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.IdCategoria.ToString(),
                            Text = c.Nombre
                        })
                    .ToListAsync();

                modelo.EtiquetasDisponibles = await _context.Etiquetas
    .AsNoTracking()
    .Where(e => e.Activa)
    .OrderBy(e => e.Nombre)
    .Select(e => new SelectListItem
    {
        Value = e.IdEtiqueta.ToString(),
        Text = e.Nombre
    })
    .ToListAsync();

                ViewBag.LogoActual =
                    string.IsNullOrWhiteSpace(emprendimiento.LogoUrl)
                        ? "/images/logo.jpg"
                        : emprendimiento.LogoUrl;

                ViewBag.FotografiasActuales = await _context.Galerias
                    .AsNoTracking()
                   .Include(g => g.GaleriaImagenes)
                    .Where(g =>
                        g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                      g.GaleriaImagenes.Any())
                    .OrderBy(g => g.FechaCreacion)
                    .Select(g => g.GaleriaImagenes.OrderBy(i => i.OrdenVisual).Select(i => i.UrlImagen).First())
                    .ToListAsync();

                return View(modelo);
            }
        }

        private void ValidarArchivosSolicitud(
    SolicitudEmprendimientoViewModel modelo)
        {
            string[] extensionesPermitidas =
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            const long tamanoMaximo =
                5 * 1024 * 1024;

            if (modelo.Logo is not null)
            {
                string extensionLogo =
                    Path.GetExtension(modelo.Logo.FileName)
                        .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extensionLogo))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Logo),
                        "El logo debe ser JPG, JPEG, PNG o WEBP."
                    );
                }

                if (modelo.Logo.Length > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Logo),
                        "El logo puede pesar como máximo 5 MB."
                    );
                }
            }

            if (modelo.Fotografias is not null &&
                modelo.Fotografias.Count > 0)
            {
                if (modelo.Fotografias.Count > 5)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fotografias),
                        "Puede seleccionar un máximo de cinco fotografías."
                    );
                }

                foreach (IFormFile fotografia in modelo.Fotografias)
                {
                    string extension =
                        Path.GetExtension(fotografia.FileName)
                            .ToLowerInvariant();

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        ModelState.AddModelError(
                            nameof(modelo.Fotografias),
                            "Las fotografías deben ser JPG, JPEG, PNG o WEBP."
                        );

                        break;
                    }

                    if (fotografia.Length > tamanoMaximo)
                    {
                        ModelState.AddModelError(
                            nameof(modelo.Fotografias),
                            "Cada fotografía puede pesar como máximo 5 MB."
                        );

                        break;
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> PlanesSuscripcion()
        {
            List<PlanDisponibleViewModel> planes = await _context.PlanesSuscripcions
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Precio)
                .Select(p => new PlanDisponibleViewModel
                {
                    IdPlan = p.IdPlan,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion ?? string.Empty,
                    Precio = p.Precio,
                    Periodicidad = p.Periodicidad,
                    Beneficios = p.Beneficios ?? string.Empty
                })
                .ToListAsync();

            SeleccionPlanViewModel modelo = new()
            {
                Planes = planes
            };

            return View(modelo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlanesSuscripcion(
    SeleccionPlanViewModel modelo)
        {
            if (!modelo.ConfirmaSeleccion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaSeleccion),
                    "Debe confirmar la selección del plan."
                );
            }


            if (!ModelState.IsValid)
            {
                modelo.Planes = await _context.PlanesSuscripcions
                    .AsNoTracking()
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Precio)
                    .Select(p => new PlanDisponibleViewModel
                    {
                        IdPlan = p.IdPlan,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion ?? string.Empty,
                        Precio = p.Precio,
                        Periodicidad = p.Periodicidad,
                        Beneficios = p.Beneficios ?? string.Empty
                    })
                    .ToListAsync();

                return View(modelo);
            }

            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null ||
                !emprendimiento.Activo ||
                emprendimiento.EstadoAprobacion != "Aprobado")
            {
                TempData["MensajeError"] =
                    "Debe tener un emprendimiento activo y aprobado para adquirir un plan.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            PlanesSuscripcion? plan =
                await _context.PlanesSuscripcions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.IdPlan == modelo.IdPlanSeleccionado &&
                        p.Activo);

            if (plan is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdPlanSeleccionado),
                    "El plan seleccionado no está disponible."
                );

                modelo.Planes = await _context.PlanesSuscripcions
                    .AsNoTracking()
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Precio)
                    .Select(p => new PlanDisponibleViewModel
                    {
                        IdPlan = p.IdPlan,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion ?? string.Empty,
                        Precio = p.Precio,
                        Periodicidad = p.Periodicidad,
                        Beneficios = p.Beneficios ?? string.Empty
                    })
                    .ToListAsync();

                return View(modelo);
            }

            return RedirectToAction(
                nameof(SolicitudPagoSinpe),
                new
                {
                    idPlan = plan.IdPlan
                }
            );
        }

        [HttpGet]
        public async Task<IActionResult> SolicitudPagoSinpe(
    int idPlan, bool renovacion = false)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null ||
                !emprendimiento.Activo ||
                emprendimiento.EstadoAprobacion != "Aprobado")
            {
                TempData["MensajeError"] =
                    "Debes tener un emprendimiento activo y aprobado para solicitar un plan.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            // Verificar que el plan seleccionado esté disponible.
            PlanesSuscripcion? plan =
                await _context.PlanesSuscripcions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.IdPlan == idPlan && p.Activo);

            if (plan is null)
            {
                TempData["MensajeError"] =
                    "El plan seleccionado no está disponible.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            // No permitir nuevas solicitudes mientras exista una pendiente.
            bool tieneSolicitudPendiente =
                await _context.SolicitudesPagoSuscripcion
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente");

            if (tieneSolicitudPendiente)
            {
                TempData["MensajeError"] =
                    "Ya tienes una solicitud de pago pendiente de revisión.";

                return RedirectToAction(nameof(MiSuscripcion));
            }
            DateOnly fechaActual = DateOnly.FromDateTime(DateTime.Today);

            bool tieneSuscripcionProgramada =
                await _context.Suscripciones
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente" &&
                        s.FechaInicio > fechaActual);

            if (tieneSuscripcionProgramada)
            {
                TempData["MensajeError"] =
                    "Ya tienes una suscripción pagada y programada.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            // Comprobar si existe una suscripción vigente.
            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            Suscripcione? suscripcionActual =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Where(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= hoy &&
                        s.FechaFin >= hoy)
                    .OrderByDescending(s => s.FechaInicio)
                    .FirstOrDefaultAsync();

            if (suscripcionActual != null &&
                suscripcionActual.IdPlan == plan.IdPlan &&
                !renovacion)
            {
                TempData["MensajeError"] =
                    "Ya tienes este plan vigente.";

                return RedirectToAction(nameof(MiSuscripcion));
            }
            if (renovacion)
            {
                if (suscripcionActual == null ||
                    suscripcionActual.IdPlan != plan.IdPlan ||
                    suscripcionActual.FechaFin.DayNumber - hoy.DayNumber > 3)
                {
                    TempData["MensajeError"] =
                        "La renovación solo está disponible durante " +
                        "los últimos 3 días de vigencia del mismo plan.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }

                bool tieneCancelacionRegistrada =
                    await _context.SuscripcionCancelaciones
                        .AnyAsync(c =>
                            c.IdSuscripcion == suscripcionActual.IdSuscripcion);

                if (tieneCancelacionRegistrada)
                {
                    TempData["MensajeError"] =
                        "Esta suscripción tiene una cancelación registrada. " +
                        "No puedes solicitar su renovación.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }
            }
            if (!renovacion && suscripcionActual != null)
            {
                bool tieneCancelacionRegistrada =
                    await _context.SuscripcionCancelaciones
                        .AnyAsync(c =>
                            c.IdSuscripcion == suscripcionActual.IdSuscripcion);

                if (tieneCancelacionRegistrada)
                {
                    TempData["MensajeError"] =
                        "Esta suscripción tiene una cancelación registrada. " +
                        "No puedes solicitar un cambio de plan.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }
            }
            // Preparar los datos que se mostrarán en el formulario SINPE.
            SolicitudPagoSinpeViewModel modelo = new()
            {
                IdPlan = plan.IdPlan,
                NombrePlan = plan.Nombre,
                Monto = plan.Precio,
                Periodicidad = plan.Periodicidad,
                EsRenovacion = renovacion
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitudPagoSinpe(
    SolicitudPagoSinpeViewModel modelo)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null ||
                !emprendimiento.Activo ||
                emprendimiento.EstadoAprobacion != "Aprobado")
            {
                TempData["MensajeError"] =
                    "Debes tener un emprendimiento activo y aprobado.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            PlanesSuscripcion? plan =
                await _context.PlanesSuscripcions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.IdPlan == modelo.IdPlan && p.Activo);

            if (plan is null)
            {
                TempData["MensajeError"] =
                    "El plan seleccionado ya no está disponible.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            // Los datos del plan siempre se obtienen desde la BD.
            modelo.NombrePlan = plan.Nombre;
            modelo.Monto = plan.Precio;
            modelo.Periodicidad = plan.Periodicidad;

            modelo.NumeroReferencia =
                (modelo.NumeroReferencia ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(modelo.NumeroReferencia))
            {
                ModelState.AddModelError(
                    nameof(modelo.NumeroReferencia),
                    "Debes ingresar el número de referencia SINPE.");
            }

            if (!modelo.ConfirmaTransferencia)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaTransferencia),
                    "Debes confirmar que realizaste la transferencia.");
            }

            // Impedir solicitudes simultáneas pendientes.
            bool tienePendiente =
                await _context.SolicitudesPagoSuscripcion
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente");

            if (tienePendiente)
            {
                TempData["MensajeError"] =
                    "Ya tienes una solicitud pendiente de revisión.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            // Impedir reutilizar referencias de pago.
            bool referenciaUtilizada =
                await _context.SolicitudesPagoSuscripcion
                    .AnyAsync(s =>
                        s.NumeroReferencia == modelo.NumeroReferencia &&
                        s.Estado != "Rechazada");

            if (referenciaUtilizada)
            {
                ModelState.AddModelError(
                    nameof(modelo.NumeroReferencia),
                    "Este número de referencia ya fue registrado.");
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            // Consultar la suscripción vigente.
            Suscripcione? suscripcionActual =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Where(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= hoy &&
                        s.FechaFin >= hoy)
                    .OrderByDescending(s => s.FechaInicio)
                    .FirstOrDefaultAsync();

            string tipoSolicitud = "Nueva";
            DateOnly? fechaInicioProgramada = null;
            int? idSuscripcionAnterior = null;

            if (modelo.EsRenovacion)
            {
                if (suscripcionActual == null ||
                    suscripcionActual.IdPlan != plan.IdPlan ||
                    suscripcionActual.FechaFin.DayNumber - hoy.DayNumber > 3)
                {
                    TempData["MensajeError"] =
                        "Solo puedes renovar tu mismo plan durante " +
                        "los últimos 3 días de vigencia.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }

                bool tieneCancelacionRegistrada =
                    await _context.SuscripcionCancelaciones
                        .AnyAsync(c =>
                            c.IdSuscripcion == suscripcionActual.IdSuscripcion);

                if (tieneCancelacionRegistrada)
                {
                    TempData["MensajeError"] =
                        "No puedes renovar esta suscripción porque " +
                        "ya tiene una cancelación registrada.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }
                tipoSolicitud = "Renovacion";
                idSuscripcionAnterior = suscripcionActual.IdSuscripcion;

                fechaInicioProgramada =
                    suscripcionActual.FechaFin.AddDays(1);
            }
            else if (suscripcionActual != null)
            {
                if (suscripcionActual.IdPlan == plan.IdPlan)
                {
                    TempData["MensajeError"] =
                        "Ya tienes este plan vigente.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }

                bool tieneCancelacionRegistrada =
                    await _context.SuscripcionCancelaciones
                        .AnyAsync(c =>
                            c.IdSuscripcion == suscripcionActual.IdSuscripcion);

                if (tieneCancelacionRegistrada)
                {
                    TempData["MensajeError"] =
                        "Esta suscripción tiene una cancelación registrada. " +
                        "No puedes solicitar un cambio de plan.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }
                tipoSolicitud = "CambioPlan";
                idSuscripcionAnterior = suscripcionActual.IdSuscripcion;

                fechaInicioProgramada =
                    suscripcionActual.FechaFin.AddDays(1);
            }

            // No permitir otro cambio cuando ya exista uno aprobado
            // y programado para comenzar en el futuro.
            bool tieneCambioProgramado =
                await _context.SolicitudesPagoSuscripcion
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        (s.TipoSolicitud == "CambioPlan" ||
                        s.TipoSolicitud == "Renovacion") &&
                        s.Estado == "Aprobada" &&
                        s.FechaInicioProgramada.HasValue &&
                        s.FechaInicioProgramada.Value > hoy);

            if (tieneCambioProgramado)
            {
                TempData["MensajeError"] =
                    "Ya tienes una renovación o un cambio de plan aprobado y programado.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            bool existeSuscripcionProgramada =
                await _context.Suscripciones
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente" &&
                        s.FechaInicio > hoy);

            if (existeSuscripcionProgramada)
            {
                TempData["MensajeError"] =
                    "Ya tienes una suscripción pagada y programada.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            SolicitudPagoSuscripcion solicitud = new()
            {
                IdEmprendimiento = emprendimiento.IdEmprendimiento,
                IdPlan = plan.IdPlan,
                Monto = plan.Precio,
                NumeroReferencia = modelo.NumeroReferencia,
                FechaSolicitud = DateTime.Now,
                Estado = "Pendiente",
                TipoSolicitud = tipoSolicitud,
                FechaInicioProgramada = fechaInicioProgramada,
                IdSuscripcionAnterior = idSuscripcionAnterior
            };

            _context.SolicitudesPagoSuscripcion.Add(solicitud);

            await _context.SaveChangesAsync();

            TempData["MensajeSuscripcion"] =
                "Tu solicitud de pago fue enviada correctamente. " +
                "El administrador verificará la transferencia SINPE.";

            return RedirectToAction(nameof(MiSuscripcion));
        }

        [HttpGet]
        public IActionResult PagoSuscripcion(int idPlan)
        {
            return RedirectToAction(
                nameof(SolicitudPagoSinpe),
                new { idPlan }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PagoSuscripcion(
    PagoSuscripcionViewModel modelo)
        {
            TempData["MensajeError"] =
                "El pago con tarjeta ya no está disponible. " +
                "Para adquirir un plan debes realizar la transferencia SINPE Móvil.";

            return RedirectToAction(nameof(PlanesSuscripcion));
        }

        [HttpGet]
        public async Task<IActionResult> MiSuscripcion()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento == null)
            {
                return RedirectToAction(nameof(Panel));
            }

            // Actualizar las suscripciones vencidas y activar
            // los planes SINPE aprobados cuya fecha ya llegó.
            await _actualizacionSuscripciones.ActualizarEstadosAsync();
            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            var suscripcion = await _context.Suscripciones
                .AsNoTracking()
                .Include(s => s.IdPlanNavigation)
                .Where(s =>
                    s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    s.Estado == "Activa" &&
                    s.FechaInicio <= fechaActual &&
                    s.FechaFin >= fechaActual)
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();

            MiSuscripcionViewModel modelo = new();

            var planProgramado = await _context.Suscripciones
                .AsNoTracking()
                .Include(s => s.IdPlanNavigation)
                .Where(s =>
                    s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    s.Estado == "Pendiente" &&
                    s.FechaInicio > fechaActual &&
                    _context.SolicitudesPagoSuscripcion.Any(p =>
                        p.IdSuscripcionGenerada == s.IdSuscripcion &&
                        p.Estado == "Aprobada"))
                .OrderBy(s => s.FechaInicio)
                .FirstOrDefaultAsync();

            if (planProgramado != null)
            {
                modelo.TienePlanProgramado = true;

                modelo.NombrePlanProgramado =
                    planProgramado.IdPlanNavigation.Nombre;

                modelo.FechaInicioPlanProgramado =
                    planProgramado.FechaInicio;

                modelo.FechaFinPlanProgramado =
                    planProgramado.FechaFin;
            }

            if (suscripcion != null)
            {
                modelo.TieneSuscripcion = true;
                modelo.IdSuscripcion = suscripcion.IdSuscripcion;
                modelo.IdPlan = suscripcion.IdPlan;

                modelo.NombrePlan =
                    suscripcion.IdPlanNavigation.Nombre;

                modelo.DescripcionPlan =
                    suscripcion.IdPlanNavigation.Descripcion;

                modelo.Precio =
                    suscripcion.IdPlanNavigation.Precio;

                modelo.Periodicidad =
                    suscripcion.IdPlanNavigation.Periodicidad;

                modelo.Beneficios =
                    suscripcion.IdPlanNavigation.Beneficios;

                modelo.FechaInicio =
                    suscripcion.FechaInicio;

                modelo.FechaFin =
                    suscripcion.FechaFin;

                modelo.Estado =
                    suscripcion.Estado;

                modelo.RenovacionAutomatica =
                    suscripcion.RenovacionAutomatica;

                var cancelacionProgramada =
                    await _context.SuscripcionCancelaciones
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.IdSuscripcion == suscripcion.IdSuscripcion);

                if (cancelacionProgramada != null)
                {
                    modelo.TieneCancelacionProgramada = true;

                    modelo.FechaSolicitudCancelacion =
                        cancelacionProgramada.FechaCancelacion;

                    modelo.MotivoCancelacionRegistrado =
                        cancelacionProgramada.MotivoCancelacion;
                }


            }

            var historialSuscripciones = await _context.Suscripciones
                .AsNoTracking()
                .Where(s =>
                    s.IdEmprendimiento == emprendimiento.IdEmprendimiento)
                .OrderByDescending(s => s.FechaInicio)
                .Select(s => new MovimientoSuscripcionViewModel
                {
                    Fecha = s.FechaInicio,

                    Descripcion = "Suscripción al plan " +
                        s.IdPlanNavigation.Nombre,

                    Monto = _context.SolicitudesPagoSuscripcion
                        .Where(p =>
                            p.IdSuscripcionGenerada == s.IdSuscripcion &&
                            p.Estado == "Aprobada")
                        .Select(p => (decimal?)p.Monto)
                        .FirstOrDefault() ?? 0,

                    Estado = s.Estado
                })
                .ToListAsync();

            modelo.Historial = historialSuscripciones;

            // Consultar las solicitudes SINPE del emprendimiento.
            modelo.SolicitudesSinpe =
                await _context.SolicitudesPagoSuscripcion
                    .AsNoTracking()
                    .Where(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento)
                    .OrderByDescending(s => s.FechaSolicitud)
                    .Select(s => new EstadoSolicitudSinpeViewModel
                    {
                        IdSolicitudPago = s.IdSolicitudPago,
                        NombrePlan = s.IdPlanNavigation.Nombre,
                        Monto = s.Monto,
                        NumeroReferencia = s.NumeroReferencia,
                        FechaSolicitud = s.FechaSolicitud,
                        Estado = s.Estado,
                        FechaRevision = s.FechaRevision,
                        Observaciones = s.Observaciones
                    })
                    .ToListAsync();

            return View(modelo);
        }

        [HttpGet]
        public IActionResult ActualizarMetodoPago()
        {
            TempData["MensajeError"] =
                "El pago con tarjeta ya no está disponible. " +
                "Las suscripciones se gestionan mediante SINPE Móvil.";

            return RedirectToAction(nameof(MiSuscripcion));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarMetodoPago(
    PagoSuscripcionViewModel modelo)
        {
            TempData["MensajeError"] =
                "La actualización de tarjetas ya no está disponible. " +
                "Para renovar tu suscripción debes utilizar SINPE Móvil.";

            return RedirectToAction(nameof(MiSuscripcion));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarSuscripcion(
    MiSuscripcionViewModel modelo)
        {
            if (!modelo.ConfirmaCancelacion)
            {
                TempData["MensajeError"] =
                    "Debe confirmar que desea cancelar la suscripción.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            if (string.IsNullOrWhiteSpace(modelo.MotivoCancelacion))
            {
                TempData["MensajeError"] =
                    "Debe indicar el motivo de la cancelación.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            if (modelo.MotivoCancelacion.Length > 400)
            {
                TempData["MensajeError"] =
                    "El motivo no puede superar los 400 caracteres.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                TempData["MensajeError"] =
                    "No se encontró el emprendimiento.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= hoy &&
                        s.FechaFin >= hoy);

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "No tienes una suscripción activa para cancelar.";

                return RedirectToAction(nameof(MiSuscripcion));
            }
            bool tienePagoPendiente =
                await _context.SolicitudesPagoSuscripcion
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente");

            if (tienePagoPendiente)
            {
                TempData["MensajeError"] =
                    "Tienes una solicitud de pago SINPE pendiente de revisión. " +
                    "Debes esperar a que el administrador la apruebe " +
                    "o rechace antes de solicitar la cancelación.";

                return RedirectToAction(nameof(MiSuscripcion));
            }
            

            bool tienePlanFuturoAprobado =
                await _context.Suscripciones
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente" &&
                        s.FechaInicio > hoy &&
                        _context.SolicitudesPagoSuscripcion.Any(p =>
                            p.IdSuscripcionGenerada == s.IdSuscripcion &&
                            p.Estado == "Aprobada"));

            if (tienePlanFuturoAprobado)
            {
                TempData["MensajeError"] =
                    "Ya tienes una renovación o cambio de plan pagado " +
                    "y programado para un período futuro. " +
                    "Podrás solicitar la cancelación durante ese nuevo período.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            bool cancelacionExistente =
                await _context.SuscripcionCancelaciones
                    .AnyAsync(c =>
                        c.IdSuscripcion ==
                            suscripcion.IdSuscripcion);

            if (cancelacionExistente)
            {
                TempData["MensajeError"] =
                    "Ya existe una solicitud de cancelación para esta suscripción.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            SuscripcionCancelacione cancelacion = new()
            {
                IdSuscripcion =
                    suscripcion.IdSuscripcion,

                MotivoCancelacion =
                    modelo.MotivoCancelacion.Trim(),

                FechaCancelacion =
                    DateTime.Now
            };

            _context.SuscripcionCancelaciones.Add(cancelacion);

            suscripcion.RenovacionAutomatica = false;

            await _context.SaveChangesAsync();

            TempData["MensajeSuscripcion"] =
                $"La cancelación fue registrada correctamente. " +
                $"Tu plan permanecerá activo hasta el " +
                $"{suscripcion.FechaFin:dd/MM/yyyy}.";

            return RedirectToAction(nameof(MiSuscripcion));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReactivarSuscripcion()
        {
            TempData["MensajeError"] =
                "La renovación automática ya no está disponible. " +
                "Si deseas continuar con tu plan, debes solicitar una renovación mediante SINPE Móvil.";

            return RedirectToAction(nameof(MiSuscripcion));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenovarSuscripcion()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value &&
                    e.Activo &&
                    e.EstadoAprobacion == "Aprobado");

            if (emprendimiento == null)
            {
                TempData["MensajeError"] =
                    "Debes tener un emprendimiento activo y aprobado.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

            var suscripcion = await _context.Suscripciones
                .AsNoTracking()
                .Where(s =>
                    s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    s.Estado == "Activa" &&
                    s.FechaInicio <= hoy &&
                    s.FechaFin >= hoy)
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();

            if (suscripcion == null)
            {
                TempData["MensajeError"] =
                    "No tienes una suscripción vigente para renovar.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            bool tieneCancelacionRegistrada =
                await _context.SuscripcionCancelaciones
                    .AnyAsync(c =>
                        c.IdSuscripcion == suscripcion.IdSuscripcion);

            if (tieneCancelacionRegistrada)
            {
                TempData["MensajeError"] =
                    "Ya solicitaste cancelar esta suscripción. " +
                    "No puedes renovarla mientras exista una cancelación registrada.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            int diasRestantes =
                suscripcion.FechaFin.DayNumber - hoy.DayNumber;

            if (diasRestantes > 3)
            {
                TempData["MensajeError"] =
                    "Podrás solicitar la renovación cuando falten " +
                    "3 días o menos para el vencimiento de tu plan.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            bool tieneSolicitudPendiente =
                await _context.SolicitudesPagoSuscripcion
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente");

            bool tienePlanProgramado =
                await _context.Suscripciones
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Pendiente" &&
                        s.FechaInicio > hoy);

            if (tieneSolicitudPendiente || tienePlanProgramado)
            {
                TempData["MensajeError"] =
                    "Ya tienes una solicitud en revisión o un plan programado.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            return RedirectToAction(
                nameof(SolicitudPagoSinpe),
                new
                {
                    idPlan = suscripcion.IdPlan,
                    renovacion = true
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcesarRenovacionesAutomaticas()
        {
            TempData["MensajeError"] =
                "Las renovaciones automáticas ya no están disponibles. " +
                "Para renovar tu plan debes realizar una nueva solicitud de pago SINPE Móvil.";

            return RedirectToAction(nameof(MiSuscripcion));
        }



        public async Task<IActionResult> MisProductos()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                TempData["MensajeError"] =
                    "No se encontró un emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(Panel));
            }

            List<Producto> productos =
                await _context.Productos
                    .AsNoTracking()
                    .Include(p => p.IdCategoriaNavigation)
                    .Include(p => p.ProductoImagene)
                    .Where(p =>
                        p.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento)
                    .OrderByDescending(p => p.FechaRegistro)
                    .ToListAsync();

            return View(productos);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublicarEnHechoEnCr(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var emprendimiento = await _context.Emprendimientos
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento == null)
            {
                TempData["MensajeError"] =
                    "No se encontró un emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(MisProductos));
            }

            if (!emprendimiento.Activo ||
                emprendimiento.EstadoAprobacion != "Aprobado" ||
                !emprendimiento.ParticipaHechoEnCr)
            {
                TempData["MensajeError"] =
                    "Tu emprendimiento debe estar aprobado y participar en Hecho en CR para publicar productos en esta sección.";

                return RedirectToAction(nameof(MisProductos));
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == id &&
                    p.IdEmprendimiento == emprendimiento.IdEmprendimiento);

            if (producto == null)
            {
                TempData["MensajeError"] =
                    "No se encontró el producto solicitado.";

                return RedirectToAction(nameof(MisProductos));
            }

            if (producto.Estado != "Publicado")
            {
                TempData["MensajeError"] =
                    "El producto debe estar publicado en el catálogo general antes de incluirlo en Hecho en CR.";

                return RedirectToAction(nameof(MisProductos));
            }
            DateOnly fechaActual =
    DateOnly.FromDateTime(DateTime.Today);

            bool tieneSuscripcionVigente =
                await _context.Suscripciones
                    .AsNoTracking()
                    .AnyAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= fechaActual &&
                        s.FechaFin >= fechaActual);

            if (!tieneSuscripcionVigente)
            {
                TempData["MensajeError"] =
                    "Necesitas una suscripción vigente para publicar productos en Hecho en CR.";

                return RedirectToAction(nameof(MisProductos));
            }

            if (producto.PublicadoHechoEnCr)
            {
                TempData["MensajeError"] =
                    "Este producto ya está publicado en Hecho en CR.";

                return RedirectToAction(nameof(MisProductos));
            }

            producto.PublicadoHechoEnCr = true;

            await _context.SaveChangesAsync();

            TempData["MensajeProducto"] =
                "El producto se publicó correctamente en Hecho en CR.";

            return RedirectToAction(nameof(MisProductos));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarDeHechoEnCr(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento == null)
            {
                TempData["MensajeError"] =
                    "No se encontró un emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(MisProductos));
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == id &&
                    p.IdEmprendimiento == emprendimiento.IdEmprendimiento);

            if (producto == null)
            {
                TempData["MensajeError"] =
                    "No se encontró el producto solicitado.";

                return RedirectToAction(nameof(MisProductos));
            }

            if (!producto.PublicadoHechoEnCr)
            {
                TempData["MensajeError"] =
                    "Este producto no está publicado en Hecho en CR.";

                return RedirectToAction(nameof(MisProductos));
            }

            producto.PublicadoHechoEnCr = false;

            await _context.SaveChangesAsync();

            TempData["MensajeProducto"] =
                "El producto se quitó correctamente de Hecho en CR.";

            return RedirectToAction(nameof(MisProductos));
        }


        [HttpGet]
        public async Task<IActionResult> CrearProducto()
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                TempData["MensajeError"] =
                    "No se encontró un emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(Panel));
            }

            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= fechaActual &&
                        s.FechaFin >= fechaActual);

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "Necesitas una suscripción activa para publicar productos o servicios.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            int limiteProductos =
                suscripcion.MaximoProductosContratado ?? 10;

            int cantidadProductos =
                await _context.Productos
                    .CountAsync(p =>
                        p.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento);

            if (cantidadProductos >= limiteProductos)
            {
                TempData["MensajeError"] =
                    $"Has alcanzado el límite de {limiteProductos} " +
                    $"publicaciones permitido por tu plan. " +
                    $"Puedes mejorar tu plan para publicar más productos o servicios.";

                return RedirectToAction(nameof(MisProductos));
            }


            var categoriasDisponibles = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Modulo == "Productos" && c.Activa)
                .OrderBy(c => c.Nombre)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.IdCategoria.ToString(),
                    Text = c.Nombre
                })
                .ToListAsync();

            var etiquetasDisponibles = await _context.Etiquetas
                .AsNoTracking()
                .Where(e => e.Activa)
                .OrderBy(e => e.Nombre)
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.IdEtiqueta.ToString(),
                    Text = e.Nombre
                })
                .ToListAsync();

            return View(new ProductoEmprendedorViewModel
            {
                TipoPublicacion = "Producto",
                Estado = "Publicado",
                Inventario = 1,
                CategoriasDisponibles = categoriasDisponibles,
                EtiquetasDisponibles = etiquetasDisponibles
            });

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearProducto(
    ProductoEmprendedorViewModel modelo)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                TempData["MensajeError"] =
                    "No se encontró un emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(Panel));
            }


            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= fechaActual &&
                        s.FechaFin >= fechaActual);

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "Necesitas una suscripción activa para publicar productos o servicios.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            int limiteProductos =
                suscripcion.MaximoProductosContratado ?? 10;

            int cantidadProductos =
                await _context.Productos
                    .CountAsync(p =>
                        p.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento);

            if (cantidadProductos >= limiteProductos)
            {
                TempData["MensajeError"] =
                    $"Has alcanzado el límite de {limiteProductos} " +
                    $"publicaciones permitido por tu plan. " +
                    $"Puedes mejorar tu plan para publicar más productos o servicios.";

                return RedirectToAction(nameof(MisProductos));
            }


            ModelState.Remove(nameof(modelo.IdEtiquetas));
            ModelState.Remove(nameof(modelo.Etiquetas));


            ValidarImagenesProducto(modelo);

            if (modelo.TipoPublicacion == "Producto" &&
                modelo.Inventario is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Inventario),
                    "Debe indicar la cantidad disponible."
                );
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información es correcta."
                );
            }


            Categoria? categoria = null;

            if (modelo.IdCategoria.HasValue)
            {
                categoria = await _context.Categorias
                    .FirstOrDefaultAsync(c =>
                        c.IdCategoria == modelo.IdCategoria.Value &&
                        c.Modulo == "Productos" &&
                        c.Activa);

                if (categoria is null)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdCategoria),
                        "La categoría seleccionada no es válida."
                    );
                }
            }


            List<int> idsEtiquetas = (modelo.IdEtiquetas ?? new List<int>())
    .Distinct()
    .ToList();

            List<Etiqueta> etiquetasSeleccionadas =
                await _context.Etiquetas
                    .Where(e =>
                        idsEtiquetas.Contains(e.IdEtiqueta) &&
                        e.Activa)
                    .ToListAsync();

            if (etiquetasSeleccionadas.Count != idsEtiquetas.Count)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdEtiquetas),
                    "Una o más etiquetas seleccionadas no son válidas."
                );
            }

            if (!ModelState.IsValid)
            {
                modelo.CategoriasDisponibles = await _context.Categorias
                    .AsNoTracking()
                    .Where(c => c.Modulo == "Productos" && c.Activa)
                    .OrderBy(c => c.Nombre)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.IdCategoria.ToString(),
                        Text = c.Nombre
                    })
                    .ToListAsync();

                modelo.EtiquetasDisponibles = await _context.Etiquetas
                    .AsNoTracking()
                    .Where(e => e.Activa)
                    .OrderBy(e => e.Nombre)
                    .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = e.IdEtiqueta.ToString(),
                        Text = e.Nombre
                    })
                    .ToListAsync();

                return View(modelo);
            }


            Producto producto = new()
            {
                IdEmprendimiento =
                    emprendimiento.IdEmprendimiento,

                IdCategoria =
                    categoria!.IdCategoria,

                Nombre =
                    modelo.Nombre.Trim(),

                Descripcion =
                    modelo.Descripcion.Trim(),

                TipoPublicacion =
                    modelo.TipoPublicacion,

                Precio =
                    modelo.Precio!.Value,

                StockActual =
                    modelo.TipoPublicacion == "Producto"
                        ? modelo.Inventario ?? 0
                        : 0,

                EsDestacado =
                    modelo.EsDestacado,

                Estado =
                    modelo.Estado,

                FechaPublicacion =
                    DateTime.Now,

                FechaRegistro =
                    DateTime.Now
            };


            producto.IdEtiqueta = etiquetasSeleccionadas;


            _context.Productos.Add(producto);

            await _context.SaveChangesAsync();

            IFormFile imagenPrincipal =
                modelo.Imagenes!.First();

            string extension =
                Path.GetExtension(imagenPrincipal.FileName)
                    .ToLowerInvariant();

            string nombreArchivo =
                $"{Guid.NewGuid()}{extension}";

            string carpetaProductos =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "productos"
                );

            Directory.CreateDirectory(carpetaProductos);

            string rutaFisica =
                Path.Combine(
                    carpetaProductos,
                    nombreArchivo
                );

            using (FileStream stream =
                   new FileStream(rutaFisica, FileMode.Create))
            {
                await imagenPrincipal.CopyToAsync(stream);
            }

            ProductoImagene imagenProducto = new()
            {
                IdProducto = producto.IdProducto,
                UrlImagen =
                    $"/uploads/productos/{nombreArchivo}",
                NombreArchivo = nombreArchivo,
                OrdenVisual = 1,
                EsPrincipal = true
            };

            _context.ProductoImagenes.Add(imagenProducto);

            await _context.SaveChangesAsync();

            TempData["MensajeProducto"] =
                "El producto o servicio fue publicado correctamente.";

            return RedirectToAction(nameof(MisProductos));
        }

        private void ValidarImagenesProducto(
    ProductoEmprendedorViewModel modelo)
        {
            if (modelo.Imagenes is null ||
                modelo.Imagenes.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.Imagenes),
                    "Debe seleccionar al menos una imagen."
                );

                return;
            }

            if (modelo.Imagenes.Count > 5)
            {
                ModelState.AddModelError(
                    nameof(modelo.Imagenes),
                    "Puede seleccionar un máximo de cinco imágenes."
                );
            }

            string[] extensionesPermitidas =
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            const long tamanoMaximo =
                5 * 1024 * 1024;

            foreach (IFormFile imagen in modelo.Imagenes)
            {
                string extension =
                    Path.GetExtension(imagen.FileName)
                        .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Todas las imágenes deben ser JPG, JPEG, PNG o WEBP."
                    );

                    break;
                }

                if (imagen.Length > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Cada imagen puede pesar como máximo 5 MB."
                    );

                    break;
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditarProducto(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction(nameof(MisProductos));
            }

            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                return RedirectToAction(nameof(Panel));
            }

            Producto? producto =
                await _context.Productos
                    .AsNoTracking()
                    .Include(p => p.IdCategoriaNavigation)
.Include(p => p.IdEtiqueta)
.Include(p => p.ProductoImagene)
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == id.Value &&
                        p.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento);

            if (producto is null)
            {
                TempData["MensajeError"] =
                    "El producto o servicio que intentas editar no existe.";

                return RedirectToAction(nameof(MisProductos));
            }

            ProductoEmprendedorViewModel modelo = new()
            {
                Nombre = producto.Nombre,

                TipoPublicacion =
                    producto.TipoPublicacion,


                IdCategoria = producto.IdCategoria,

                IdEtiquetas = producto.IdEtiqueta
    .Select(e => e.IdEtiqueta)
    .ToList(),

                Descripcion =
                    producto.Descripcion,

                Precio =
                    producto.Precio,

                Inventario =
                    producto.TipoPublicacion == "Producto"
                        ? producto.StockActual
                        : null,

                Estado =
                    producto.Estado,

                EsDestacado =
                    producto.EsDestacado
            };


            modelo.CategoriasDisponibles = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Modulo == "Productos" && c.Activa)
                .OrderBy(c => c.Nombre)
                .Select(c => new SelectListItem
                {
                    Value = c.IdCategoria.ToString(),
                    Text = c.Nombre
                })
                .ToListAsync();

            modelo.EtiquetasDisponibles = await _context.Etiquetas
                .AsNoTracking()
                .Where(e => e.Activa)
                .OrderBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.IdEtiqueta.ToString(),
                    Text = e.Nombre
                })
                .ToListAsync();


            ViewBag.IdProducto =
                producto.IdProducto;

            ViewBag.ImagenActual =
                producto.ProductoImagene?.UrlImagen;

            return View(modelo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarProducto(
    int id,
    ProductoEmprendedorViewModel modelo)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                return RedirectToAction(nameof(Panel));
            }

            Producto? producto =
                await _context.Productos
                    .Include(p => p.ProductoImagene)
                    .Include(p => p.IdEtiqueta)
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == id &&
                        p.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento);


            if (producto is null)
            {
                TempData["MensajeError"] =
                    "El producto o servicio que intentas editar no existe.";

                return RedirectToAction(nameof(MisProductos));
            }

            ValidarImagenesOpcionalesProducto(modelo);


            ModelState.Remove(nameof(modelo.Etiquetas));


            if (modelo.Estado == "Publicado")
            {
                DateOnly fechaActual =
                    DateOnly.FromDateTime(DateTime.Today);

                bool tieneSuscripcionVigente =
                    await _context.Suscripciones
                        .AsNoTracking()
                        .AnyAsync(s =>
                            s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                            s.Estado == "Activa" &&
                            s.FechaInicio <= fechaActual &&
                            s.FechaFin >= fechaActual);

                if (!tieneSuscripcionVigente)
                {
                    TempData["MensajeError"] =
                        "Necesitas una suscripción vigente para publicar productos o servicios.";

                    return RedirectToAction(nameof(MisProductos));
                }
            }

            if (modelo.TipoPublicacion == "Producto" &&
                modelo.Inventario is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Inventario),
                    "Debe indicar la cantidad disponible."
                );
            }

            if (!modelo.ConfirmaInformacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaInformacion),
                    "Debe confirmar que la información es correcta."
                );
            }


            Categoria? categoria =
                await _context.Categorias
                    .FirstOrDefaultAsync(c =>
                        c.IdCategoria == modelo.IdCategoria &&
                        c.Modulo == "Productos" &&
                        c.Activa);

            if (modelo.IdCategoria.HasValue && categoria is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdCategoria),
                    "La categoría seleccionada no es válida."
                );
            }



            if (!ModelState.IsValid)
            {
                modelo.CategoriasDisponibles = await _context.Categorias
                    .AsNoTracking()
                    .Where(c => c.Modulo == "Productos" && c.Activa)
                    .OrderBy(c => c.Nombre)
                    .Select(c => new SelectListItem
                    {
                        Value = c.IdCategoria.ToString(),
                        Text = c.Nombre
                    })
                    .ToListAsync();

                modelo.EtiquetasDisponibles = await _context.Etiquetas
                    .AsNoTracking()
                    .Where(e => e.Activa)
                    .OrderBy(e => e.Nombre)
                    .Select(e => new SelectListItem
                    {
                        Value = e.IdEtiqueta.ToString(),
                        Text = e.Nombre
                    })
                    .ToListAsync();

                ViewBag.IdProducto = producto.IdProducto;

                ViewBag.ImagenActual =
                    producto.ProductoImagene?.UrlImagen;

                return View(modelo);
            }


            // HU-70: Detectar cambios en productos favoritos.
            bool cambioInformacionProducto =
                producto.Nombre != modelo.Nombre.Trim() ||
                producto.Descripcion != modelo.Descripcion.Trim() ||
                producto.TipoPublicacion != modelo.TipoPublicacion ||
                producto.IdCategoria != categoria!.IdCategoria ||
                producto.Precio != modelo.Precio!.Value ||
                producto.StockActual !=
                    (modelo.TipoPublicacion == "Producto"
                        ? modelo.Inventario ?? 0
                        : 0) ||
                producto.Estado != modelo.Estado ||
                producto.EsDestacado != modelo.EsDestacado ||
                (modelo.Imagenes is { Count: > 0 });

            // Notificar únicamente novedades de productos publicados.
            bool hayNovedadesProducto =
                cambioInformacionProducto &&
                producto.Estado == "Publicado" &&
                modelo.Estado == "Publicado";

            producto.Nombre =
                modelo.Nombre.Trim();

            producto.Descripcion =
                modelo.Descripcion.Trim();

            producto.TipoPublicacion =
                modelo.TipoPublicacion;

            producto.IdCategoria =
    categoria!.IdCategoria;



            List<Etiqueta> etiquetasSeleccionadas =
                await _context.Etiquetas
                    .Where(e =>
                        (modelo.IdEtiquetas ?? new List<int>())
                            .Contains(e.IdEtiqueta) &&
                        e.Activa)
                    .ToListAsync();


            producto.IdEtiqueta.Clear();

            foreach (Etiqueta etiqueta in etiquetasSeleccionadas)
            {
                producto.IdEtiqueta.Add(etiqueta);
            }


            producto.Precio =
                modelo.Precio!.Value;

            producto.StockActual =
                modelo.TipoPublicacion == "Producto"
                    ? modelo.Inventario ?? 0
                    : 0;

            producto.Estado =
                modelo.Estado;

            producto.EsDestacado =
    modelo.EsDestacado;

            if (modelo.Imagenes is not null &&
                modelo.Imagenes.Count > 0)
            {
                IFormFile imagenNueva =
                    modelo.Imagenes.First();

                string extension =
                    Path.GetExtension(imagenNueva.FileName)
                        .ToLowerInvariant();

                string nombreArchivo =
                    $"{Guid.NewGuid()}{extension}";

                string carpetaProductos =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "productos"
                    );

                Directory.CreateDirectory(carpetaProductos);

                string rutaFisica =
                    Path.Combine(
                        carpetaProductos,
                        nombreArchivo
                    );

                using (FileStream stream =
                       new FileStream(rutaFisica, FileMode.Create))
                {
                    await imagenNueva.CopyToAsync(stream);
                }

                string urlImagenNueva =
                    $"/uploads/productos/{nombreArchivo}";

                if (producto.ProductoImagene is not null)
                {
                    producto.ProductoImagene.UrlImagen =
                        urlImagenNueva;

                    producto.ProductoImagene.NombreArchivo =
                        nombreArchivo;

                    producto.ProductoImagene.OrdenVisual = 1;
                    producto.ProductoImagene.EsPrincipal = true;
                }
                else
                {
                    ProductoImagene imagenProducto = new()
                    {
                        IdProducto = producto.IdProducto,
                        UrlImagen = urlImagenNueva,
                        NombreArchivo = nombreArchivo,
                        OrdenVisual = 1,
                        EsPrincipal = true
                    };

                    _context.ProductoImagenes.Add(imagenProducto);
                }
            }

            // HU-70: Notificar novedades a usuarios con este producto favorito.
            if (hayNovedadesProducto)
            {
                List<int> idsUsuariosFavoritos = await _context.Favoritos
                    .AsNoTracking()
                    .Where(f => f.IdProducto == producto.IdProducto)
                    .Select(f => f.IdUsuario)
                    .Distinct()
                    .ToListAsync();

                foreach (int idUsuarioFavorito in idsUsuariosFavoritos)
                {
                    await _notificaciones.AgregarAsync(new Notificacione
                    {
                        IdUsuario = idUsuarioFavorito,
                        Titulo = "Novedades de un producto favorito",
                        Mensaje =
        $"El producto «{producto.Nombre}» que guardaste como favorito " +
        "tiene información actualizada. Consulta sus detalles.",
                        Tipo = "Promoción",

                        // HU-70: Referencia para abrir el detalle del producto favorito.
                        TipoReferencia = "Producto",
                        IdReferencia = producto.IdProducto,

                        FechaEnvio = DateTime.Now,
                        Leida = false,
                        FechaLectura = null
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["MensajeProducto"] =
                $"El producto «{producto.Nombre}» fue actualizado correctamente.";

            return RedirectToAction(nameof(MisProductos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PausarProducto(int id)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario is null)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            var emprendimiento = await _context.Emprendimientos
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                TempData["MensajeError"] =
                    "No se encontró el emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(MisProductos));
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == id &&
                    p.IdEmprendimiento == emprendimiento.IdEmprendimiento);

            if (producto is null)
            {
                TempData["MensajeError"] =
                    "No se encontró el producto o no tienes permiso para modificarlo.";

                return RedirectToAction(nameof(MisProductos));
            }

            if (producto.Estado != "Publicado")
            {
                TempData["MensajeError"] =
                    "Solo se pueden pausar productos que estén publicados.";

                return RedirectToAction(nameof(MisProductos));
            }

            producto.Estado = "Inactivo";

            await _context.SaveChangesAsync();

            TempData["MensajeProducto"] =
                $"El producto \"{producto.Nombre}\" fue pausado correctamente.";

            return RedirectToAction(nameof(MisProductos));
        }

        // HU-18 - Reactivar un producto pausado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivarProducto(int id)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                TempData["MensajeError"] =
                    "No se encontró un emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(MisProductos));
            }

            Producto? producto =
                await _context.Productos
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == id &&
                        p.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento);

            if (producto is null)
            {
                TempData["MensajeError"] =
                    "El producto no existe o no tienes permiso para reactivarlo.";

                return RedirectToAction(nameof(MisProductos));
            }

            if (producto.Estado != "Inactivo")
            {
                TempData["MensajeError"] =
                    "Solo se pueden reactivar productos que estén inactivos.";

                return RedirectToAction(nameof(MisProductos));
            }

            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= fechaActual &&
                        s.FechaFin >= fechaActual);

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "Necesitas una suscripción vigente para reactivar productos o servicios.";

                return RedirectToAction(nameof(MisProductos));
            }

            producto.Estado = "Publicado";

            await _context.SaveChangesAsync();

            TempData["MensajeProducto"] =
                $"El producto «{producto.Nombre}» fue reactivado correctamente.";

            return RedirectToAction(nameof(MisProductos));
        }

        // HU-18 - Eliminar definitivamente un producto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            int? idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento is null)
            {
                TempData["MensajeError"] =
                    "No se encontró un emprendimiento asociado a tu cuenta.";

                return RedirectToAction(nameof(MisProductos));
            }

            Producto? producto =
                await _context.Productos
                    .Include(p => p.ProductoImagene)
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == id &&
                        p.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento);

            if (producto is null)
            {
                TempData["MensajeError"] =
                    "El producto no existe o no tienes permiso para eliminarlo.";

                return RedirectToAction(nameof(MisProductos));
            }

            // No eliminar productos que tengan ventas registradas.
            bool tieneVentas =
                await _context.VentaDetalles
                    .AnyAsync(v => v.IdProducto == id);

            if (tieneVentas)
            {
                TempData["MensajeError"] =
                    "No puedes eliminar definitivamente este producto porque tiene compras registradas.";

                return RedirectToAction(nameof(MisProductos));
            }

            // Comprobar si tiene otras relaciones que impidan eliminarlo.
            bool tieneOtrasRelaciones =
                await _context.CarritoDetalles.AnyAsync(x => x.IdProducto == id) ||
                await _context.ComentariosResenas.AnyAsync(x => x.IdProducto == id) ||
                await _context.Favoritos.AnyAsync(x => x.IdProducto == id) ||
                await _context.Galerias.AnyAsync(x => x.IdProducto == id) ||
                await _context.MovimientosInventarios.AnyAsync(x => x.IdProducto == id);

            if (tieneOtrasRelaciones)
            {
                TempData["MensajeError"] =
                    "No se puede eliminar este producto porque tiene información relacionada.";

                return RedirectToAction(nameof(MisProductos));
            }

            string nombreProducto = producto.Nombre;

            string? nombreImagen =
                producto.ProductoImagene?.NombreArchivo;

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Eliminar primero la imagen registrada en SQL.
                if (producto.ProductoImagene is not null)
                {
                    _context.ProductoImagenes.Remove(
                        producto.ProductoImagene
                    );
                }

                // Eliminar el producto.
                _context.Productos.Remove(producto);

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                await transaccion.RollbackAsync();

                TempData["MensajeError"] =
                    "No se pudo eliminar el producto porque tiene registros relacionados.";

                return RedirectToAction(nameof(MisProductos));
            }

            // Eliminar el archivo físico solamente después
            // de confirmar la eliminación en SQL.
            if (!string.IsNullOrWhiteSpace(nombreImagen))
            {
                string carpetaProductos =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "productos"
                    );

                string rutaImagen =
                    Path.Combine(
                        carpetaProductos,
                        Path.GetFileName(nombreImagen)
                    );

                try
                {
                    if (System.IO.File.Exists(rutaImagen))
                    {
                        System.IO.File.Delete(rutaImagen);
                    }
                }
                catch (IOException)
                {
                    // La eliminación en SQL ya fue confirmada.
                    // No interrumpimos la respuesta por el archivo.
                }
                catch (UnauthorizedAccessException)
                {
                    // El archivo no pudo eliminarse por permisos.
                }
            }

            TempData["MensajeProducto"] =
                $"El producto «{nombreProducto}» fue eliminado correctamente.";

            return RedirectToAction(nameof(MisProductos));
        }

        private void ValidarImagenesOpcionalesProducto(
    ProductoEmprendedorViewModel modelo)
        {
            if (modelo.Imagenes is null ||
                modelo.Imagenes.Count == 0)
            {
                return;
            }

            if (modelo.Imagenes.Count > 5)
            {
                ModelState.AddModelError(
                    nameof(modelo.Imagenes),
                    "Puede seleccionar un máximo de cinco imágenes."
                );
            }

            string[] extensionesPermitidas =
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            const long tamanoMaximo =
                5 * 1024 * 1024;

            foreach (IFormFile imagen in modelo.Imagenes)
            {
                string extension =
                    Path.GetExtension(imagen.FileName)
                        .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Todas las imágenes deben ser JPG, JPEG, PNG o WEBP."
                    );

                    break;
                }

                if (imagen.Length > tamanoMaximo)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Imagenes),
                        "Cada imagen puede pesar como máximo 5 MB."
                    );

                    break;
                }
            }
        }


        private async Task<InventarioEmprendedorViewModel> CargarInventarioAsync(
            int idUsuario)
        {
            InventarioEmprendedorViewModel modelo = new();

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario);

            if (emprendimiento is null)
            {
                return modelo;
            }

            var productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.ProductoImagene)
                .Where(p =>
                    p.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    p.TipoPublicacion == "Producto" &&
                    p.Estado == "Publicado")
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            modelo.Productos = productos.Select(p => new ProductoInventarioViewModel
            {
                IdProducto = p.IdProducto,
                Nombre = p.Nombre,
                Categoria = p.IdCategoriaNavigation?.Nombre ?? "Sin categoría",
                ImagenUrl = p.ProductoImagene?.UrlImagen ?? "/images/logo.jpg",
                StockActual = p.StockActual,
                EstadoInventario = p.StockActual == 0
                    ? "agotado"
                    : p.StockActual <= 3
                        ? "bajo"
                        : "disponible"
            }).ToList();

            modelo.UnidadesDisponibles = productos.Sum(p => p.StockActual);

            modelo.ProductosDisponibles = productos.Count(p =>
                p.StockActual > 0);

            modelo.ProductosInventarioBajo = productos.Count(p =>
                p.StockActual > 0 && p.StockActual <= 3);

            modelo.ProductosAgotados = productos.Count(p =>
                p.StockActual == 0);

            return modelo;
        }



        [HttpGet]
        public async Task<IActionResult> Inventario()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            InventarioEmprendedorViewModel modelo =
                await CargarInventarioAsync(idUsuario.Value);

            return View(modelo);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustarInventario(
            [Bind(Prefix = "Ajuste")] AjustarInventarioViewModel modelo)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            if (!modelo.ConfirmaAjuste)
            {
                ModelState.AddModelError(
                    "Ajuste.ConfirmaAjuste",
                    "Debe confirmar el ajuste de inventario.");
            }

            if (!ModelState.IsValid)
            {
                InventarioEmprendedorViewModel vista =
                    await CargarInventarioAsync(idUsuario.Value);

                vista.Ajuste = modelo;

                ViewBag.AbrirModalInventario = true;

                return View("Inventario", vista);
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == modelo.IdProducto &&
                    p.IdEmprendimientoNavigation.IdUsuarioPropietario == idUsuario.Value &&
                    p.TipoPublicacion == "Producto" &&
                    p.Estado == "Publicado");

            if (producto == null)
            {
                ModelState.AddModelError(
                    "Ajuste.IdProducto",
                    "No se encontró el producto o no pertenece a tu emprendimiento.");

                InventarioEmprendedorViewModel vista =
                    await CargarInventarioAsync(idUsuario.Value);

                vista.Ajuste = modelo;

                ViewBag.AbrirModalInventario = true;

                return View("Inventario", vista);
            }

            int stockAnterior = producto.StockActual;
            int stockNuevo = modelo.NuevaCantidad!.Value;

            producto.StockActual = stockNuevo;

            _context.MovimientosInventarios.Add(new MovimientosInventario
            {
                IdProducto = producto.IdProducto,
                IdUsuario = idUsuario.Value,
                TipoMovimiento = "Ajuste",
                Cantidad = Math.Abs(stockNuevo - stockAnterior),
                StockAnterior = stockAnterior,
                StockNuevo = stockNuevo,
                Observacion = modelo.Observacion,
                FechaMovimiento = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["MensajeInventario"] =
                $"El inventario de «{producto.Nombre}» fue actualizado a " +
                $"{stockNuevo} unidades.";

            return RedirectToAction(nameof(Inventario));
        }


        [HttpGet]
        public async Task<IActionResult> Ventas()
        {
            await CargarVentasEmprendimientoAsync();

            return View(new ActualizarVentaViewModel());
        }


        private async Task CargarVentasEmprendimientoAsync()
        {
            int idUsuario =
                HttpContext.Session.GetInt32("IdUsuario")!.Value;

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario);

            if (emprendimiento is null)
            {
                ViewBag.Ventas = new List<VentaEmprendedorViewModel>();
                return;
            }

            var detalles = await _context.VentaDetalles
                .AsNoTracking()
                .Where(vd =>
                    vd.IdProductoNavigation.IdEmprendimiento ==
                    emprendimiento.IdEmprendimiento)
                .Select(vd => new
                {
                    vd.IdVenta,
                    vd.IdVentaNavigation.NumeroOrden,
                    vd.IdVentaNavigation.FechaVenta,
                    vd.IdVentaNavigation.Estado,
                    vd.IdVentaNavigation.MetodoPago,
                    Nombre = vd.IdVentaNavigation.IdUsuarioNavigation.Nombre,
                    Apellido = vd.IdVentaNavigation.IdUsuarioNavigation.ApellidoPaterno,
                    Correo = vd.IdVentaNavigation.IdUsuarioNavigation.Correo,
                    Producto = vd.IdProductoNavigation.Nombre,
                    vd.Cantidad,
                    Monto = vd.Cantidad * vd.PrecioUnitario
                })
                .ToListAsync();

            ViewBag.Ventas = detalles
                .GroupBy(d => d.IdVenta)
                .Select(g => new VentaEmprendedorViewModel
                {
                    IdVenta = g.Key,
                    NumeroOrden = g.First().NumeroOrden,
                    NombreComprador =
                        g.First().Nombre + " " + g.First().Apellido,
                    CorreoComprador = g.First().Correo,
                    FechaVenta = g.First().FechaVenta,
                    Estado = g.First().Estado,
                    MetodoPago = g.First().MetodoPago,
                    CantidadProductos = g.Sum(x => x.Cantidad),
                    Productos = string.Join(
                        ", ",
                        g.Select(x => x.Cantidad > 1
                            ? $"{x.Producto} (x{x.Cantidad})"
                            : x.Producto)),
                    Monto = g.Sum(x => x.Monto)
                })
                .OrderByDescending(v => v.FechaVenta)
                .ToList();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEstadoVenta(
    ActualizarVentaViewModel modelo)
        {
            string[] estadosPermitidos =
            {
                "Pendiente", "Pagada", "En preparacion",
                "Enviada", "Entregada", "Cancelada"
            };

            if (!modelo.ConfirmaCambio)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaCambio),
                    "Debe confirmar el cambio de estado de la venta."
                );
            }

            if (!string.IsNullOrWhiteSpace(modelo.NuevoEstado) &&
                !estadosPermitidos.Contains(modelo.NuevoEstado))
            {
                ModelState.AddModelError(
                    nameof(modelo.NuevoEstado),
                    "El estado seleccionado no es válido."
                );
            }

            if (modelo.NuevoEstado == "Cancelada" &&
                string.IsNullOrWhiteSpace(modelo.Observacion))
            {
                ModelState.AddModelError(
                    nameof(modelo.Observacion),
                    "Debe indicar el motivo de la cancelación en la observación."
                );
            }

            int idUsuario =
                HttpContext.Session.GetInt32("IdUsuario")!.Value;

            Emprendimiento? emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario);

            Venta? venta = emprendimiento is null
                ? null
                : await _context.Ventas
                    .Include(v => v.VentasCancelada)
                    .FirstOrDefaultAsync(v =>
                        v.IdVenta == modelo.IdVenta &&
                        v.VentaDetalles.Any(d =>
                            d.IdProductoNavigation.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento));

            if (venta is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La venta seleccionada no existe o no pertenece a su emprendimiento."
                );
            }
            else if (venta.Estado == "Cancelada")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La venta ya fue cancelada y no puede modificarse."
                );
            }

            else if (venta.MetodoPago == "SINPE Móvil" &&
                     modelo.NuevoEstado == "Cancelada")
            {
                ModelState.AddModelError(
                    nameof(modelo.NuevoEstado),
                    "Las cancelaciones de pedidos SINPE Móvil deben gestionarse desde administración."
                );
            }
            else if (venta.MetodoPago == "SINPE Móvil" &&
                     venta.EstadoPago != "Aprobado" &&
                     modelo.NuevoEstado != "Pendiente")
            {
                ModelState.AddModelError(
                    nameof(modelo.NuevoEstado),
                    "El administrador debe aprobar el pago SINPE Móvil antes de avanzar el pedido."
                );
            }



            else if (modelo.NuevoEstado == "Cancelada" &&
         await _context.VentaDetalles
             .AnyAsync(d =>
                 d.IdVenta == venta.IdVenta &&
                 d.IdProductoNavigation.IdEmprendimiento !=
                     emprendimiento!.IdEmprendimiento))
            {
                ModelState.AddModelError(
                    nameof(modelo.NuevoEstado),
                    "Esta compra incluye productos de otros emprendimientos. " +
                    "No puede cancelarse toda la orden desde este emprendimiento."
                );
            }

            else if (venta.Estado == modelo.NuevoEstado)
            {
                ModelState.AddModelError(
                    nameof(modelo.NuevoEstado),
                    "La venta ya se encuentra en ese estado."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AbrirModalVenta = true;

                await CargarVentasEmprendimientoAsync();

                return View(
                    "Ventas",
                    modelo
                );
            }

            venta!.Estado = modelo.NuevoEstado;

            if (modelo.NuevoEstado == "Cancelada")
            {
                venta.VentasCancelada = new VentasCancelada
                {
                    MotivoCancelacion = modelo.Observacion!.Trim(),
                    FechaCancelacion = DateTime.Now
                };

                var puntosAcumulados = await _context.MovimientosPuntos
                    .AsNoTracking()
                    .Where(m =>
                        m.IdVenta == venta.IdVenta &&
                        m.TipoMovimiento == "Acumulacion")
                    .SumAsync(m => (int?)m.Puntos) ?? 0;

                var existeReversion = await _context.MovimientosPuntos
                    .AnyAsync(m =>
                        m.IdVenta == venta.IdVenta &&
                        m.TipoMovimiento == "Reversion");

                if (puntosAcumulados > 0 && !existeReversion)
                {
                    var movimientoReversion = new MovimientosPunto
                    {
                        IdUsuario = venta.IdUsuario,
                        TipoMovimiento = "Reversion",
                        Puntos = -puntosAcumulados,
                        Motivo = "Reversión de puntos por cancelación de la compra "
                            + venta.NumeroOrden,
                        IdVenta = venta.IdVenta,
                        FechaMovimiento = DateTime.Now
                    };

                    _context.MovimientosPuntos.Add(movimientoReversion);
                }
            }

            string estadoTexto =
                modelo.NuevoEstado == "En preparacion"
                    ? "En preparación"
                    : modelo.NuevoEstado;

            string mensaje =
                $"Tu pedido {venta.NumeroOrden} cambió al estado «{estadoTexto}»." +
                (string.IsNullOrWhiteSpace(modelo.Observacion)
                    ? string.Empty
                    : $" Observación del emprendedor: {modelo.Observacion.Trim()}");

            await _notificaciones.AgregarAsync(new Notificacione            {
                IdUsuario = venta.IdUsuario,
                Titulo = "Estado de tu pedido actualizado",
                Mensaje = mensaje,
                Tipo = "Compra",
                FechaEnvio = DateTime.Now,
                Leida = false,
                FechaLectura = null
            });

            await _context.SaveChangesAsync();

            TempData["MensajeVenta"] =
                $"La orden {venta.NumeroOrden} fue actualizada al estado " +
                $"«{estadoTexto}» y se notificó al comprador.";

            return RedirectToAction(nameof(Ventas));
        }

        [HttpGet]
        public async Task<IActionResult> Estadisticas(int periodo = 30)
        {
            if (periodo != 7 && periodo != 30 && periodo != 90 && periodo != 365)
            {
                periodo = 30;
            }

            int idUsuario = HttpContext.Session.GetInt32("IdUsuario")!.Value;
            DateTime ahora = DateTime.Now;
            DateTime desde = ahora.AddDays(-periodo);
            DateTime desdeAnterior = desde.AddDays(-periodo);

            static double Variacion(decimal actual, decimal anterior)
            {
                if (anterior == 0)
                {
                    return actual > 0 ? 100 : 0;
                }

                return Math.Round((double)((actual - anterior) / anterior * 100), 1);
            }

            static decimal TopeEscala(decimal maximo)
            {
                if (maximo <= 0)
                {
                    return 1000m;
                }

                double magnitud = Math.Pow(10, Math.Floor(Math.Log10((double)maximo)));

                return (decimal)(Math.Ceiling((double)maximo / magnitud) * magnitud);
            }

            string descripcion = periodo switch
            {
                7 => "los últimos 7 días.",
                30 => "los últimos 30 días.",
                90 => "los últimos 3 meses.",
                _ => "el último año."
            };

            EstadisticasEmprendedorViewModel modelo = new()
            {
                Periodo = periodo,
                DescripcionPeriodo = "Ingresos obtenidos durante " + descripcion
            };

            int? idEmprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.IdUsuarioPropietario == idUsuario)
                .Select(e => (int?)e.IdEmprendimiento)
                .FirstOrDefaultAsync();

            if (!idEmprendimiento.HasValue)
            {
                return View(modelo);
            }

            int idEmp = idEmprendimiento.Value;

            string[] estadosVenta = { "Pagada", "En preparacion", "Enviada", "Entregada" };

            // Ventas del período actual y del anterior.
            var filas = await _context.VentaDetalles
                .AsNoTracking()
                .Where(d =>
                    d.IdProductoNavigation.IdEmprendimiento == idEmp &&
                    estadosVenta.Contains(d.IdVentaNavigation.Estado) &&
                    d.IdVentaNavigation.FechaVenta >= desdeAnterior)
                .Select(d => new
                {
                    d.IdVenta,
                    d.IdProducto,
                    Fecha = d.IdVentaNavigation.FechaVenta,
                    IdComprador = d.IdVentaNavigation.IdUsuario,
                    Producto = d.IdProductoNavigation.Nombre,
                    Categoria = d.IdProductoNavigation.IdCategoriaNavigation == null
                        ? null
                        : d.IdProductoNavigation.IdCategoriaNavigation.Nombre,
                    Imagen = d.IdProductoNavigation.ProductoImagene == null
                        ? null
                        : d.IdProductoNavigation.ProductoImagene.UrlImagen,
                    d.Cantidad,
                    Monto = d.Subtotal ?? d.Cantidad * d.PrecioUnitario
                })
                .ToListAsync();

            var actuales = filas.Where(f => f.Fecha >= desde).ToList();
            var anteriores = filas.Where(f => f.Fecha < desde).ToList();

            modelo.Ventas = actuales.Sum(f => f.Monto);
            modelo.VentasAnterior = anteriores.Sum(f => f.Monto);
            modelo.VariacionVentas = Variacion(modelo.Ventas, modelo.VentasAnterior);

            modelo.Ordenes = actuales.Select(f => f.IdVenta).Distinct().Count();
            int ordenesAnteriores = anteriores.Select(f => f.IdVenta).Distinct().Count();
            modelo.VariacionOrdenes = Variacion(modelo.Ordenes, ordenesAnteriores);

            modelo.Unidades = actuales.Sum(f => f.Cantidad);
            int unidadesAnteriores = anteriores.Sum(f => f.Cantidad);
            modelo.VariacionUnidades = Variacion(modelo.Unidades, unidadesAnteriores);

            modelo.Clientes = actuales.Select(f => f.IdComprador).Distinct().Count();
            modelo.TicketPromedio = modelo.Ordenes > 0 ? modelo.Ventas / modelo.Ordenes : 0;

            // Favoritos del emprendimiento y de sus productos.
            IQueryable<Favorito> favoritos = _context.Favoritos
                .AsNoTracking()
                .Where(f =>
                    f.IdEmprendimiento == idEmp ||
                    (f.IdProducto != null &&
                     f.IdProductoNavigation!.IdEmprendimiento == idEmp));

            modelo.Favoritos = await favoritos.CountAsync(f => f.FechaRegistro >= desde);
            int favoritosAnteriores = await favoritos.CountAsync(f =>
                f.FechaRegistro >= desdeAnterior && f.FechaRegistro < desde);
            modelo.VariacionFavoritos = Variacion(modelo.Favoritos, favoritosAnteriores);

            // Valoración promedio general.
            List<byte?> valoraciones = await _context.ComentariosResenas
                .AsNoTracking()
                .Where(c =>
                    c.Estado != "Eliminado" &&
                    c.Valoracion != null &&
                    (c.IdEmprendimiento == idEmp ||
                     (c.IdProducto != null &&
                      c.IdProductoNavigation!.IdEmprendimiento == idEmp)))
                .Select(c => c.Valoracion)
                .ToListAsync();

            modelo.TotalValoraciones = valoraciones.Count;
            modelo.Valoracion = valoraciones.Count > 0
                ? Math.Round(valoraciones.Average(v => (double)v!.Value), 1)
                : null;

            // Productos.
            modelo.ProductosTotales = await _context.Productos
                .CountAsync(p => p.IdEmprendimiento == idEmp);

            var publicados = await _context.Productos
                .AsNoTracking()
                .Where(p => p.IdEmprendimiento == idEmp && p.Estado == "Publicado")
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.IdProducto,
                    p.Nombre,
                    p.StockActual,
                    Categoria = p.IdCategoriaNavigation == null
                        ? null
                        : p.IdCategoriaNavigation.Nombre,
                    Imagen = p.ProductoImagene == null
                        ? null
                        : p.ProductoImagene.UrlImagen
                })
                .ToListAsync();

            modelo.ProductosPublicados = publicados.Count;
            modelo.ProductosBajoStock = publicados.Count(p => p.StockActual <= 3);

            Dictionary<int, int> favoritosPorProducto = await _context.Favoritos
                .AsNoTracking()
                .Where(f =>
                    f.IdProducto != null &&
                    f.IdProductoNavigation!.IdEmprendimiento == idEmp &&
                    f.FechaRegistro >= desde)
                .GroupBy(f => f.IdProducto!.Value)
                .Select(g => new { IdProducto = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.IdProducto, x => x.Total);

            Dictionary<int, (int Unidades, int Ordenes, decimal Ingresos)> ventasPorProducto =
                actuales
                    .GroupBy(f => f.IdProducto)
                    .ToDictionary(
                        g => g.Key,
                        g => (
                            g.Sum(x => x.Cantidad),
                            g.Select(x => x.IdVenta).Distinct().Count(),
                            g.Sum(x => x.Monto)));

            modelo.Productos = publicados
                .Select(p =>
                {
                    ventasPorProducto.TryGetValue(p.IdProducto, out var v);
                    favoritosPorProducto.TryGetValue(p.IdProducto, out int totalFavoritos);

                    return new ProductoEstadisticaViewModel
                    {
                        IdProducto = p.IdProducto,
                        Nombre = p.Nombre,
                        Categoria = p.Categoria,
                        ImagenUrl = p.Imagen,
                        Stock = p.StockActual,
                        Favoritos = totalFavoritos,
                        Unidades = v.Unidades,
                        Ordenes = v.Ordenes,
                        Ingresos = v.Ingresos
                    };
                })
                .OrderByDescending(p => p.Ingresos)
                .ThenBy(p => p.Nombre)
                .ToList();

            // Productos más vendidos.
            modelo.TopProductos = actuales
                .GroupBy(f => f.IdProducto)
                .Select(g => new ProductoEstadisticaViewModel
                {
                    IdProducto = g.Key,
                    Nombre = g.First().Producto,
                    Categoria = g.First().Categoria,
                    ImagenUrl = g.First().Imagen,
                    Unidades = g.Sum(x => x.Cantidad),
                    Ingresos = g.Sum(x => x.Monto)
                })
                .OrderByDescending(p => p.Unidades)
                .ThenByDescending(p => p.Ingresos)
                .Take(4)
                .ToList();

            int maximoUnidades = modelo.TopProductos.Count > 0
                ? modelo.TopProductos.Max(p => p.Unidades)
                : 1;

            foreach (ProductoEstadisticaViewModel producto in modelo.TopProductos)
            {
                producto.Porcentaje = Math.Round((double)producto.Unidades / maximoUnidades * 100);
            }

            modelo.ProductoEstrella = modelo.TopProductos.FirstOrDefault()?.Nombre;

            // Ventas por categoría.
            string[] colores = { "#7f45c6", "#e95b86", "#e6a42e", "#3cae78", "#94a3b8" };

            var porCategoria = actuales
                .GroupBy(f => f.Categoria ?? "Sin categoría")
                .Select(g => new { Nombre = g.Key, Monto = g.Sum(x => x.Monto) })
                .OrderByDescending(c => c.Monto)
                .ToList();

            List<(string Nombre, decimal Monto)> categorias = porCategoria
                .Take(4)
                .Select(c => (c.Nombre, c.Monto))
                .ToList();

            decimal otras = porCategoria.Skip(4).Sum(c => c.Monto);

            if (otras > 0)
            {
                categorias.Add(("Otras", otras));
            }

            double acumulado = 0;
            List<string> tramos = new();

            for (int i = 0; i < categorias.Count; i++)
            {
                double porcentaje = modelo.Ventas > 0
                    ? (double)(categorias[i].Monto / modelo.Ventas * 100)
                    : 0;

                tramos.Add(
                    $"{colores[i]} " +
                    $"{acumulado.ToString("0.##", CultureInfo.InvariantCulture)}% " +
                    $"{(acumulado + porcentaje).ToString("0.##", CultureInfo.InvariantCulture)}%");

                modelo.Categorias.Add(new CategoriaVentaViewModel
                {
                    Nombre = categorias[i].Nombre,
                    Color = colores[i],
                    Monto = categorias[i].Monto,
                    Porcentaje = Math.Round(porcentaje)
                });

                acumulado += porcentaje;
            }

            if (tramos.Count > 0)
            {
                modelo.GradienteCategorias = "conic-gradient(" + string.Join(", ", tramos) + ")";
            }

            // Evolución de ventas.
            int cubetas = periodo switch { 7 => 7, 30 => 10, _ => 12 };
            double diasPorCubeta = (double)periodo / cubetas;

            decimal[] serieActual = new decimal[cubetas];
            decimal[] serieAnterior = new decimal[cubetas];

            foreach (var fila in filas)
            {
                bool esActual = fila.Fecha >= desde;
                DateTime inicioBase = esActual ? desde : desdeAnterior;

                int indice = (int)Math.Floor(
                    (fila.Fecha - inicioBase).TotalDays / diasPorCubeta);

                indice = Math.Clamp(indice, 0, cubetas - 1);

                if (esActual)
                {
                    serieActual[indice] += fila.Monto;
                }
                else
                {
                    serieAnterior[indice] += fila.Monto;
                }
            }

            decimal tope = TopeEscala(Math.Max(serieActual.Max(), serieAnterior.Max()));

            for (int i = 5; i >= 0; i--)
            {
                modelo.EscalaVentas.Add("₡" + (tope * i / 5).ToString("N0"));
            }

            for (int i = 0; i < cubetas; i++)
            {
                modelo.EtiquetasVentas.Add(
                    desde.AddDays(i * diasPorCubeta).ToString("dd/MM"));
            }

            string Ruta(decimal[] serie, bool guardarPuntos)
            {
                List<string> comandos = new();

                for (int i = 0; i < serie.Length; i++)
                {
                    double x = i * 700.0 / (cubetas - 1);
                    double y = 250 - (double)(serie[i] / tope) * 230;

                    comandos.Add(
                        (i == 0 ? "M" : "L") +
                        x.ToString("0.##", CultureInfo.InvariantCulture) + "," +
                        y.ToString("0.##", CultureInfo.InvariantCulture));

                    if (guardarPuntos)
                    {
                        modelo.Puntos.Add(new PuntoGraficoEmprendedorViewModel { X = x, Y = y });
                    }
                }

                return string.Join(" ", comandos);
            }

            modelo.RutaActual = Ruta(serieActual, true);
            modelo.RutaAnterior = Ruta(serieAnterior, false);
            modelo.RutaArea = modelo.RutaActual + " L700,260 L0,260 Z";

            // Actividad reciente.
            List<ActividadEstadisticaViewModel> actividad = new();

            var ventasRecientes = await _context.VentaDetalles
                .AsNoTracking()
                .Where(d =>
                    d.IdProductoNavigation.IdEmprendimiento == idEmp &&
                    estadosVenta.Contains(d.IdVentaNavigation.Estado))
                .GroupBy(d => new
                {
                    d.IdVenta,
                    d.IdVentaNavigation.FechaVenta,
                    d.IdVentaNavigation.IdUsuarioNavigation.Nombre
                })
                .Select(g => new
                {
                    g.Key.FechaVenta,
                    g.Key.Nombre,
                    Total = g.Sum(x => x.Subtotal ?? x.Cantidad * x.PrecioUnitario)
                })
                .OrderByDescending(v => v.FechaVenta)
                .Take(3)
                .ToListAsync();

            actividad.AddRange(ventasRecientes.Select(v => new ActividadEstadisticaViewModel
            {
                Tipo = "venta",
                Icono = "bi-bag-check-fill",
                Titulo = "Nueva venta recibida",
                Detalle = $"{v.Nombre} realizó una compra por ₡{v.Total:N0}.",
                Fecha = v.FechaVenta
            }));

            var favoritosRecientes = await favoritos
                .OrderByDescending(f => f.FechaRegistro)
                .Take(2)
                .Select(f => new
                {
                    f.FechaRegistro,
                    Producto = f.IdProductoNavigation == null
                        ? null
                        : f.IdProductoNavigation.Nombre
                })
                .ToListAsync();

            actividad.AddRange(favoritosRecientes.Select(f => new ActividadEstadisticaViewModel
            {
                Tipo = "favorito",
                Icono = "bi-heart-fill",
                Titulo = "Agregado a favoritos",
                Detalle = f.Producto is null
                    ? "Tu emprendimiento fue agregado a favoritos."
                    : $"«{f.Producto}» fue agregado a favoritos.",
                Fecha = f.FechaRegistro
            }));

            var resenasRecientes = await _context.ComentariosResenas
                .AsNoTracking()
                .Where(c =>
                    c.Estado != "Eliminado" &&
                    c.Valoracion != null &&
                    (c.IdEmprendimiento == idEmp ||
                     (c.IdProducto != null &&
                      c.IdProductoNavigation!.IdEmprendimiento == idEmp)))
                .OrderByDescending(c => c.FechaPublicacion)
                .Take(2)
                .Select(c => new { c.FechaPublicacion, c.Valoracion })
                .ToListAsync();

            actividad.AddRange(resenasRecientes.Select(c => new ActividadEstadisticaViewModel
            {
                Tipo = "valoracion",
                Icono = "bi-star-fill",
                Titulo = "Nueva valoración",
                Detalle = $"Recibiste una valoración de {c.Valoracion} de 5.",
                Fecha = c.FechaPublicacion
            }));

            var participacion = await _context.ParticipacionesEventos
                .AsNoTracking()
                .Where(p => p.IdEmprendimiento == idEmp && p.Estado == "Aprobada")
                .OrderByDescending(p => p.FechaSolicitud)
                .Select(p => new
                {
                    p.FechaSolicitud,
                    Evento = p.IdEventoNavigation.Nombre
                })
                .FirstOrDefaultAsync();

            if (participacion is not null)
            {
                actividad.Add(new ActividadEstadisticaViewModel
                {
                    Tipo = "evento",
                    Icono = "bi-calendar-check-fill",
                    Titulo = "Participación aprobada",
                    Detalle = $"Tu participación en «{participacion.Evento}» fue aprobada.",
                    Fecha = participacion.FechaSolicitud
                });
            }

            modelo.Actividad = actividad
                .OrderByDescending(a => a.Fecha)
                .Take(5)
                .ToList();

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> PostularHechoEnCr()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            return View(CrearModeloPostulacion(emprendimiento));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostularHechoEnCr(
            PostulacionHechoEnCrViewModel modelo)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction("IniciarSesion", "Cuenta");
            }

            Emprendimiento? emprendimiento = await _context.Emprendimientos
                .FirstOrDefaultAsync(e =>
                    e.IdUsuarioPropietario == idUsuario.Value);

            PostulacionHechoEnCrViewModel vista =
                CrearModeloPostulacion(emprendimiento);

            vista.ConfirmaPostulacion = modelo.ConfirmaPostulacion;

            if (!vista.EstaAprobado || vista.YaPostulado)
            {
                return View(vista);
            }

            if (!modelo.ConfirmaPostulacion)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaPostulacion),
                    "Debe confirmar la postulación a Hecho en CR."
                );

                return View(vista);
            }

            emprendimiento!.ParticipaHechoEnCr = true;

            await _context.SaveChangesAsync();

            TempData["MensajePostulacion"] =
                $"«{emprendimiento.NombreComercial}» fue postulado a la iniciativa Hecho en CR correctamente.";

            return RedirectToAction(nameof(PostularHechoEnCr));
        }

        private static PostulacionHechoEnCrViewModel CrearModeloPostulacion(
            Emprendimiento? emprendimiento)
        {
            return new PostulacionHechoEnCrViewModel
            {
                NombreEmprendimiento = emprendimiento?.NombreComercial ?? string.Empty,
                EstaAprobado = emprendimiento is not null &&
                               emprendimiento.Activo &&
                               emprendimiento.EstadoAprobacion == "Aprobado",
                YaPostulado = emprendimiento?.ParticipaHechoEnCr ?? false
            };
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "IniciarSesion",
                "Cuenta"
            );
        }
    }
}