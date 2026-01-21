using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SGRC_Integrador.Models;
using Rotativa; // Para PDF
using ClosedXML.Excel; // Para Excel
using System.IO;

namespace SGRC_Integrador.Controllers
{
    public class ReportesController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        // Vista principal del módulo de reportes
        public ActionResult Index()
        {
            return PartialView();
        }

        // Generar PDF del Inventario de Activos
        public ActionResult ExportarActivosPDF()
        {
            var activos = db.Activos.ToList();
            // Retorna una vista convertida en PDF
            return new ViewAsPdf("ActivosPDF", activos)
            {
                FileName = "Inventario_Activos_SGRC.pdf",
                PageOrientation = Rotativa.Options.Orientation.Landscape
            };
        }

        // Generar Excel de la Matriz de Riesgos
        public ActionResult ExportarRiesgosExcel()
        {
            var riesgos = db.Riesgos.Include("Activo").ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Matriz de Riesgos");

                // Cabeceras
                worksheet.Cell(1, 1).Value = "Activo";
                worksheet.Cell(1, 2).Value = "Amenaza";
                worksheet.Cell(1, 3).Value = "Nivel (P x I)";
                worksheet.Cell(1, 4).Value = "Estado";

                // Datos
                for (int i = 0; i < riesgos.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = riesgos[i].Activo.Nombre;
                    worksheet.Cell(i + 2, 2).Value = riesgos[i].Amenaza;
                    worksheet.Cell(i + 2, 3).Value = riesgos[i].NivelRiesgo;
                    worksheet.Cell(i + 2, 4).Value = riesgos[i].Tratado.GetValueOrDefault() ? "Tratado" : "Expuesto";
                }

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