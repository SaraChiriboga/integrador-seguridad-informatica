using DocumentFormat.OpenXml.Vml;
using MailKit.Net.Smtp;
using MimeKit;
using SGRC_Integrador.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace SGRC_Integrador.Controllers
{
    public class AuthController : Controller
    {
        private SGRC_DBEntities db = new SGRC_DBEntities();

        public ActionResult Login() => View();

        [HttpPost]
        public ActionResult Login(string correo, string password)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Correo == correo && u.PasswordHash == password);
            if (usuario != null)
            {
                string codigo = new Random().Next(100000, 999999).ToString();
                usuario.Codigo2FA = codigo;
                usuario.FechaExpiracion2FA = DateTime.Now.AddMinutes(5);
                db.SaveChanges();

                EnviarCorreo(usuario.Correo, codigo);

                Session["UserPending2FA"] = usuario.IdUsuario;
                return RedirectToAction("VerificarCodigo");
            }
            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }

        public ActionResult VerificarCodigo() => View();

        [HttpPost]
        public ActionResult VerificarCodigo(string codigo)
        {
            int idUsuario = (int)(Session["UserPending2FA"] ?? 0);
            var usuario = db.Usuarios.Find(idUsuario);
            if (usuario != null && usuario.Codigo2FA == codigo && usuario.FechaExpiracion2FA > DateTime.Now)
            {
                // --- DATOS QUE SOLICITASTE CONSERVAR ---
                Session["UsuarioNombre"] = usuario.Nombre;
                Session["HoraInicioSesion"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                // AGREGAR ESTAS LÍNEAS DE DEPURACIÓN
                Session["UsuarioId"] = usuario.IdUsuario;
                Session["UsuarioCorreo"] = usuario.Correo;

                // VERIFICACIÓN INMEDIATA
                System.Diagnostics.Debug.WriteLine($"✓ Sesión guardada - Nombre: {Session["UsuarioNombre"]}");
                System.Diagnostics.Debug.WriteLine($"✓ SessionID: {Session.SessionID}");

                // Cookie de autenticación para controlar inactividad
                FormsAuthentication.SetAuthCookie(usuario.Correo, false);
                usuario.Codigo2FA = null; // Limpiar código
                db.SaveChanges();

                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Código inválido o expirado.";
            return View();
        }

        private void EnviarCorreo(string destino, string codigo)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("Sistema SGRC", "sarilola10@gmail.com"));
            mensaje.To.Add(new MailboxAddress("Usuario", destino));
            mensaje.Subject = "Código de Seguridad";
            mensaje.Body = new TextPart("plain") { Text = $"Tu código es: {codigo}. Expira en 5 min." };

            using (var client = new SmtpClient())
            {
                // 1. Evita errores de certificados locales que causan Timeouts
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // 2. Cambia a puerto 465 con SSL directo (es más estable para Google)
                client.Connect("smtp.gmail.com", 465, true);

                // 3. Autenticación con tu App Password
                client.Authenticate("sarilola10@gmail.com", "mdzl kroq vkhf rjxf");

                client.Send(mensaje);
                client.Disconnect(true);
            }
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}