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
        public IActionResult Eventos()
        {
            return View();
        }
        public IActionResult Talleres()
        {
            return View();
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

            return View(producto);
        }
        public IActionResult DetalleEvento(string name)
        {
            ViewData["NombreEvento"] = string.IsNullOrWhiteSpace(name) ? "Arte Inarrivo San Pedro" : name;
            return View();
        }
        public IActionResult DetalleTaller(string name)
        {
            ViewData["NombreTaller"] = string.IsNullOrWhiteSpace(name) ? "Cerámica para principiantes" : name;
            return View();
        }
        public IActionResult GaleriaMultimedia()
        {
            return View();
        }
    }
}
