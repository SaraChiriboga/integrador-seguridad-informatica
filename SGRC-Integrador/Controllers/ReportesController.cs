using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SGRC_Integrador.Models;
using Rotativa;
using ClosedXML.Excel;
using System.IO;
using System.Data.Entity; // Necesario para que las expresiones lambda t => t.Propiedad funcionen en .Include()

namespace SGRC_Integrador.Controllers
{
    public class ReportesController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        public ActionResult Index()
        {
            return PartialView();
        }

        public ActionResult ExportarActivosPDF()
        {
            var activos = db.Activos.ToList();

            // 1. FECHA: Formato largo en español
            ViewBag.FechaReporte = DateTime.Now.ToString("f", new System.Globalization.CultureInfo("es-ES"));

            // 2. AUDITOR: Obtenemos el nombre de la sesión
            string nombreAuditor = Session["UsuarioNombre"]?.ToString();
            ViewBag.Auditor = !string.IsNullOrEmpty(nombreAuditor) ? nombreAuditor : "Auditor no identificado";

            return new Rotativa.ViewAsPdf("ActivosPDF", activos)
            {
                FileName = "Inventario_Activos_SGRC.pdf",
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                CustomSwitches = "--load-error-handling ignore" // Ayuda con la carga de imágenes
            };
        }

        public ActionResult PlanTratamientoPDF(int id)
        {
            var tratamiento = db.Tratamientos
        .Include(t => t.Riesgo)
        .Include(t => t.Riesgo.Activo)
        .FirstOrDefault(t => t.IdTratamiento == id);

            if (tratamiento == null) return HttpNotFound();

            // Carga de KPIs y Bitácora mediante SQL Directo o navegación
            ViewBag.KPIs = db.Database.SqlQuery<TratamientoKPI>(
                "SELECT * FROM TratamientoKPIs WHERE IdTratamiento = {0}", id).ToList();

            ViewBag.Bitacora = db.Database.SqlQuery<TratamientoBitacora>(
                "SELECT * FROM TratamientoBitacora WHERE IdTratamiento = {0} ORDER BY FechaRegistro ASC", id).ToList();

            ViewBag.Auditor = Session["UsuarioNombre"]?.ToString() ?? "Auditor de Seguridad";
            ViewBag.FechaReporte = DateTime.Now.ToString("f");

            return new Rotativa.ViewAsPdf("PlanTratamientoPDF", tratamiento)
            {
                FileName = $"Plan_Mitigacion_TRAT-{id}.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageMargins = new Rotativa.Options.Margins(15, 15, 15, 15)
            };
        }

        public JsonResult GetTratamientosList()
        {
            var lista = db.Tratamientos
                .Include(t => t.Riesgo)
                .Include(t => t.Riesgo.Activo)
                .Select(t => new {
                    Id = t.IdTratamiento,
                    Amenaza = t.Riesgo.Amenaza,
                    Activo = t.Riesgo.Activo.Nombre
                }).ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportarRiesgosExcel()
        {
            // Cambiado a lambda para consistencia
            var riesgos = db.Riesgos.Include(r => r.Activo).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Matriz de Riesgos");

                // Cabeceras y Estilos básicos
                var header = worksheet.Row(1);
                header.Cell(1).Value = "Activo";
                header.Cell(2).Value = "Amenaza";
                header.Cell(3).Value = "Nivel (P x I)";
                header.Cell(4).Value = "Estado";
                header.Style.Font.Bold = true;

                // Datos
                for (int i = 0; i < riesgos.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = riesgos[i].Activo.Nombre;
                    worksheet.Cell(i + 2, 2).Value = riesgos[i].Amenaza;
                    worksheet.Cell(i + 2, 3).Value = riesgos[i].NivelRiesgo;
                    worksheet.Cell(i + 2, 4).Value = riesgos[i].Tratado.GetValueOrDefault() ? "Tratado" : "Expuesto";
                }

                worksheet.Columns().AdjustToContents(); // Ajusta el ancho de las celdas automáticamente

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Matriz_Riesgos_SGRC.xlsx");
                }
            }
        }
    }
}