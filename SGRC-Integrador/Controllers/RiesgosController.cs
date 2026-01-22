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
            ViewBag.IdActivo = new SelectList(listaActivos, "IdActivo", "Nombre", activoId);
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
    }
}