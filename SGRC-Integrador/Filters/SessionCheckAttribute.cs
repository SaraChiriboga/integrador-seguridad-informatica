using System;
using System.Linq;
using System.Web.Mvc;
using SGRC_Integrador.Models;

namespace SGRC_Integrador.Filters
{
    public class SessionCheckAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            var user = filterContext.HttpContext.User;

            System.Diagnostics.Debug.WriteLine($"=== SESSION CHECK ===");
            System.Diagnostics.Debug.WriteLine($"SessionID: {session?.SessionID}");
            System.Diagnostics.Debug.WriteLine($"UsuarioNombre en Session: {session?["UsuarioNombre"]}");
            System.Diagnostics.Debug.WriteLine($"User.Identity.IsAuthenticated: {user.Identity.IsAuthenticated}");
            System.Diagnostics.Debug.WriteLine($"User.Identity.Name: {user.Identity.Name}");

            // Si la sesión está vacía pero el usuario está autenticado, restaurar
            if (session != null && session["UsuarioNombre"] == null && user.Identity.IsAuthenticated)
            {
                System.Diagnostics.Debug.WriteLine($"⚠ RESTAURANDO SESIÓN para: {user.Identity.Name}");

                using (var db = new SGRC_DBEntities())
                {
                    var usuario = db.Usuarios.FirstOrDefault(u => u.Correo == user.Identity.Name);
                    if (usuario != null)
                    {
                        session["UsuarioNombre"] = usuario.Nombre;
                        session["UsuarioId"] = usuario.IdUsuario;
                        session["UsuarioCorreo"] = usuario.Correo;
                        System.Diagnostics.Debug.WriteLine($"✓ Sesión restaurada para: {usuario.Nombre}");
                    }
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}