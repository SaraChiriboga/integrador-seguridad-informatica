using SGRC_Integrador.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SGRC_Integrador.Controllers
{
    public class ActivosController : Controller
    {
        // Simulación de Base de Datos (Estática para que persista mientras corre la app)
        public static List<Activo> _activos = new List<Activo>
{
    // Hardware Crítico
    new Activo { Id = 1, Nombre = "Data Center Principal UIO", Tipo = "INST", Ubicacion = "Quito - Iñaquito", ValorC = 4, ValorI = 4, ValorD = 4 },
    new Activo { Id = 2, Nombre = "Switch Core MPLS Huawei", Tipo = "HW", Ubicacion = "DC Guayaquil", ValorC = 3, ValorI = 4, ValorD = 4 },
    new Activo { Id = 3, Nombre = "OLT Fibra Óptica (Nodo Norte)", Tipo = "HW", Ubicacion = "Carcelén", ValorC = 2, ValorI = 3, ValorD = 4 },

    // Datos Sensibles
    new Activo { Id = 4, Nombre = "Base de Datos Clientes (CRM)", Tipo = "DATOS", Ubicacion = "Servidor DB01", ValorC = 4, ValorI = 4, ValorD = 3 },
    new Activo { Id = 5, Nombre = "Registros de Llamadas (CDR)", Tipo = "DATOS", Ubicacion = "Storage SAN", ValorC = 4, ValorI = 4, ValorD = 2 },
    new Activo { Id = 6, Nombre = "Facturación Electrónica", Tipo = "DATOS", Ubicacion = "Nube Híbrida", ValorC = 3, ValorI = 4, ValorD = 3 },

    // Software y Servicios
    new Activo { Id = 7, Nombre = "Portal Web de Pagos", Tipo = "SERV", Ubicacion = "DMZ Web", ValorC = 3, ValorI = 3, ValorD = 4 },
    new Activo { Id = 8, Nombre = "Sistema de Gestión SAP", Tipo = "SW", Ubicacion = "Cluster Virtual", ValorC = 4, ValorI = 4, ValorD = 3 },
    
    // Personal
    new Activo { Id = 9, Nombre = "Administradores de Base de Datos", Tipo = "PERSONAL", Ubicacion = "Gerencia TI", ValorC = 3, ValorI = 3, ValorD = 2 }
};

        // GET: Activos
        public ActionResult Index()
        {
            // Retorna solo el HTML parcial para inyectar en el div
            return PartialView(_activos);
        }

        // GET: Activos/Create
        public ActionResult Create()
        {
            return PartialView();
        }

        // POST: Activos/Create
        [HttpPost]
        public ActionResult Create(Activo model)
        {
            // Lógica de guardado
            model.Id = _activos.Count + 1;
            _activos.Add(model);

            // Retornamos la vista Index actualizada para que la tabla se refresque
            return PartialView("Index", _activos);
        }
    }
}