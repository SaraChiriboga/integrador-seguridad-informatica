using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SGRC_Integrador.Models;
using SGRC_Integrador.Filters; // Agregar esto

namespace SGRC_Integrador.Controllers
{
    [Authorize]
    [SessionCheck] // AGREGAR ESTE FILTRO
    public class TratamientosController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        public ActionResult Index()
        {
            var tratamientos = db.Tratamientos
                .Include(t => t.Riesgo)
                .Include(t => t.Riesgo.Activo)
                .ToList();

            return PartialView(tratamientos);
        }

        public ActionResult Index(int? highlightId)
        {
            var tratamientos = db.Tratamientos
                .Include(t => t.Riesgo)
                .Include(t => t.Riesgo.Activo)
                .ToList();

            ViewBag.HighlightId = highlightId; // Pasamos el ID a resaltar
            return PartialView(tratamientos);
        }

        public ActionResult Gestionar(int? idRiesgo)
        {
            if (idRiesgo == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var riesgo = db.Riesgos.Include(r => r.Activo).FirstOrDefault(r => r.IdRiesgo == idRiesgo);
            if (riesgo == null) return HttpNotFound();

            // DEPURACIÓN DETALLADA
            System.Diagnostics.Debug.WriteLine($"╔══════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine($"║ GESTIONAR - Diagnóstico de Sesión    ║");
            System.Diagnostics.Debug.WriteLine($"╠══════════════════════════════════════╣");
            System.Diagnostics.Debug.WriteLine($"║ SessionID: {Session.SessionID}");
            System.Diagnostics.Debug.WriteLine($"║ Session[UsuarioNombre]: {Session["UsuarioNombre"]}");
            System.Diagnostics.Debug.WriteLine($"║ Session[UsuarioId]: {Session["UsuarioId"]}");
            System.Diagnostics.Debug.WriteLine($"║ User.Identity.Name: {User.Identity.Name}");
            System.Diagnostics.Debug.WriteLine($"║ User.Identity.IsAuthenticated: {User.Identity.IsAuthenticated}");
            System.Diagnostics.Debug.WriteLine($"╚══════════════════════════════════════╝");

            // Obtener nombre de usuario
            string nombreUsuario = Session["UsuarioNombre"]?.ToString();

            ViewBag.UsuarioActual = nombreUsuario ?? "Usuario SGRC";
            ViewBag.RiesgoInfo = riesgo;

            System.Diagnostics.Debug.WriteLine($"→ ViewBag.UsuarioActual asignado: '{ViewBag.UsuarioActual}'");

            return PartialView(new Tratamiento { IdRiesgo = riesgo.IdRiesgo });
        }

        [HttpPost]
        public ActionResult Gestionar(Tratamiento tratamiento, int nuevaProbabilidad, int nuevoImpacto,
            string[] kpi_nombre, string[] kpi_meta, string[] kpi_frec_valor, string[] kpi_frec_unidad)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Tratamientos.Add(tratamiento);

                    var riesgo = db.Riesgos.Find(tratamiento.IdRiesgo);
                    if (riesgo != null)
                    {
                        riesgo.Tratado = true;
                        db.Entry(riesgo).State = EntityState.Modified;
                    }

                    db.SaveChanges();

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
            ViewBag.UsuarioActual = Session["UsuarioNombre"]?.ToString() ?? "Usuario SGRC";

            return PartialView(tratamiento);
        }

        public ActionResult Bitacora(int id)
        {
            var tratamiento = db.Tratamientos
                .Include(t => t.Riesgo)
                .FirstOrDefault(t => t.IdTratamiento == id);

            ViewBag.KPIs = db.Database.SqlQuery<TratamientoKPI>(
                "SELECT * FROM TratamientoKPIs WHERE IdTratamiento = {0}", id).ToList();

            ViewBag.Historial = db.Database.SqlQuery<TratamientoBitacora>(
                "SELECT * FROM TratamientoBitacora WHERE IdTratamiento = {0} ORDER BY FechaRegistro DESC", id).ToList();

            return PartialView(tratamiento);
        }

        [HttpPost]
        public JsonResult GuardarBitacora(int IdTratamiento, int? IdKPI, string DescripcionActividad,
            string ObservacionesTecnicas, int NuevoProgreso)
        {
            try
            {
                string usuario = Session["UsuarioNombre"]?.ToString() ?? "Consultor SGRC";

                string sqlBitacora = @"INSERT INTO TratamientoBitacora 
                               (IdTratamiento, IdKPI, DescripcionActividad, UsuarioResponsable, ObservacionesTecnicas, FechaRegistro) 
                               VALUES (@p0, @p1, @p2, @p3, @p4, GETDATE())";

                db.Database.ExecuteSqlCommand(sqlBitacora, IdTratamiento, (object)IdKPI ?? DBNull.Value,
                    DescripcionActividad, usuario, (object)ObservacionesTecnicas ?? DBNull.Value);

                var tratamiento = db.Tratamientos.Find(IdTratamiento);
                if (tratamiento != null)
                {
                    tratamiento.Progreso = NuevoProgreso;
                    db.Entry(tratamiento).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}