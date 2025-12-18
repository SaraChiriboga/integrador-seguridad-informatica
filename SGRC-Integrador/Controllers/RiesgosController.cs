using SGRC_Integrador.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SGRC_Integrador.Controllers
{
    public class RiesgosController : Controller
    {
        // Lista estática para simular la base de datos
        public static List<Riesgo> _riesgos = new List<Riesgo>
{
    // Escenario 1: Ransomware (Muy común y Crítico)
    new Riesgo {
        Id = 1,
        ActivoNombre = "Base de Datos Clientes (CRM)",
        Amenaza = "Ataque de Ransomware / Cifrado no autorizado",
        Probabilidad = 3, // Probable
        Impacto = 4,      // Crítico
        Tratado = false   // Pendiente (Rojo)
    },

    // Escenario 2: Corte físico (Afecta Disponibilidad)
    new Riesgo {
        Id = 2,
        ActivoNombre = "OLT Fibra Óptica (Nodo Norte)",
        Amenaza = "Corte de fibra por vandalismo o accidente",
        Probabilidad = 4, // Muy Probable
        Impacto = 3,      // Alto
        Tratado = true    // Gestionado (Verde)
    },

    // Escenario 3: Fuga de información (Afecta Confidencialidad)
    new Riesgo {
        Id = 3,
        ActivoNombre = "Registros de Llamadas (CDR)",
        Amenaza = "Exfiltración de datos por empleado desleal (Insider)",
        Probabilidad = 2, // Posible
        Impacto = 4,      // Crítico
        Tratado = false
    },

    // Escenario 4: Ataque Web
    new Riesgo {
        Id = 4,
        ActivoNombre = "Portal Web de Pagos",
        Amenaza = "Ataque de Denegación de Servicio (DDoS)",
        Probabilidad = 3,
        Impacto = 3,
        Tratado = true
    },

    // Escenario 5: Falla de Infraestructura
    new Riesgo {
        Id = 5,
        ActivoNombre = "Data Center Principal UIO",
        Amenaza = "Falla prolongada de climatización/energía",
        Probabilidad = 1, // Rara
        Impacto = 4,      // Crítico
        Tratado = true
    }
};

        // GET: Riesgos (Muestra la tabla)
        public ActionResult Index()
        {
            return PartialView(_riesgos);
        }

        // GET: Riesgos/Create
        public ActionResult Create(int? activoId)
        {
            // 1. Variable para guardar el nombre que queremos pre-seleccionar
            string nombrePreseleccionado = null;

            // 2. Si recibimos un ID (viniendo desde la tabla de Activos)
            if (activoId.HasValue)
            {
                // Buscamos el activo en la lista estática
                var activoEncontrado = ActivosController._activos.Find(a => a.Id == activoId.Value);
                if (activoEncontrado != null)
                {
                    nombrePreseleccionado = activoEncontrado.Nombre;
                }
            }

            // 3. Creamos el SelectList
            // Parámetro 4: 'nombrePreseleccionado' es el valor que el dropdown mostrará por defecto
            ViewBag.ListaActivos = new SelectList(ActivosController._activos, "Nombre", "Nombre", nombrePreseleccionado);

            return PartialView();
        }

        // POST: Riesgos/Create (Guarda los datos)
        [HttpPost]
        public ActionResult Create(Riesgo model)
        {
            // Asignar ID autoincremental
            model.Id = _riesgos.Count + 1;

            // Guardar en nuestra "Base de Datos" temporal
            _riesgos.Add(model);

            // Redirigir al Index actualizado
            return PartialView("Index", _riesgos);
        }
    }
}