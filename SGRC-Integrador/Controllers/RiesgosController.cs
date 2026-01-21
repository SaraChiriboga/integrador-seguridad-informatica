using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SGRC_Integrador.Models;

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
    }
}