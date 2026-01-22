using System;
using System.Collections.Generic;
using System.Data.Entity; // Necesario para EntityState
using System.Linq;
using System.Web.Mvc;
using SGRC_Integrador.Models;
using System.Net;

namespace SGRC_Integrador.Controllers
{
    public class ActivosController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        public ActionResult Index()
        {
            var lista = db.Activos.ToList();
            return PartialView(lista);
        }

        public ActionResult Create()
        {
            return PartialView();
        }

        [HttpPost]
        public ActionResult Create(Activo activo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Activos.Add(activo);
                    db.SaveChanges();
                    // Devolvemos éxito para que JS recargue la lista
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return PartialView(activo);
        }

        // --- NUEVO MÉTODO: DETALLES ---
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var activo = db.Activos.Find(id);
            if (activo == null) return HttpNotFound();

            return PartialView(activo);
        }

        // --- NUEVO MÉTODO: EDITAR (GET) ---
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var activo = db.Activos.Find(id);
            if (activo == null) return HttpNotFound();

            return PartialView(activo);
        }

        // --- NUEVO MÉTODO: EDITAR (POST) ---
        [HttpPost]
        public ActionResult Edit(Activo activo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(activo).State = EntityState.Modified;
                    db.SaveChanges();
                    // Devolvemos éxito para que JS recargue la lista
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return PartialView(activo);
        }

        [HttpPost]
        public JsonResult DeleteConfirmed(int id, string password)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Validar identidad del Auditor
                    string nombreUsuario = Session["UsuarioNombre"]?.ToString();
                    var usuario = db.Usuarios.FirstOrDefault(u => u.Nombre == nombreUsuario);

                    if (usuario == null || usuario.PasswordHash != password)
                    {
                        return Json(new { success = false, message = "Contraseña de seguridad incorrecta. Autorización denegada." });
                    }

                    // 2. Buscar activo con sus riesgos
                    var activo = db.Activos.Include(a => a.Riesgos).FirstOrDefault(a => a.IdActivo == id);
                    if (activo == null) return Json(new { success = false, message = "El activo ya no existe." });

                    // 3. Eliminación en Cascada Manual
                    // Borramos los riesgos asociados primero para evitar errores de llave foránea
                    if (activo.Riesgos.Any())
                    {
                        db.Riesgos.RemoveRange(activo.Riesgos);
                    }

                    // 4. Borrar Activo
                    db.Activos.Remove(activo);
                    db.SaveChanges();

                    transaction.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Error técnico: " + ex.Message });
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}