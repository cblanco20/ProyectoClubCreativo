using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProyectoClubCreativo.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;
using ProyectoClubCreativo.Services;

namespace ProyectoClubCreativo.Controllers
{
    public class EmprendedorController : Controller
    {
        private readonly ClubCreativoDbContext _context;
        private readonly IWebHostEnvironment _entornoWeb;
        private readonly PagoService _pagoService;

        public EmprendedorController(
    ClubCreativoDbContext context,
    IWebHostEnvironment entornoWeb,
    PagoService pagoService)
        {
            _context = context;
            _entornoWeb = entornoWeb;
            _pagoService = pagoService;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

            if (idUsuario is null || rolUsuario != "Emprendedor")
            {
                context.Result = RedirectToAction(
                    "IniciarSesion",
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

            await next();
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
                .Include(g => g.GaleriaImagene)
                .Where(g =>
                    g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    g.GaleriaImagene != null)
                .OrderBy(g => g.FechaCreacion)
                .Select(g => g.GaleriaImagene!.UrlImagen)
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

            ViewBag.LogoActual =
                string.IsNullOrWhiteSpace(emprendimiento.LogoUrl)
                    ? "/images/logo.jpg"
                    : emprendimiento.LogoUrl;

            ViewBag.FotografiasActuales = await _context.Galerias
                .AsNoTracking()
                .Include(g => g.GaleriaImagene)
                .Where(g =>
                    g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    g.GaleriaImagene != null)
                .OrderBy(g => g.FechaCreacion)
                .Select(g => g.GaleriaImagene!.UrlImagen)
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

                ViewBag.LogoActual =
                    string.IsNullOrWhiteSpace(emprendimiento.LogoUrl)
                        ? "/images/logo.jpg"
                        : emprendimiento.LogoUrl;

                ViewBag.FotografiasActuales = await _context.Galerias
                    .AsNoTracking()
                    .Include(g => g.GaleriaImagene)
                    .Where(g =>
                        g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        g.GaleriaImagene != null)
                    .OrderBy(g => g.FechaCreacion)
                    .Select(g => g.GaleriaImagene!.UrlImagen)
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

                ViewBag.LogoActual =
                    string.IsNullOrWhiteSpace(emprendimiento.LogoUrl)
                        ? "/images/logo.jpg"
                        : emprendimiento.LogoUrl;

                ViewBag.FotografiasActuales = await _context.Galerias
                    .AsNoTracking()
                    .Include(g => g.GaleriaImagene)
                    .Where(g =>
                        g.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        g.GaleriaImagene != null)
                    .OrderBy(g => g.FechaCreacion)
                    .Select(g => g.GaleriaImagene!.UrlImagen)
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
                nameof(PagoSuscripcion),
                new
                {
                     idPlan = plan.IdPlan
                }
            );
        }

        [HttpGet]
        public async Task<IActionResult> PagoSuscripcion(int idPlan)
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
                        p.IdPlan == idPlan &&
                        p.Activo);

            if (plan is null)
            {
                TempData["MensajeError"] =
                    "El plan seleccionado ya no se encuentra disponible.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            Suscripcione? suscripcionActual =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .Where(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa")
                    .OrderByDescending(s => s.FechaInicio)
                    .FirstOrDefaultAsync();

            PagoSuscripcionViewModel modelo = new()
            {
                IdPlan = plan.IdPlan,
                NombrePlan = plan.Nombre,
                Precio = plan.Precio,
                Periodicidad = plan.Periodicidad,
                TotalPagar = plan.Precio
            };

            if (suscripcionActual != null)
            {
                // Si seleccionó exactamente el mismo plan que ya posee,
                // no es necesario realizar otro pago.
                if (suscripcionActual.IdPlan == plan.IdPlan)
                {
                    TempData["MensajeError"] =
                        "Este es el plan que actualmente tiene activo.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }

                modelo.EsCambioPlan = true;
                modelo.NombrePlanActual =
                    suscripcionActual.IdPlanNavigation.Nombre;

                modelo.PrecioPlanActual =
                    suscripcionActual.IdPlanNavigation.Precio;

                DateOnly hoy =
                    DateOnly.FromDateTime(DateTime.Today);

                int diasTotales =
                    suscripcionActual.FechaFin.DayNumber -
                    suscripcionActual.FechaInicio.DayNumber;

                int diasRestantes =
                    suscripcionActual.FechaFin.DayNumber -
                    hoy.DayNumber;

                diasRestantes = Math.Max(
                    0,
                    Math.Min(diasRestantes, diasTotales)
                );

                decimal creditoProporcional = 0;

                if (diasTotales > 0)
                {
                    creditoProporcional =
                        suscripcionActual.IdPlanNavigation.Precio *
                        diasRestantes /
                        diasTotales;
                }

                creditoProporcional =
                    Math.Round(creditoProporcional, 2);

                modelo.CreditoProporcional =
                    creditoProporcional;

                modelo.TotalPagar =
                    Math.Max(
                        0,
                        plan.Precio - creditoProporcional
                    );
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagoSuscripcion(
    PagoSuscripcionViewModel modelo)
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

            // Consultar nuevamente el plan seleccionado.
            PlanesSuscripcion? plan =
                await _context.PlanesSuscripcions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.IdPlan == modelo.IdPlan &&
                        p.Activo);

            if (plan is null)
            {
                TempData["MensajeError"] =
                    "El plan seleccionado ya no se encuentra disponible.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            // Comprobar nuevamente el emprendimiento.
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
                    "Debe tener un emprendimiento activo y aprobado para adquirir un plan.";

                return RedirectToAction(nameof(PlanesSuscripcion));
            }

            // Buscar la suscripción activa actual.
            Suscripcione? suscripcionActual =
                await _context.Suscripciones
                    .Include(s => s.IdPlanNavigation)
                    .Where(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa")
                    .OrderByDescending(s => s.FechaInicio)
                    .FirstOrDefaultAsync();

            // Los datos visibles siempre se vuelven a obtener desde la BD.
            modelo.NombrePlan = plan.Nombre;
            modelo.Precio = plan.Precio;
            modelo.Periodicidad = plan.Periodicidad;
            modelo.TotalPagar = plan.Precio;

            // Si existe una suscripción activa, estamos ante un cambio de plan.
            if (suscripcionActual != null)
            {
                if (suscripcionActual.IdPlan == plan.IdPlan)
                {
                    TempData["MensajeError"] =
                        "Este es el plan que actualmente tiene activo.";

                    return RedirectToAction(nameof(MiSuscripcion));
                }

                modelo.EsCambioPlan = true;

                modelo.NombrePlanActual =
                    suscripcionActual.IdPlanNavigation.Nombre;

                modelo.PrecioPlanActual =
                    suscripcionActual.IdPlanNavigation.Precio;

                DateOnly hoy =
                    DateOnly.FromDateTime(DateTime.Today);

                int diasTotales =
                    suscripcionActual.FechaFin.DayNumber -
                    suscripcionActual.FechaInicio.DayNumber;

                int diasRestantes =
                    suscripcionActual.FechaFin.DayNumber -
                    hoy.DayNumber;

                diasRestantes = Math.Max(
                    0,
                    Math.Min(diasRestantes, diasTotales)
                );

                decimal creditoProporcional = 0;

                if (diasTotales > 0)
                {
                    creditoProporcional =
                        suscripcionActual.IdPlanNavigation.Precio *
                        diasRestantes /
                        diasTotales;
                }

                creditoProporcional =
                    Math.Round(creditoProporcional, 2);

                modelo.CreditoProporcional =
                    creditoProporcional;

                modelo.TotalPagar =
                    Math.Max(
                        0,
                        plan.Precio - creditoProporcional
                    );
            }

            // Validar vencimiento de la tarjeta.
            if (modelo.MesVencimiento.HasValue &&
                modelo.AnioVencimiento.HasValue)
            {
                DateTime hoy = DateTime.Today;

                if (modelo.AnioVencimiento.Value < hoy.Year ||
                    (modelo.AnioVencimiento.Value == hoy.Year &&
                     modelo.MesVencimiento.Value < hoy.Month))
                {
                    ModelState.AddModelError(
                        nameof(modelo.AnioVencimiento),
                        "La tarjeta se encuentra vencida."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Procesar el monto calculado por el servidor.
            ResultadoPago resultadoPago =
                _pagoService.ProcesarPago(
                    modelo.NumeroTarjeta,
                    modelo.TotalPagar
                );

            if (!resultadoPago.Aprobado)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultadoPago.Mensaje
                );

                modelo.NumeroTarjeta = string.Empty;
                modelo.Cvv = string.Empty;

                return View(modelo);
            }

            DateOnly fechaInicio =
                DateOnly.FromDateTime(DateTime.Today);

            DateOnly fechaFin;

            if (plan.Periodicidad.Equals(
                "Anual",
                StringComparison.OrdinalIgnoreCase))
            {
                fechaFin = fechaInicio.AddYears(1);
            }
            else if (plan.Periodicidad.Equals(
                "Trimestral",
                StringComparison.OrdinalIgnoreCase))
            {
                fechaFin = fechaInicio.AddMonths(3);
            }
            else
            {
                fechaFin = fechaInicio.AddMonths(1);
            }

            // Si es un cambio de plan, cerrar la suscripción anterior
            // únicamente después de que el pago haya sido aprobado.
            if (suscripcionActual != null)
            {
                suscripcionActual.Estado = "Cancelada";
            }

            Suscripcione nuevaSuscripcion = new()
            {
                IdEmprendimiento = emprendimiento.IdEmprendimiento,

                IdPlan = plan.IdPlan,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = "Activa",
                RenovacionAutomatica = true
            };

            _context.Suscripciones.Add(nuevaSuscripcion);

            // Guardar el método de pago únicamente si el usuario
            // seleccionó la opción y el pago fue aprobado.
            if (modelo.GuardarMetodoPago)
            {
                // Desactivar cualquier método de pago anterior
                // del mismo emprendimiento.
                var metodosAnteriores =
                    await _context.MetodosPagoSuscripcion
                        .Where(m =>
                            m.IdEmprendimiento ==
                                emprendimiento.IdEmprendimiento &&
                            m.Activo)
                        .ToListAsync();

                foreach (var metodoAnterior in metodosAnteriores)
                {
                    metodoAnterior.Activo = false;
                }

                MetodoPagoSuscripcion metodoPago = new()
                {
                    IdEmprendimiento =
                        emprendimiento.IdEmprendimiento,

                    UltimosCuatro =
                        modelo.NumeroTarjeta[^4..],

                    MesVencimiento =
                        modelo.MesVencimiento!.Value,

                    AnioVencimiento =
                        modelo.AnioVencimiento!.Value,

                    EstadoSimulado = "Aprobado",

                    Activo = true
                };

                _context.MetodosPagoSuscripcion.Add(metodoPago);
            }


            await _context.SaveChangesAsync();

            if (suscripcionActual != null)
            {
                TempData["MensajeSuscripcion"] =
                    "El pago fue procesado y el cambio de plan se realizó correctamente.";
            }
            else
            {
                TempData["MensajeSuscripcion"] =
                    "El pago fue procesado y tu suscripción fue activada correctamente.";
            }

            return RedirectToAction(nameof(MiSuscripcion));
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

            var suscripcion = await _context.Suscripciones
                .AsNoTracking()
                .Include(s => s.IdPlanNavigation)
                .Where(s =>
                    s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                    s.Estado == "Activa")
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();

            MiSuscripcionViewModel modelo = new();

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
            .Include(s => s.IdPlanNavigation)
            .Where(s =>
                s.IdEmprendimiento == emprendimiento.IdEmprendimiento)
            .OrderByDescending(s => s.FechaInicio)
            .Select(s => new MovimientoSuscripcionViewModel
            {
                Fecha = s.FechaInicio,
                Descripcion = "Activación del " + s.IdPlanNavigation.Nombre,
                Monto = s.IdPlanNavigation.Precio,
                Estado = s.Estado
            })
            .ToListAsync();

            modelo.Historial = historialSuscripciones;

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> ActualizarMetodoPago()
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
                    "No se encontró el emprendimiento.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "No se encontró una suscripción activa.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            PagoSuscripcionViewModel modelo = new()
            {
                IdPlan = suscripcion.IdPlan,
                NombrePlan = suscripcion.IdPlanNavigation.Nombre,
                Precio = suscripcion.IdPlanNavigation.Precio,
                Periodicidad =
                    suscripcion.IdPlanNavigation.Periodicidad
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarMetodoPago(
    PagoSuscripcionViewModel modelo)
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
                    "No se encontró el emprendimiento.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "No se encontró una suscripción activa.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            // Estos datos siempre se recuperan desde la BD
            // y no se confía en los valores enviados por el navegador.
            modelo.IdPlan = suscripcion.IdPlan;
            modelo.NombrePlan =
                suscripcion.IdPlanNavigation.Nombre;
            modelo.Precio =
                suscripcion.IdPlanNavigation.Precio;
            modelo.Periodicidad =
                suscripcion.IdPlanNavigation.Periodicidad;

            // Validar el vencimiento de la nueva tarjeta.
            if (modelo.MesVencimiento.HasValue &&
                modelo.AnioVencimiento.HasValue)
            {
                DateTime hoy = DateTime.Today;

                if (modelo.AnioVencimiento.Value < hoy.Year ||
                    (modelo.AnioVencimiento.Value == hoy.Year &&
                     modelo.MesVencimiento.Value < hoy.Month))
                {
                    ModelState.AddModelError(
                        nameof(modelo.AnioVencimiento),
                        "La tarjeta se encuentra vencida."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Validar la nueva tarjeta mediante
            // la misma pasarela simulada del proyecto.
            ResultadoPago resultado =
                _pagoService.ProcesarPago(
                    modelo.NumeroTarjeta,
                    suscripcion.IdPlanNavigation.Precio
                );

            if (!resultado.Aprobado)
            {
                ModelState.AddModelError(
                    string.Empty,
                    resultado.Mensaje
                );

                modelo.NumeroTarjeta = string.Empty;
                modelo.Cvv = string.Empty;

                return View(modelo);
            }

            // Desactivar cualquier método anterior.
            List<MetodoPagoSuscripcion> metodosAnteriores =
                await _context.MetodosPagoSuscripcion
                    .Where(m =>
                        m.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        m.Activo)
                    .ToListAsync();

            foreach (MetodoPagoSuscripcion metodoAnterior
                in metodosAnteriores)
            {
                metodoAnterior.Activo = false;
            }

            // Registrar el nuevo método.
            MetodoPagoSuscripcion nuevoMetodo = new()
            {
                IdEmprendimiento =
                    emprendimiento.IdEmprendimiento,

                UltimosCuatro =
                    modelo.NumeroTarjeta[^4..],

                MesVencimiento =
                    modelo.MesVencimiento!.Value,

                AnioVencimiento =
                    modelo.AnioVencimiento!.Value,

                EstadoSimulado = "Aprobado",

                Activo = true
            };

            _context.MetodosPagoSuscripcion.Add(nuevoMetodo);

            await _context.SaveChangesAsync();

            TempData["MensajeSuscripcion"] =
                "El método de pago fue actualizado correctamente. " +
                "Ya puedes volver a intentar la renovación de tu suscripción.";

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

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "No tienes una suscripción activa para cancelar.";

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
        public async Task<IActionResult> ReactivarSuscripcion()
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

            var emprendimiento =
                await _context.Emprendimientos
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento == null)
            {
                return RedirectToAction(nameof(Panel));
            }

            var suscripcion =
                await _context.Suscripciones
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion == null)
            {
                TempData["MensajeError"] =
                    "No se encontró una suscripción activa.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            var cancelacion =
                await _context.SuscripcionCancelaciones
                    .FirstOrDefaultAsync(c =>
                        c.IdSuscripcion == suscripcion.IdSuscripcion);

            if (cancelacion == null)
            {
                TempData["MensajeError"] =
                    "La suscripción no tiene una cancelación programada.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            if (suscripcion.FechaFin <=
                DateOnly.FromDateTime(DateTime.Today))
            {
                TempData["MensajeError"] =
                    "La suscripción ya alcanzó la fecha de finalización y no puede reactivarse.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            _context.SuscripcionCancelaciones.Remove(cancelacion);

            suscripcion.RenovacionAutomatica = true;

            await _context.SaveChangesAsync();

            TempData["MensajeSuscripcion"] =
                "La suscripción fue reactivada correctamente. La renovación continuará activa.";

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
                return RedirectToAction(
                    "IniciarSesion",
                    "Cuenta"
                );
            }

            var emprendimiento =
                await _context.Emprendimientos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.IdUsuarioPropietario == idUsuario.Value);

            if (emprendimiento == null)
            {
                return RedirectToAction(nameof(Panel));
            }

            var suscripcion =
                await _context.Suscripciones
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento == emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion == null)
            {
                TempData["MensajeError"] =
                    "No se encontró una suscripción activa.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            if (suscripcion.FechaFin <=
                DateOnly.FromDateTime(DateTime.Today))
            {
                TempData["MensajeError"] =
                    "La suscripción ya alcanzó su fecha de vencimiento.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            suscripcion.RenovacionAutomatica =
                !suscripcion.RenovacionAutomatica;

            await _context.SaveChangesAsync();

            if (suscripcion.RenovacionAutomatica)
            {
                TempData["MensajeSuscripcion"] =
                    "La renovación automática fue activada correctamente.";
            }
            else
            {
                TempData["MensajeSuscripcion"] =
                    "La renovación automática fue desactivada. Tu plan permanecerá activo hasta finalizar el período actual.";
            }

            return RedirectToAction(nameof(MiSuscripcion));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarRenovacionesAutomaticas()
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
                    "No se encontró el emprendimiento.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .Include(s => s.IdPlanNavigation)
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "No se encontró una suscripción activa.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            DateOnly hoy =
    DateOnly.FromDateTime(DateTime.Today);

            // Si la renovación automática fue desactivada,
            // no se realiza ningún cobro.
            if (!suscripcion.RenovacionAutomatica)
            {
                // Si ya llegó la fecha de vencimiento,
                // la suscripción pasa a estado Vencida.
                if (suscripcion.FechaFin <= hoy)
                {
                    suscripcion.Estado = "Vencida";

                    await _context.SaveChangesAsync();

                    TempData["MensajeSuscripcion"] =
                        "La suscripción llegó a su fecha de vencimiento. " +
                        "No se realizó ningún cobro porque la renovación automática estaba desactivada.";
                }
                else
                {
                    TempData["MensajeError"] =
                        $"La renovación automática está desactivada. " +
                        $"Tu plan permanecerá activo hasta el " +
                        $"{suscripcion.FechaFin:dd/MM/yyyy}.";
                }

                return RedirectToAction(nameof(MiSuscripcion));
            }

            if (suscripcion.FechaFin > hoy)
            {
                TempData["MensajeError"] =
                    $"La suscripción todavía no requiere renovación. " +
                    $"Su fecha de vencimiento es el " +
                    $"{suscripcion.FechaFin:dd/MM/yyyy}.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            MetodoPagoSuscripcion? metodoPago =
                await _context.MetodosPagoSuscripcion
                    .FirstOrDefaultAsync(m =>
                        m.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        m.Activo);

            if (metodoPago is null)
            {
                TempData["MensajeError"] =
                    "No existe un método de pago registrado para realizar la renovación automática.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            ResultadoPago resultado =
                _pagoService.ProcesarRenovacionAutomatica(
                    metodoPago.EstadoSimulado,
                    suscripcion.IdPlanNavigation.Precio
                );

            if (!resultado.Aprobado)
            {
                DateTime fechaLimite =
                    DateTime.Now.AddDays(3);

                Notificacione notificacion = new()
                {
                    IdUsuario = idUsuario.Value,

                    Titulo = "Fallo en la renovación de la suscripción",

                    Mensaje =
                        $"No fue posible procesar la renovación automática de tu plan. " +
                        $"Actualiza tu método de pago antes del " +
                        $"{fechaLimite:dd/MM/yyyy} para evitar perder el acceso.",

                    Tipo = "Suscripcion",

                    FechaEnvio = DateTime.Now,

                    Leida = false,

                    FechaLectura = null
                };

                _context.Notificaciones.Add(notificacion);

                await _context.SaveChangesAsync();

                TempData["MensajeError"] =
                    $"No fue posible procesar la renovación automática. " +
                    $"Debes actualizar tu método de pago antes del " +
                    $"{fechaLimite:dd/MM/yyyy}.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            // Extender la vigencia del mismo plan.
            if (suscripcion.IdPlanNavigation.Periodicidad.Equals(
                "Anual",
                StringComparison.OrdinalIgnoreCase))
            {
                suscripcion.FechaFin =
                    suscripcion.FechaFin.AddYears(1);
            }
            else if (suscripcion.IdPlanNavigation.Periodicidad.Equals(
                "Trimestral",
                StringComparison.OrdinalIgnoreCase))
            {
                suscripcion.FechaFin =
                    suscripcion.FechaFin.AddMonths(3);
            }
            else
            {
                suscripcion.FechaFin =
                    suscripcion.FechaFin.AddMonths(1);
            }

            await _context.SaveChangesAsync();

            TempData["MensajeSuscripcion"] =
                $"La suscripción se renovó automáticamente hasta el " +
                $"{suscripcion.FechaFin:dd/MM/yyyy}.";

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

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "Necesitas una suscripción activa para publicar productos o servicios.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            int limiteProductos =
                suscripcion.IdPlanNavigation.Nombre switch
                {
                    "Plan Básico" => 10,
                    "Plan Emprendedor" => 20,
                    "Plan Premium" => 35,
                    _ => 10
                };

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

            return View(new ProductoEmprendedorViewModel
            {
                TipoPublicacion = "Producto",
                Estado = "Publicado",
                Inventario = 1
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

            Suscripcione? suscripcion =
                await _context.Suscripciones
                    .AsNoTracking()
                    .Include(s => s.IdPlanNavigation)
                    .FirstOrDefaultAsync(s =>
                        s.IdEmprendimiento ==
                            emprendimiento.IdEmprendimiento &&
                        s.Estado == "Activa");

            if (suscripcion is null)
            {
                TempData["MensajeError"] =
                    "Necesitas una suscripción activa para publicar productos o servicios.";

                return RedirectToAction(nameof(MiSuscripcion));
            }

            int limiteProductos =
                suscripcion.IdPlanNavigation.Nombre switch
                {
                    "Plan Básico" => 10,
                    "Plan Emprendedor" => 20,
                    "Plan Premium" => 35,
                    _ => 10
                };

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

            Categoria? categoria =
                await _context.Categorias
                    .FirstOrDefaultAsync(c =>
                        c.Nombre == modelo.Categoria &&
                        c.Modulo == "Emprendimientos" &&
                        c.Activa);

            if (categoria is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Categoria),
                    "La categoría seleccionada no es válida."
                );
            }

            if (!ModelState.IsValid)
            {
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

                Categoria =
                    producto.IdCategoriaNavigation?.Nombre
                    ?? string.Empty,

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
                        c.Nombre == modelo.Categoria &&
                        c.Modulo == "Emprendimientos" &&
                        c.Activa);

            if (categoria is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Categoria),
                    "La categoría seleccionada no es válida."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IdProducto = producto.IdProducto;

                ViewBag.ImagenActual =
                    producto.ProductoImagene?.UrlImagen;

                return View(modelo);
            }

            producto.Nombre =
                modelo.Nombre.Trim();

            producto.Descripcion =
                modelo.Descripcion.Trim();

            producto.TipoPublicacion =
                modelo.TipoPublicacion;

            producto.IdCategoria =
                categoria!.IdCategoria;

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

        [HttpGet]
        public IActionResult Inventario()
        {
            return View(new AjustarInventarioViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AjustarInventario(
            AjustarInventarioViewModel modelo)
        {
            if (!modelo.ConfirmaAjuste)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaAjuste),
                    "Debe confirmar el ajuste de inventario."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AbrirModalInventario = true;

                return View(
                    "Inventario",
                    modelo
                );
            }

            TempData["MensajeInventario"] =
                $"El inventario de «{modelo.NombreProducto}» fue actualizado a " +
                $"{modelo.NuevaCantidad} unidades.";

            return RedirectToAction(nameof(Inventario));
        }

        [HttpGet]
        public IActionResult Ventas()
        {
            return View(new ActualizarVentaViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarEstadoVenta(
            ActualizarVentaViewModel modelo)
        {
            if (!modelo.ConfirmaCambio)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaCambio),
                    "Debe confirmar el cambio de estado de la venta."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AbrirModalVenta = true;

                return View(
                    "Ventas",
                    modelo
                );
            }

            TempData["MensajeVenta"] =
                $"La orden {modelo.NumeroOrden} fue actualizada al estado " +
                $"«{modelo.NuevoEstado}» correctamente.";

            return RedirectToAction(nameof(Ventas));
        }

        [HttpGet]
        public IActionResult ParticipacionEventos()
        {
            return View(new SolicitudEventoViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SolicitarParticipacionEvento(
            SolicitudEventoViewModel modelo)
        {
            if (!modelo.ConfirmaSolicitud)
            {
                ModelState.AddModelError(
                    nameof(modelo.ConfirmaSolicitud),
                    "Debe confirmar que desea enviar la solicitud."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AbrirModalEvento = true;

                return View(
                    "ParticipacionEventos",
                    modelo
                );
            }

            TempData["MensajeEvento"] =
                $"La solicitud para participar en «{modelo.NombreEvento}» " +
                "fue enviada correctamente.";

            return RedirectToAction(nameof(ParticipacionEventos));
        }

        public IActionResult Estadisticas()
        {
            return View();
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