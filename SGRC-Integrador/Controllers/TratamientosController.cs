using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SGRC_Integrador.Models;

namespace SGRC_Integrador.Controllers
{
    public class TratamientosController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        public ActionResult Index()
        {
            // Cargamos los tratamientos incluyendo el riesgo, el activo y los KPIs relacionados
            var tratamientos = db.Tratamientos
                .Include(t => t.Riesgo)
                .Include(t => t.Riesgo.Activo)
                .ToList();

            return PartialView(tratamientos);
        }

        // Cambiado de Create a Gestionar
        public ActionResult Gestionar(int? idRiesgo)
        {
            if (idRiesgo == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var riesgo = db.Riesgos.Include(r => r.Activo).FirstOrDefault(r => r.IdRiesgo == idRiesgo);
            if (riesgo == null) return HttpNotFound();

            ViewBag.RiesgoInfo = riesgo;
            return PartialView(new Tratamiento { IdRiesgo = riesgo.IdRiesgo });
        }

        [HttpPost]
        public ActionResult Gestionar(Tratamiento tratamiento, int nuevaProbabilidad, int nuevoImpacto, string[] kpi_nombre, string[] kpi_meta, string[] kpi_frec_valor, string[] kpi_frec_unidad)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // 1. Guardar Plan de Tratamiento
                    db.Tratamientos.Add(tratamiento);

                    // 2. Actualizar Riesgo (Marcar como tratado y registrar el residual)
                    var riesgo = db.Riesgos.Find(tratamiento.IdRiesgo);
                    if (riesgo != null)
                    {
                        riesgo.Tratado = true;
                        // Calculamos el residual para que quede constancia en la BD
                        // riesgo.NivelResidual = nuevaProbabilidad * nuevoImpacto; (Si tienes el campo)
                        db.Entry(riesgo).State = EntityState.Modified;
                    }

                    db.SaveChanges();

                    // 3. Guardar KPIs
                    if (kpi_nombre != null)
                    {
                        for (int i = 0; i < kpi_nombre.Length; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(kpi_nombre[i]))
                            {
                                string meta = (kpi_meta != null && kpi_meta.Length > i) ? kpi_meta[i] : "0";
                                string val = (kpi_frec_valor != null && kpi_frec_valor.Length > i) ? kpi_frec_valor[i] : "1";
                                string uni = (kpi_frec_unidad != null && kpi_frec_unidad.Length > i) ? kpi_frec_unidad[i] : "Semanas";

                                string sql = "INSERT INTO TratamientoKPIs (IdTratamiento, NombreKPI, MetaEsperada, FrecuenciaMedicion) VALUES (@p0, @p1, @p2, @p3)";
                                db.Database.ExecuteSqlCommand(sql, tratamiento.IdTratamiento, kpi_nombre[i], meta + "%", "Cada " + val + " " + uni);
                            }
                        }
                    }

                    return PartialView("~/Views/Riesgos/Index.cshtml", db.Riesgos.Include(r => r.Activo).ToList());
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Content("Error: " + ex.Message);
            }

            ViewBag.RiesgoInfo = db.Riesgos.Include(r => r.Activo).FirstOrDefault(r => r.IdRiesgo == tratamiento.IdRiesgo);
            return PartialView(tratamiento);
        }

        // GET: Tratamientos/Bitacora/5
        public ActionResult Bitacora(int id)
        {
            var tratamiento = db.Tratamientos
                .Include(t => t.Riesgo)
                .FirstOrDefault(t => t.IdTratamiento == id);

            // Obtenemos los KPIs para que el usuario pueda elegir en cuál trabajó
            ViewBag.KPIs = db.Database.SqlQuery<TratamientoKPI>(
                "SELECT * FROM TratamientoKPIs WHERE IdTratamiento = {0}", id).ToList();

            // Obtenemos el historial de la bitácora
            ViewBag.Historial = db.Database.SqlQuery<TratamientoBitacora>(
                "SELECT * FROM TratamientoBitacora WHERE IdTratamiento = {0} ORDER BY FechaRegistro DESC", id).ToList();

            return PartialView(tratamiento);
        }

        [HttpPost]
        public JsonResult GuardarBitacora(int IdTratamiento, int? IdKPI, string DescripcionActividad, string ObservacionesTecnicas)
        {
            try
            {
                // Validación de seguridad básica
                if (string.IsNullOrEmpty(DescripcionActividad))
                {
                    return Json(new { success = false, message = "La descripción de la actividad es obligatoria." });
                }

                string usuario = Session["UsuarioNombre"]?.ToString() ?? "Consultor SGRC";

                // Usamos parámetros con nombre (@p0, @p1...) para evitar errores de tipo de datos y nulos
                string sql = @"INSERT INTO TratamientoBitacora 
                       (IdTratamiento, IdKPI, DescripcionActividad, UsuarioResponsable, ObservacionesTecnicas, FechaRegistro) 
                       VALUES (@p0, @p1, @p2, @p3, @p4, GETDATE())";

                // El truco aquí es (object)VALOR ?? DBNull.Value para que SQL no explote si el KPI es nulo
                db.Database.ExecuteSqlCommand(sql,
                    IdTratamiento,
                    (object)IdKPI ?? DBNull.Value,
                    DescripcionActividad,
                    usuario,
                    (object)ObservacionesTecnicas ?? DBNull.Value);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Enviamos el mensaje real del error para depurar
                return Json(new { success = false, message = ex.InnerException != null ? ex.InnerException.Message : ex.Message });
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}