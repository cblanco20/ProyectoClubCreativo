using ProyectoClubCreativo.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models;
using ProyectoClubCreativo.Models.ViewModels;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ProyectoClubCreativo.Data;
using ProyectoClubCreativo.Models.Entities;

namespace ProyectoClubCreativo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ClubCreativoDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            ClubCreativoDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
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
            return View(new ContactoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contacto(ContactoViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            TempData["MensajeContacto"] =
                "Tu mensaje fue validado correctamente. Gracias por contactarnos.";

            return RedirectToAction(nameof(Contacto));
        }
        public IActionResult SobreNosotros()
        {
            return View();
        }
        public IActionResult Reglamento()
        {
            return View();
        }
        public IActionResult Promociones()
        {
            return View();
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
        public async Task<IActionResult> Emprendimientos(int? categoria)
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
                        .Where(c => c.Estado != "Eliminado" && c.Valoracion != null)
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

            return View(modelo);
        }
        public async Task<IActionResult> Eventos(int? categoria)
        {
            DateTime ahora = DateTime.Now;

            IQueryable<Evento> consulta = _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    (e.Estado == "Programado" || e.Estado == "Activo") &&
                    (e.FechaFin ?? e.FechaInicio) >= ahora);

            if (categoria.HasValue)
            {
                consulta = consulta.Where(e => e.IdCategoria == categoria.Value);
            }

            var eventos = await consulta
                .OrderBy(e => e.FechaInicio)
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

            return View();
        }
        public async Task<IActionResult> Talleres()
        {
            List<TallerPublicoViewModel> talleres =
                await ConsultarTalleresPublicosAsync(null, 0);

            return View(talleres);
        }

        private async Task<List<TallerPublicoViewModel>> ConsultarTalleresPublicosAsync(
            int? idTaller,
            int idUsuario)
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
        public async Task<IActionResult> CatalogoProductos()
        {
            List<Producto> productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdEmprendimientoNavigation)
                .Include(p => p.ProductoImagene)
                .Where(p =>
                    p.Estado == "Publicado" &&
                    p.IdEmprendimientoNavigation.Activo &&
                    p.IdEmprendimientoNavigation.EstadoAprobacion == "Aprobado")
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            return View(productos);
        }


        [HttpGet]
        public async Task<IActionResult> HechoEnCR()
        {
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
                    p.IdEmprendimientoNavigation.ParticipaHechoEnCr)
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            return View(productos);
        }


        [HttpGet]
        public async Task<IActionResult> DetalleProducto(int id)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdEmprendimientoNavigation)
                .Include(p => p.ProductoImagene)
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == id &&
                    p.Estado == "Publicado" &&
                    p.IdEmprendimientoNavigation.Activo &&
                    p.IdEmprendimientoNavigation.EstadoAprobacion == "Aprobado");
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
                    (e.Estado == "Programado" || e.Estado == "Activo"))
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
                    (e.Estado == "Programado" || e.Estado == "Activo") &&
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
