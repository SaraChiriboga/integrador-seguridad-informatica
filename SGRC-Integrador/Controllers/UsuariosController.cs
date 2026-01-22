using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SGRC_Integrador.Controllers
{
    public class UsuariosController : Controller
    {
        // GET: Usuarios
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult Logout()
        {
            // 1. Limpiar la sesión de ASP.NET
            Session.Clear();
            Session.Abandon();

            // 2. Limpiar la cookie de autenticación (si usas FormsAuthentication)
            System.Web.Security.FormsAuthentication.SignOut();

            // 3. Redirigir al Login
            return RedirectToAction("Login", "Auth");
        }
    }
}