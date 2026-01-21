using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SGRC_Integrador.Models;

namespace SGRC_Integrador.Controllers
{
    public class ActivosController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        // Lista los activos desde la BD
        public ActionResult Index()
        {
            var lista = db.Activos.ToList();
            return PartialView(lista);
        }

        // Muestra el formulario de creación
        public ActionResult Create()
        {
            return PartialView();
        }

        // Procesa el guardado del activo
        [HttpPost]
        public ActionResult Create(Activo activo)
        {
            if (ModelState.IsValid)
            {
                db.Activos.Add(activo);
                db.SaveChanges();
                return PartialView("Index", db.Activos.ToList());
            }
            return PartialView(activo);
        }
    }
}