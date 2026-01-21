using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SGRC_Integrador.Models;

namespace SGRC_Integrador.Controllers
{
    public class TratamientosController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        // GET: Tratamientos - Cambiado a PartialView para AJAX
        public ActionResult Index()
        {
            // Usamos .Include para que no de error al buscar la Amenaza del Riesgo
            var tratamientos = db.Tratamientos.Include(t => t.Riesgo).ToList();
            return PartialView(tratamientos);
        }

        // GET: Tratamientos/Create/5 (Recibe el ID del Riesgo)
        public ActionResult Create(int? idRiesgo)
        {
            if (idRiesgo == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // IMPORTANTE: Cargamos el Riesgo con su Activo para evitar el NullReferenceException en la vista
            var riesgo = db.Riesgos.Include(r => r.Activo).FirstOrDefault(r => r.IdRiesgo == idRiesgo);

            if (riesgo == null)
            {
                return HttpNotFound();
            }

            ViewBag.RiesgoInfo = riesgo;
            return PartialView();
        }

        // POST: Tratamientos/Create
        [HttpPost]
        public ActionResult Create([Bind(Include = "IdTratamiento,IdRiesgo,Estrategia,AccionCorrectiva,Responsable,FechaLimite,Progreso")] Tratamiento tratamiento)
        {
            if (ModelState.IsValid)
            {
                db.Tratamientos.Add(tratamiento);

                // LÓGICA ADICIONAL: Marcamos el riesgo como 'Tratado' automáticamente
                var riesgo = db.Riesgos.Find(tratamiento.IdRiesgo);
                if (riesgo != null)
                {
                    riesgo.Tratado = true;
                }

                db.SaveChanges();

                // Retornamos la vista de Riesgos para ver el cambio de estado
                return PartialView("~/Views/Riesgos/Index.cshtml", db.Riesgos.Include(r => r.Activo).ToList());
            }

            // Si hay error, recargamos la info del riesgo para la vista
            ViewBag.RiesgoInfo = db.Riesgos.Include(r => r.Activo).FirstOrDefault(r => r.IdRiesgo == tratamiento.IdRiesgo);
            return PartialView(tratamiento);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}