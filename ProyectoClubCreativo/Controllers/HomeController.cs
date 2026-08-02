using Microsoft.AspNetCore.Mvc;
using ProyectoClubCreativo.Models;
using ProyectoClubCreativo.Models.ViewModels;
using System.Diagnostics;

namespace ProyectoClubCreativo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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
    }
}
