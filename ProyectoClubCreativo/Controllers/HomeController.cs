using ProyectoClubCreativo.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models;
using ProyectoClubCreativo.Models.ViewModels;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;
using ProyectoClubCreativo.Services;

namespace ProyectoClubCreativo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ClubCreativoDbContext _context;
        private readonly PromocionesService _promociones;

        public HomeController(
            ILogger<HomeController> logger,
            ClubCreativoDbContext context,
            PromocionesService promociones)
        {
            _logger = logger;
            _context = context;
            _promociones = promociones;
        }

        public async Task<IActionResult> Index()
        {
            DateTime ahora = DateTime.Now;

            var eventos = await _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    (e.Estado == "Programado" || e.Estado == "Activo") &&
                    (e.FechaFin ?? e.FechaInicio) >= ahora)
                .OrderBy(e => e.FechaInicio)
                .Take(3)
                .Select(e => new
                {
                    e.IdEvento,
                    e.Nombre,
                    e.Descripcion,
                    e.Ubicacion,
                    e.ImagenUrl,
                    e.FechaInicio,
                    e.FechaFin
                })
                .ToListAsync();

            var emprendimientos = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.EstadoAprobacion == "Aprobado" && e.Activo)
                .Select(e => new
                {
                    e.IdEmprendimiento,
                    e.NombreComercial,
                    e.Descripcion,
                    e.LogoUrl,
                    Categoria = e.IdCategoriaNavigation == null
                        ? null
                        : e.IdCategoriaNavigation.Nombre,
                    ImagenPrincipal = e.EmprendimientoImagene == null
                        ? null
                        : e.EmprendimientoImagene.UrlImagen,
                    Valoraciones = e.ComentariosResenas
                        .Where(c => c.Estado != "Eliminado" && c.Valoracion != null)
                        .Select(c => (int)c.Valoracion!.Value)
                        .ToList()
                })
                .ToListAsync();

            List<PromocionPublicaViewModel> promociones =
                await ConsultarPromocionesPublicasAsync();

            List<string> imagenesComunidad = await _context.GaleriaImagenes
                .AsNoTracking()
                .Where(i => i.IdGaleriaNavigation.Estado == "Publicada")
                .OrderByDescending(i => i.IdGaleriaNavigation.FechaCreacion)
                .ThenBy(i => i.OrdenVisual)
                .Select(i => i.UrlImagen)
                .Take(6)
                .ToListAsync();

            return View(new InicioViewModel
            {
                Eventos = eventos
                    .Select(e => new EventoPublicoViewModel
                    {
                        IdEvento = e.IdEvento,
                        Nombre = e.Nombre,
                        Descripcion = e.Descripcion,
                        Ubicacion = e.Ubicacion,
                        ImagenUrl = e.ImagenUrl,
                        FechaInicio = e.FechaInicio,
                        FechaFin = e.FechaFin
                    })
                    .ToList(),

                Talleres = (await ConsultarTalleresPublicosAsync(null, 0))
                    .Take(3)
                    .ToList(),

                Emprendimientos = emprendimientos
                    .Select(e => new EmprendimientoPublicoViewModel
                    {
                        IdEmprendimiento = e.IdEmprendimiento,
                        NombreComercial = e.NombreComercial,
                        Descripcion = e.Descripcion,
                        Categoria = e.Categoria,
                        ImagenUrl = !string.IsNullOrWhiteSpace(e.ImagenPrincipal)
                            ? e.ImagenPrincipal
                            : e.LogoUrl,
                        Valoracion = e.Valoraciones.Count > 0
                            ? Math.Round(e.Valoraciones.Average(), 1)
                            : null
                    })
                    .OrderByDescending(e => e.Valoracion ?? 0)
                    .ThenBy(e => e.NombreComercial)
                    .Take(4)
                    .ToList(),

                Promocion = promociones.FirstOrDefault(),
                ImagenesComunidad = imagenesComunidad
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Contacto()
        {
            return View();
        }
        public IActionResult SobreNosotros()
        {
            return View();
        }
        public IActionResult Reglamento()
        {
            return View();
        }
        public async Task<IActionResult> Promociones()
        {
            return View(await ConsultarPromocionesPublicasAsync());
        }

        private async Task<List<PromocionPublicaViewModel>> ConsultarPromocionesPublicasAsync()
        {
            await _promociones.SincronizarEstadosAsync();

            var datos = await _context.Promociones
                .AsNoTracking()
                .Where(p => p.Estado == PromocionesService.Activa)
                .OrderBy(p => p.FechaFin)
                .Select(p => new
                {
                    p.IdPromocion,
                    p.Nombre,
                    p.Descripcion,
                    p.TipoDescuento,
                    p.ValorDescuento,
                    p.FechaInicio,
                    p.FechaFin,
                    p.LimiteUsos,
                    Usos = p.UsosPromocions.Count(),
                    Emprendimientos = p.IdEmprendimientos.Select(e => e.NombreComercial).ToList(),
                    Productos = p.IdProductos.Select(x => x.Nombre).ToList(),
                    Eventos = p.IdEventos.Select(x => x.Nombre).ToList()
                })
                .ToListAsync();

            return datos
                .Where(p => !p.LimiteUsos.HasValue || p.Usos < p.LimiteUsos.Value)
                .Select(p =>
                {
                    List<string> partes = [];

                    if (p.Emprendimientos.Count > 0)
                    {
                        partes.Add(string.Join(", ", p.Emprendimientos));
                    }

                    if (p.Productos.Count > 0)
                    {
                        partes.Add(string.Join(", ", p.Productos));
                    }

                    if (p.Eventos.Count > 0)
                    {
                        partes.Add("Evento: " + string.Join(", ", p.Eventos));
                    }

                    return new PromocionPublicaViewModel
                    {
                        Id = p.IdPromocion,
                        Nombre = p.Nombre,
                        Descripcion = p.Descripcion ?? string.Empty,
                        Descuento = p.TipoDescuento == "Porcentaje"
                            ? $"{p.ValorDescuento:0.##}%"
                            : $"₡{p.ValorDescuento:N0}",
                        AplicaA = partes.Count == 0 ? "Todos los productos" : string.Join(" · ", partes),
                        FechaInicio = p.FechaInicio,
                        FechaFin = p.FechaFin,
                        UsosRestantes = p.LimiteUsos.HasValue ? p.LimiteUsos.Value - p.Usos : null
                    };
                })
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> AprovecharPromocion(int id)
        {
            await _promociones.SincronizarEstadosAsync();

            var promocion = await _context.Promociones
                .AsNoTracking()
                .Where(p => p.IdPromocion == id)
                .Select(p => new
                {
                    p.Estado,
                    p.Nombre,
                    p.LimiteUsos,
                    Usos = p.UsosPromocions.Count()
                })
                .FirstOrDefaultAsync();

            bool disponible = promocion is not null &&
                              promocion.Estado == PromocionesService.Activa &&
                              (!promocion.LimiteUsos.HasValue || promocion.Usos < promocion.LimiteUsos.Value);

            if (!disponible)
            {
                TempData["ErrorPromocion"] = "Esta promoción ya no está disponible.";

                return RedirectToAction(nameof(Promociones));
            }

            return RedirectToAction(nameof(CatalogoProductos), new { promocion = id });
        }
        public async Task<IActionResult> Noticias()
        {
            List<NoticiaPublicaViewModel> noticias = await ConsultarNoticiasPublicadasAsync(null, 13);

            return View(new ListadoNoticiasPublicasViewModel
            {
                Destacada = noticias.FirstOrDefault(),
                Recientes = noticias.Skip(1).ToList()
            });
        }

        public async Task<IActionResult> DetalleNoticia(int id)
        {
            List<NoticiaPublicaViewModel> noticias = await ConsultarNoticiasPublicadasAsync(id, 1);
            NoticiaPublicaViewModel? noticia = noticias.FirstOrDefault();

            if (noticia is null)
            {
                return RedirectToAction(nameof(Noticias));
            }

            List<NoticiaPublicaViewModel> relacionadas = await ConsultarNoticiasPublicadasAsync(null, 3);

            return View(new DetalleNoticiaPublicaViewModel
            {
                Noticia = noticia,
                Relacionadas = relacionadas.Where(n => n.Id != id).Take(2).ToList()
            });
        }

        private async Task<List<NoticiaPublicaViewModel>> ConsultarNoticiasPublicadasAsync(int? id, int cantidad)
        {
            IQueryable<Noticia> consulta = _context.Noticias
                .AsNoTracking()
                .Where(n => n.Estado == "Publicada");

            if (id.HasValue)
            {
                consulta = consulta.Where(n => n.IdNoticia == id.Value);
            }

            var datos = await consulta
                .OrderByDescending(n => n.FechaPublicacion ?? n.FechaCreacion)
                .Take(cantidad)
                .Select(n => new
                {
                    n.IdNoticia,
                    n.Titulo,
                    n.Contenido,
                    n.Tipo,
                    n.ImagenUrl,
                    Fecha = n.FechaPublicacion ?? n.FechaCreacion
                })
                .ToListAsync();

            return datos
                .Select(n => new NoticiaPublicaViewModel
                {
                    Id = n.IdNoticia,
                    Titulo = n.Titulo,
                    Contenido = n.Contenido,
                    Resumen = n.Contenido.Length > 180 ? n.Contenido[..180].TrimEnd() + "..." : n.Contenido,
                    Tipo = n.Tipo,
                    ImagenUrl = n.ImagenUrl ?? "/images/noticia-comunidad.jpg",
                    Fecha = n.Fecha
                })
                .ToList();
        }
        public async Task<IActionResult> Emprendimientos(int? categoria, int? etiqueta)
        {
            IQueryable<Emprendimiento> consulta = _context.Emprendimientos
                .AsNoTracking()
                .Where(e =>
                    e.EstadoAprobacion == "Aprobado" &&
                    e.Activo);

            if (categoria.HasValue)
            {
                consulta = consulta.Where(e => e.IdCategoria == categoria.Value);
            }

            if (etiqueta.HasValue)
            {
                consulta = consulta.Where(e => e.IdEtiqueta.Any(t => t.IdEtiqueta == etiqueta.Value));
            }

            var datos = await consulta
                .OrderBy(e => e.NombreComercial)
                .Select(e => new
                {
                    e.IdEmprendimiento,
                    e.NombreComercial,
                    e.Descripcion,
                    e.LogoUrl,
                    Categoria = e.IdCategoriaNavigation == null
                        ? null
                        : e.IdCategoriaNavigation.Nombre,
                    Provincia = e.IdUsuarioPropietarioNavigation.IdProvinciaNavigation == null
                        ? null
                        : e.IdUsuarioPropietarioNavigation.IdProvinciaNavigation.Nombre,
                    ImagenPrincipal = e.EmprendimientoImagene == null
                        ? null
                        : e.EmprendimientoImagene.UrlImagen,
                    Valoraciones = e.ComentariosResenas
                        .Where(c => c.Estado != "Eliminado" && c.Valoracion != null)
                        .Select(c => (int)c.Valoracion!.Value)
                        .ToList()
                })
                .ToListAsync();

            ViewBag.Emprendimientos = datos
                .Select(e => new EmprendimientoPublicoViewModel
                {
                    IdEmprendimiento = e.IdEmprendimiento,
                    NombreComercial = e.NombreComercial,
                    Descripcion = e.Descripcion,
                    Categoria = e.Categoria,
                    Provincia = e.Provincia,
                    ImagenUrl = !string.IsNullOrWhiteSpace(e.ImagenPrincipal)
                        ? e.ImagenPrincipal
                        : e.LogoUrl,
                    Valoracion = e.Valoraciones.Count > 0
                        ? Math.Round(e.Valoraciones.Average(), 1)
                        : null
                })
                .ToList();

            ViewBag.CategoriasEmprendimiento = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Modulo == "Emprendimientos" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.CategoriaSeleccionada = categoria;
            ViewBag.EtiquetaSeleccionada = etiqueta;
            ViewBag.EtiquetasFiltro = await _context.Etiquetas
                .AsNoTracking()
                .Where(t => t.Activa && t.IdEmprendimientos.Any())
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            return View();
        }
        [HttpGet]
        public async Task<IActionResult> DetalleEmprendimiento(int? id, string? name)
        {
            IQueryable<Emprendimiento> consulta = _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.EstadoAprobacion == "Aprobado" && e.Activo);

            if (id.HasValue)
            {
                consulta = consulta.Where(e => e.IdEmprendimiento == id.Value);
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                consulta = consulta.Where(e => e.NombreComercial == name);
            }
            else
            {
                return NotFound();
            }

            var datos = await consulta
                .OrderBy(e => e.IdEmprendimiento)
                .Select(e => new
                {
                    e.IdEmprendimiento,
                    e.IdCategoria,
                    e.NombreComercial,
                    e.Descripcion,
                    e.Telefono,
                    e.Correo,
                    e.SitioWeb,
                    e.Instagram,
                    e.Facebook,
                    Categoria = e.IdCategoriaNavigation == null
                        ? null
                        : e.IdCategoriaNavigation.Nombre,
                    Provincia = e.IdUsuarioPropietarioNavigation.IdProvinciaNavigation == null
                        ? null
                        : e.IdUsuarioPropietarioNavigation.IdProvinciaNavigation.Nombre,
                    ImagenPrincipal = e.EmprendimientoImagene == null
                        ? null
                        : e.EmprendimientoImagene.UrlImagen,
                    e.LogoUrl,
                    Valoraciones = e.ComentariosResenas
                        .Where(c => c.Estado == "Visible" && c.Valoracion != null)
                        .Select(c => (int)c.Valoracion!.Value)
                        .ToList(),
                    Imagenes = e.Galeria
                        .Where(g => g.Estado == "Publicada")
                        .SelectMany(g => g.GaleriaImagenes)
                        .OrderByDescending(i => i.EsPortada)
                        .ThenBy(i => i.OrdenVisual)
                        .Select(i => i.UrlImagen)
                        .Take(6)
                        .ToList(),
                    Productos = e.Productos
                        .Where(p => p.Estado == "Publicado")
                        .OrderByDescending(p => p.EsDestacado)
                        .ThenByDescending(p => p.FechaRegistro)
                        .Take(6)
                        .Select(p => new ProductoEmprendimientoPublicoViewModel
                        {
                            IdProducto = p.IdProducto,
                            Nombre = p.Nombre,
                            Descripcion = p.Descripcion,
                            Precio = p.Precio,
                            ImagenUrl = p.ProductoImagene == null
                                ? null
                                : p.ProductoImagene.UrlImagen
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (datos is null)
            {
                return NotFound();
            }

            int? idCategoria = datos.IdCategoria;

            List<EmprendimientoPublicoViewModel> relacionados = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e =>
                    e.EstadoAprobacion == "Aprobado" &&
                    e.Activo &&
                    e.IdEmprendimiento != datos.IdEmprendimiento)
                .OrderBy(e => e.IdCategoria == idCategoria ? 0 : 1)
                .ThenBy(e => e.NombreComercial)
                .Take(2)
                .Select(e => new EmprendimientoPublicoViewModel
                {
                    IdEmprendimiento = e.IdEmprendimiento,
                    NombreComercial = e.NombreComercial,
                    Categoria = e.IdCategoriaNavigation == null
                        ? null
                        : e.IdCategoriaNavigation.Nombre,
                    ImagenUrl = e.EmprendimientoImagene != null
                        ? e.EmprendimientoImagene.UrlImagen
                        : e.LogoUrl
                })
                .ToListAsync();

            DetalleEmprendimientoPublicoViewModel modelo = new()
            {
                IdEmprendimiento = datos.IdEmprendimiento,
                NombreComercial = datos.NombreComercial,
                Descripcion = datos.Descripcion,
                Categoria = datos.Categoria,
                Provincia = datos.Provincia,
                LogoUrl = !string.IsNullOrWhiteSpace(datos.ImagenPrincipal)
                    ? datos.ImagenPrincipal
                    : datos.LogoUrl,
                Telefono = datos.Telefono,
                Correo = datos.Correo,
                SitioWeb = datos.SitioWeb,
                Instagram = datos.Instagram,
                Facebook = datos.Facebook,
                Valoracion = datos.Valoraciones.Count > 0
                    ? Math.Round(datos.Valoraciones.Average(), 1)
                    : null,
                CantidadResenas = datos.Valoraciones.Count,
                Imagenes = datos.Imagenes,
                Productos = datos.Productos,
                Relacionados = relacionados
            };

            int idUsuarioActual =
    HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            // Obtener las reseñas visibles del emprendimiento.
            var resenas = await _context.ComentariosResenas
                .AsNoTracking()
                .Where(c =>
                    c.IdEmprendimiento == datos.IdEmprendimiento &&
                    c.Estado == "Visible" &&
                    c.Valoracion != null)
                .OrderByDescending(c =>
                    c.FechaEdicion ?? c.FechaPublicacion)
                .Select(c => new
                {
                    c.IdComentario,
                    c.IdUsuario,
                    Autor = c.IdUsuarioNavigation.Nombre,
                    Valoracion = c.Valoracion!.Value,
                    c.Contenido,
                    Fecha = c.FechaEdicion ?? c.FechaPublicacion,
                    Editada = c.FechaEdicion != null
                })
                .ToListAsync();

            // Preparar las reseñas para la vista pública.
            ViewBag.Resenas = resenas
                .Select(r => new ResenaPublicaViewModel
                {
                    IdComentario = r.IdComentario,
                    IdUsuario = r.IdUsuario,
                    Autor = r.Autor,
                    Valoracion = r.Valoracion,
                    Contenido = r.Contenido,
                    Fecha = r.Fecha,
                    Editada = r.Editada
                })
                .ToList();

            // Identificar si el usuario ya publicó una reseña.
            ViewBag.MiResenaEmprendimiento =
                (ViewBag.Resenas as List<ResenaPublicaViewModel>)?
                    .FirstOrDefault(r => r.IdUsuario == idUsuarioActual);

            // Saber si existe una sesión iniciada.
            ViewBag.UsuarioAutenticado = idUsuarioActual != 0;
            return View(modelo);
        }
        public async Task<IActionResult> Eventos(
            int? categoria,
            int? etiqueta,
            string estado = "proximos")
        {
            DateTime ahora = DateTime.Now;

            bool mostrarFinalizados = estado == "finalizados";

            IQueryable<Evento> consulta = _context.Eventos
                .AsNoTracking();

            if (mostrarFinalizados)
            {
                consulta = consulta.Where(e =>
                    (e.Estado == "Programado" ||
                     e.Estado == "Activo" ||
                     e.Estado == "Finalizado") &&
                    (e.FechaFin ?? e.FechaInicio) < ahora);
            }
            else
            {
                consulta = consulta.Where(e =>
                    (e.Estado == "Programado" ||
                     e.Estado == "Activo") &&
                    (e.FechaFin ?? e.FechaInicio) >= ahora);
            }

            if (categoria.HasValue)
            {
                consulta = consulta.Where(e => e.IdCategoria == categoria.Value);
            }

            if (etiqueta.HasValue)
            {
                consulta = consulta.Where(e => e.IdEtiqueta.Any(t => t.IdEtiqueta == etiqueta.Value));
            }

            var eventos = await (
                mostrarFinalizados
                    ? consulta.OrderByDescending(e => e.FechaInicio)
                    : consulta.OrderBy(e => e.FechaInicio)
                )
                .Select(e => new
                {
                    e.IdEvento,
                    e.Nombre,
                    e.Descripcion,
                    e.Ubicacion,
                    e.ImagenUrl,
                    e.FechaInicio,
                    e.FechaFin,
                    Categoria = e.IdCategoriaNavigation == null
                        ? null
                        : e.IdCategoriaNavigation.Nombre
                })
                .ToListAsync();

            ViewBag.Eventos = eventos
                .Select(e => new EventoPublicoViewModel
                {
                    IdEvento = e.IdEvento,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Ubicacion = e.Ubicacion,
                    ImagenUrl = e.ImagenUrl,
                    FechaInicio = e.FechaInicio,
                    FechaFin = e.FechaFin,
                    Categoria = e.Categoria
                })
                .ToList();

            ViewBag.CategoriasEvento = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Modulo == "Eventos" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.CategoriaSeleccionada = categoria;
            ViewBag.EtiquetaSeleccionada = etiqueta;
            ViewBag.EtiquetasFiltro = await _context.Etiquetas
                .AsNoTracking()
                .Where(t => t.Activa && t.IdEventos.Any())
                .OrderBy(t => t.Nombre)
                .ToListAsync();
            ViewBag.EstadoEventos = mostrarFinalizados
                ? "finalizados"
                : "proximos";

            return View();
        }
        public async Task<IActionResult> Talleres(int? categoria, int? etiqueta)
        {
            List<TallerPublicoViewModel> talleres =
                await ConsultarTalleresPublicosAsync(null, 0, categoria, etiqueta);

            ViewBag.Filtro = new FiltroCatalogoViewModel
            {
                Accion = nameof(Talleres),
                Categoria = categoria,
                Etiqueta = etiqueta,
                Categorias = await _context.Categorias
                    .AsNoTracking()
                    .Where(c => c.Modulo == "Talleres" && c.Activa)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync(),
                Etiquetas = await _context.Etiquetas
                    .AsNoTracking()
                    .Where(t => t.Activa && t.IdTallers.Any())
                    .OrderBy(t => t.Nombre)
                    .ToListAsync()
            };

            return View(talleres);
        }

        private async Task<List<TallerPublicoViewModel>> ConsultarTalleresPublicosAsync(
            int? idTaller,
            int idUsuario,
            int? categoria = null,
            int? etiqueta = null)
        {
            DateTime ahora = DateTime.Now;

            IQueryable<Tallere> consulta = _context.Talleres
                .AsNoTracking()
                .Where(t =>
                    t.FechaInicio != null &&
                    ((t.Estado == ReglasTalleres.Programado &&
                      (t.FechaFin ?? t.FechaInicio) >= ahora) ||
                     (idTaller.HasValue &&
                      idUsuario != 0 &&
                      t.IdTaller == idTaller.Value &&
                      t.InscripcionesTallers.Any(i => i.IdUsuario == idUsuario))));

            if (idTaller.HasValue)
            {
                consulta = consulta.Where(t => t.IdTaller == idTaller.Value);
            }

            if (categoria.HasValue)
            {
                consulta = consulta.Where(t => t.IdCategoria == categoria.Value);
            }

            if (etiqueta.HasValue)
            {
                consulta = consulta.Where(t => t.IdEtiqueta.Any(x => x.IdEtiqueta == etiqueta.Value));
            }

            var filas = await consulta
                .OrderBy(t => t.FechaInicio)
                .Select(t => new
                {
                    t.IdTaller,
                    t.Nombre,
                    t.Descripcion,
                    t.Ubicacion,
                    t.ImagenUrl,
                    t.FechaInicio,
                    t.FechaFin,
                    t.CupoMaximo,
                    t.Estado,
                    t.ResponsableNombre,
                    t.ResponsableCorreo,
                    t.ResponsableTelefono,
                    Categoria = t.IdCategoriaNavigation == null
                        ? null
                        : t.IdCategoriaNavigation.Nombre,
                    Inscritos = t.InscripcionesTallers
                        .Count(i => i.Estado != "Cancelada"),
                    Registrado = idUsuario != 0 &&
                        t.InscripcionesTallers.Any(i =>
                            i.IdUsuario == idUsuario &&
                            i.Estado != "Cancelada")
                })
                .ToListAsync();

            return filas
                .Select(t => new TallerPublicoViewModel
                {
                    IdTaller = t.IdTaller,
                    Nombre = t.Nombre,
                    Descripcion = t.Descripcion ?? string.Empty,
                    Ubicacion = t.Ubicacion ?? string.Empty,
                    ImagenUrl = t.ImagenUrl,
                    FechaInicio = t.FechaInicio!.Value,
                    FechaFin = t.FechaFin,
                    CupoMaximo = t.CupoMaximo,
                    Estado = t.Estado,
                    Inscritos = t.Inscritos,
                    Categoria = t.Categoria,
                    ResponsableNombre = t.ResponsableNombre,
                    ResponsableCorreo = t.ResponsableCorreo,
                    ResponsableTelefono = t.ResponsableTelefono,
                    UsuarioRegistrado = t.Registrado
                })
                .ToList();
        }

        public IActionResult PreguntasFrecuentes()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> CatalogoProductos(int? categoria, int? etiqueta, int? promocion)
        {
            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            List<Producto> productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdEmprendimientoNavigation)
                .Include(p => p.ProductoImagene)
                .Where(p =>
                    p.Estado == "Publicado" &&
                    p.IdEmprendimientoNavigation.Activo &&
                    p.IdEmprendimientoNavigation.EstadoAprobacion == "Aprobado" &&

                    _context.Suscripciones.Any(s =>
                        s.IdEmprendimiento == p.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= fechaActual &&
                        s.FechaFin >= fechaActual))
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            if (categoria.HasValue)
            {
                productos = productos.Where(p => p.IdCategoria == categoria.Value).ToList();
            }

            if (etiqueta.HasValue)
            {
                HashSet<int> idsConEtiqueta = (await _context.Productos
                    .AsNoTracking()
                    .Where(p => p.IdEtiqueta.Any(t => t.IdEtiqueta == etiqueta.Value))
                    .Select(p => p.IdProducto)
                    .ToListAsync()).ToHashSet();

                productos = productos.Where(p => idsConEtiqueta.Contains(p.IdProducto)).ToList();
            }

            if (promocion.HasValue)
            {
                var datosPromocion = await _context.Promociones
                    .AsNoTracking()
                    .Where(p => p.IdPromocion == promocion.Value && p.Estado == PromocionesService.Activa)
                    .Select(p => new
                    {
                        p.Nombre,
                        Productos = p.IdProductos.Select(x => x.IdProducto).ToList(),
                        Emprendimientos = p.IdEmprendimientos.Select(x => x.IdEmprendimiento).ToList(),
                        Eventos = p.IdEventos.Count()
                    })
                    .FirstOrDefaultAsync();

                if (datosPromocion is not null)
                {
                    bool aplicaATodos = datosPromocion.Productos.Count == 0 &&
                                        datosPromocion.Emprendimientos.Count == 0;

                    if (!aplicaATodos)
                    {
                        productos = productos
                            .Where(p => datosPromocion.Productos.Contains(p.IdProducto) ||
                                        datosPromocion.Emprendimientos.Contains(p.IdEmprendimiento))
                            .ToList();
                    }

                    ViewBag.PromocionActiva = datosPromocion.Nombre;
                }
            }

            ViewBag.Filtro = new FiltroCatalogoViewModel
            {
                Accion = nameof(CatalogoProductos),
                Categoria = categoria,
                Etiqueta = etiqueta,
                RutaExtra = promocion.HasValue
                    ? new Dictionary<string, string> { ["promocion"] = promocion.Value.ToString() }
                    : [],
                Categorias = await _context.Categorias
                    .AsNoTracking()
                    .Where(c => c.Modulo == "Productos" && c.Activa)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync(),
                Etiquetas = await _context.Etiquetas
                    .AsNoTracking()
                    .Where(t => t.Activa && t.IdProductos.Any())
                    .OrderBy(t => t.Nombre)
                    .ToListAsync()
            };

            return View(productos);
        }


        [HttpGet]
        public async Task<IActionResult> HechoEnCR()
        {
            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            var productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdEmprendimientoNavigation)
                .Include(p => p.ProductoImagene)
                .Where(p =>
                    p.PublicadoHechoEnCr &&
                    p.Estado == "Publicado" &&
                    p.IdEmprendimientoNavigation.Activo &&
                    p.IdEmprendimientoNavigation.EstadoAprobacion == "Aprobado" &&
                    p.IdEmprendimientoNavigation.ParticipaHechoEnCr &&

                    _context.Suscripciones.Any(s =>
                        s.IdEmprendimiento == p.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= fechaActual &&
                        s.FechaFin >= fechaActual))
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            return View(productos);
        }


        [HttpGet]
        public async Task<IActionResult> DetalleProducto(int id)
        {
            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Today);

            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdEmprendimientoNavigation)
                .Include(p => p.ProductoImagene)
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == id &&
                    p.Estado == "Publicado" &&
                    p.IdEmprendimientoNavigation.Activo &&
                    p.IdEmprendimientoNavigation.EstadoAprobacion == "Aprobado" &&
                    _context.Suscripciones.Any(s =>
                        s.IdEmprendimiento == p.IdEmprendimiento &&
                        s.Estado == "Activa" &&
                        s.FechaInicio <= fechaActual &&
                        s.FechaFin >= fechaActual));

            if (producto == null)
            {
                return NotFound();
            }
            var resenas = await _context.ComentariosResenas
                .AsNoTracking()
                .Where(c =>
                    c.IdProducto == id &&
                    c.Estado != "Eliminado" &&
                    c.Valoracion != null)
                .OrderByDescending(c => c.FechaEdicion ?? c.FechaPublicacion)
                .Select(c => new
                {
                    Autor = c.IdUsuarioNavigation.Nombre,
                    Valoracion = c.Valoracion!.Value,
                    c.Contenido,
                    Fecha = c.FechaEdicion ?? c.FechaPublicacion,
                    Editada = c.FechaEdicion != null
                })
                .ToListAsync();

            ViewBag.Resenas = resenas
                .Select(r => new ResenaPublicaViewModel
                {
                    Autor = r.Autor,
                    Valoracion = r.Valoracion,
                    Contenido = r.Contenido,
                    Fecha = r.Fecha,
                    Editada = r.Editada
                })
                .ToList();

            ViewBag.PromedioValoracion = resenas.Count > 0
                ? Math.Round(resenas.Average(r => (double)r.Valoracion), 1)
                : 0d;

            return View(producto);
        }
        public async Task<IActionResult> DetalleEvento(int id)
        {
            int idUsuarioActual = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            EventoPublicoViewModel? evento = await _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    e.IdEvento == id &&
                    (e.Estado == "Programado" ||
                     e.Estado == "Activo" ||
                     e.Estado == "Finalizado"))
                .Select(e => new EventoPublicoViewModel
                {
                    IdEvento = e.IdEvento,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Ubicacion = e.Ubicacion,
                    ImagenUrl = e.ImagenUrl,
                    FechaInicio = e.FechaInicio,
                    FechaFin = e.FechaFin,
                    CupoMaximo = e.CupoMaximo,
                    Inscritos = e.InscripcionesEventos
                        .Count(i => i.Estado != "Cancelada"),
                    UsuarioRegistrado = idUsuarioActual != 0 &&
                        e.InscripcionesEventos.Any(i =>
                            i.IdUsuario == idUsuarioActual &&
                            i.Estado != "Cancelada"),
                    Categoria = e.IdCategoriaNavigation == null
                        ? null
                        : e.IdCategoriaNavigation.Nombre
                })
                .FirstOrDefaultAsync();

            if (evento == null)
            {
                return NotFound();
            }

            ViewBag.Emprendimientos = await _context.Emprendimientos
                .AsNoTracking()
                .Where(e => e.ParticipacionesEventos.Any(p =>
                    p.IdEvento == id && p.Estado == "Aprobada"))
                .OrderBy(e => e.NombreComercial)
                .ToListAsync();

            DateTime ahora = DateTime.Now;

            var otros = await _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    e.IdEvento != id &&
                    (e.Estado == "Programado" ||
                     e.Estado == "Activo") &&
                    (e.FechaFin ?? e.FechaInicio) >= ahora)
                .OrderBy(e => e.FechaInicio)
                .Take(2)
                .Select(e => new
                {
                    e.IdEvento,
                    e.Nombre,
                    e.ImagenUrl,
                    e.FechaInicio,
                    e.FechaFin
                })
                .ToListAsync();

            ViewBag.OtrosEventos = otros
                .Select(e => new EventoPublicoViewModel
                {
                    IdEvento = e.IdEvento,
                    Nombre = e.Nombre,
                    ImagenUrl = e.ImagenUrl,
                    FechaInicio = e.FechaInicio,
                    FechaFin = e.FechaFin
                })
                .ToList();
            var resenas = await _context.ComentariosResenas
                .AsNoTracking()
                .Where(c =>
                    c.IdEvento == id &&
                    c.Estado == "Visible" &&
                    c.Valoracion != null)
                .OrderByDescending(c =>
                    c.FechaEdicion ?? c.FechaPublicacion)
                .Select(c => new
                {
                    c.IdUsuario,
                    c.IdComentario,
                    Autor = c.IdUsuarioNavigation.Nombre,
                    Valoracion = c.Valoracion!.Value,
                    c.Contenido,
                    Fecha = c.FechaEdicion ?? c.FechaPublicacion,
                    Editada = c.FechaEdicion != null
                })
                .ToListAsync();

            ViewBag.Resenas = resenas
                .Select(r => new ResenaPublicaViewModel
                {
                    Autor = r.Autor,
                    Valoracion = r.Valoracion,
                    Contenido = r.Contenido,
                    Fecha = r.Fecha,
                    IdComentario = r.IdComentario,
                    IdUsuario = r.IdUsuario,
                    Editada = r.Editada
                })
                .ToList();

            ViewBag.PromedioResenas = resenas.Count > 0
                ? Math.Round(resenas.Average(r => (double)r.Valoracion), 1)
                : 0d;
            ViewBag.MiResenaEvento =
                (ViewBag.Resenas as List<ResenaPublicaViewModel>)?
                    .FirstOrDefault(r => r.IdUsuario == idUsuarioActual);

            ViewBag.EventoFinalizado =
                (evento.FechaFin ?? evento.FechaInicio) < DateTime.Now;

            return View(evento);
        }
        public async Task<IActionResult> DetalleTaller(int id)
        {
            int idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            TallerPublicoViewModel? taller =
                (await ConsultarTalleresPublicosAsync(id, idUsuario)).FirstOrDefault();

            if (taller == null)
            {
                return NotFound();
            }

            ViewBag.OtrosTalleres = (await ConsultarTalleresPublicosAsync(null, 0))
                .Where(t => t.IdTaller != id)
                .Take(2)
                .ToList();

            return View(taller);
        }
        public async Task<IActionResult> GaleriaMultimedia(string? categoria = null)
        {
            IQueryable<Galeria> consulta = _context.Galerias
                .AsNoTracking()
                .Where(g =>
                    g.Estado == "Publicada" &&
                    g.GaleriaImagenes.Any());

            consulta = categoria switch
            {
                "evento" => consulta.Where(g => g.IdEvento != null),
                "emprendimiento" => consulta.Where(g => g.IdEmprendimiento != null),
                "producto" => consulta.Where(g => g.IdProducto != null),
                _ => consulta
            };

            var datos = await consulta
                .OrderByDescending(g => g.FechaCreacion)
                .Select(g => new
                {
                    g.Nombre,
                    g.IdEvento,
                    g.IdProducto,
                    Imagenes = g.GaleriaImagenes
                        .OrderByDescending(i => i.EsPortada)
                        .ThenBy(i => i.OrdenVisual)
                        .Select(i => i.UrlImagen)
                        .ToList()
                })
                .ToListAsync();

            List<GaleriaAdminViewModel> modelo = datos
                .Select(g => new GaleriaAdminViewModel
                {
                    Nombre = g.Nombre,
                    Categoria =
                        g.IdEvento != null ? "Evento" :
                        g.IdProducto != null ? "Producto" : "Emprendimiento",
                    Imagenes = g.Imagenes
                        .Select(url => new ImagenGaleriaViewModel { Url = url })
                        .ToList()
                })
                .ToList();

            ViewBag.CategoriaActiva = categoria ?? string.Empty;

            return View(modelo);
        }
    }
}
