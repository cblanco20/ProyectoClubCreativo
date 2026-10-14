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
        public IActionResult Noticias()
        {
            return View();
        }
        public IActionResult DetalleNoticia()
        {
            return View();
        }
        public IActionResult Emprendimientos()
        {
            return View();
        }
        public IActionResult DetalleEmprendimiento(string name)
        {
            ViewData["NombreEmprendimiento"] = string.IsNullOrWhiteSpace(name) ? "Orquídea" : name;
            return View();
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
                    t.Estado == ReglasTalleres.Programado &&
                    t.FechaInicio != null &&
                    (t.FechaFin ?? t.FechaInicio) >= ahora);

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
        public IActionResult GaleriaMultimedia()
        {
            return View();
        }
    }
}
