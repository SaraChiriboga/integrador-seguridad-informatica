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
            // 1. Total de Activos
            ViewBag.TotalActivos = db.Activos.Count();

            // 2. Activos Críticos (ValorTotal >= 10)
            ViewBag.ActivosCriticos = db.Activos.Count(a => a.ValorTotal >= 10);

            // 3. Riesgos Expuestos (Aquellos que NO han sido tratados)
            ViewBag.RiesgosPendientes = db.Riesgos.Count(r => r.Tratado != true);

            // 4. Tratamientos Activos (En proceso: Progreso < 100)
            ViewBag.TratamientosActivos = db.Tratamientos.Count(t => t.Progreso < 100);

            // --- Lógica para el gráfico de barras (Magerit) ---
            var datosGrafico = db.Activos
                .GroupBy(a => a.Tipo)
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .ToList();

            ViewBag.Labels = datosGrafico.Select(d => d.Tipo).ToArray();
            ViewBag.Valores = datosGrafico.Select(d => d.Cantidad).ToArray();

            return View();
        }
    }
}