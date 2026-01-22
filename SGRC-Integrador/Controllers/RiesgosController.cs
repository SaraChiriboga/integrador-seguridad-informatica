using SGRC_Integrador.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace SGRC_Integrador.Controllers
{
    public class RiesgosController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        public ActionResult Index()
        {
            return PartialView(db.Riesgos.ToList());
        }

        // Carga el Dropdown dinámico de Activos
        public ActionResult Create(int? activoId)
        {
            var listaActivos = db.Activos.OrderBy(a => a.Nombre).ToList();

            // Es vital que activoId tenga valor para que SelectList marque ese ítem como seleccionado
            ViewBag.IdActivo = new SelectList(listaActivos, "IdActivo", "Nombre", activoId);
            ViewBag.IsLocked = activoId.HasValue;

            return PartialView();
        }

        [HttpPost]
        public ActionResult Create(Riesgo riesgo)
        {
            if (ModelState.IsValid)
            {
                db.Riesgos.Add(riesgo);
                db.SaveChanges();
                return PartialView("Index", db.Riesgos.ToList());
            }
            return PartialView(riesgo);
        }

        // GET: Riesgos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Usamos .Include(r => r.Activo) para cargar los datos del activo relacionado
            // y .Include(r => r.Tratamientos) si quieres mostrar info del tratamiento en la ficha
            var riesgo = db.Riesgos
                .Include("Activo")
                .FirstOrDefault(r => r.IdRiesgo == id);

            if (riesgo == null)
            {
                return HttpNotFound();
            }

            return PartialView(riesgo);
        }

        [HttpPost]
        public JsonResult EliminarRiesgoConfirmed(int id, string password)
        {
            try
            {
                // 1. Validar Auditor y Contraseña
                string nombreUsuario = Session["UsuarioNombre"]?.ToString();
                var auditor = db.Usuarios.FirstOrDefault(u => u.Nombre == nombreUsuario);

                if (auditor == null || auditor.PasswordHash != password)
                {
                    return Json(new { success = false, message = "Contraseña de seguridad incorrecta." });
                }

                // 2. Buscar el Riesgo incluyendo sus tratamientos
                var riesgo = db.Riesgos.Include("Tratamientos").FirstOrDefault(r => r.IdRiesgo == id);
                if (riesgo == null) return Json(new { success = false, message = "Análisis de riesgo no encontrado." });

                // 3. Eliminación en Cascada manual (si no está configurada en la BD)
                if (riesgo.Tratamientos.Any())
                {
                    db.Tratamientos.RemoveRange(riesgo.Tratamientos);
                }

                db.Riesgos.Remove(riesgo);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error técnico: " + ex.Message });
            }
        }
    }
}