using System.Linq;
using System.Web.Mvc;
using SGRC_Integrador.Models;

namespace SGRC_Integrador.Controllers
{
    public class HomeController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        public ActionResult Index()
        {
            // KPIs para los contadores
            ViewBag.TotalActivos = db.Activos.Count();
            ViewBag.ActivosCriticos = db.Activos.Count(a => a.ValorTotal >= 10);
            ViewBag.RiesgosPendientes = db.Riesgos.Count(r => r.Tratado == false);

            // Datos para el gráfico de barras (Activos por Tipo)
            var datosGrafico = db.Activos
                .GroupBy(a => a.Tipo)
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .ToList();

            ViewBag.Labels = datosGrafico.Select(x => x.Tipo).ToArray();
            ViewBag.Valores = datosGrafico.Select(x => x.Cantidad).ToArray();

            return View();
        }
    }
}