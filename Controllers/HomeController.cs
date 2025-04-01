using Microsoft.AspNetCore.Mvc;
using WebPlan.Models;
using WebPlan.Services; // Asegúrate de que este espacio de nombres y el ensamblado estén referenciados correctamente
using WebPlan.Models;
using WebPlan.Services; // Asegúrate de que este espacio de nombres y el ensamblado estén referenciados correctamente


using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WebPlan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISolicitudService _solicitudService;

        // Un único constructor que recibe todas las dependencias
        public HomeController(
            ILogger<HomeController> logger,
            ISolicitudService solicitudService)
        {
            _logger = logger;
            _solicitudService = solicitudService;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        public IActionResult Solicitud() => View();

        public IActionResult TipoSeguro() => View();

        public IActionResult PlanPagos() => View();

        [HttpPost]
        public async Task<IActionResult> CargarSolicitudes()
        {
            try
            {
                var solicitudes = await _solicitudService.GetSolicitudesAsync(17, "2");
                return PartialView("_SolicitudesPartial", solicitudes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CargarSolicitudes");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }


    }
}
