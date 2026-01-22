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

                // Guardamos AMBOS para tener persistencia en el reenvío
                Session["UserPending2FA"] = usuario.IdUsuario;
                Session["TempEmail"] = usuario.Correo; // <--- AGREGAR ESTO

                return RedirectToAction("VerificarCodigo");
            }
            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }

        [HttpPost]
        public JsonResult ResendCode()
        {
            try
            {
                // 1. Recuperar el ID del usuario pendiente
                int idUsuario = (int)(Session["UserPending2FA"] ?? 0);
                string email = Session["TempEmail"]?.ToString();

                if (idUsuario == 0 || string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Sesión expirada. Inicie sesión de nuevo." });
                }

                // 2. Generar nuevo código
                string nuevoCodigo = new Random().Next(100000, 999999).ToString();

                // 3. Actualizar en la base de datos (IMPORTANTE: Debe guardarse en la DB para que VerificarCodigo lo reconozca)
                var usuario = db.Usuarios.Find(idUsuario);
                if (usuario == null) return Json(new { success = false, message = "Usuario no encontrado." });

                usuario.Codigo2FA = nuevoCodigo;
                usuario.FechaExpiracion2FA = DateTime.Now.AddMinutes(5);
                db.SaveChanges();

                // 4. Enviar el correo real
                EnviarCorreo(email, nuevoCodigo);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
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
            mensaje.Subject = "🔐 Código de Seguridad - SGRC";

            var bodyBuilder = new BodyBuilder();

            // Ruta física del logo (Asegúrate de tener logo-azul.png o similar para mejor compatibilidad)
            // Si necesitas usar el SVG, la mayoría de clientes lo bloquearán; se recomienda PNG para emails.
            string pathLogo = Server.MapPath("~/Content/Images/logo-azul.svg");
            var image = bodyBuilder.LinkedResources.Add(pathLogo);
            image.ContentId = "logo_sgrc";

            // Plantilla HTML Premium
            bodyBuilder.HtmlBody = $@"
    <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f9; padding: 40px 20px; color: #333;'>
        <div style='max-width: 500px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
            <div style='background-color: white; padding: 30px; text-align: center;'>
                <img src=""cid:logo_sgrc"" alt='SGRC Logo' style='width: 100px; height: auto;' />
                <h1 style='color: #004aad; margin-top: 15px; font-size: 30px; letter-spacing: 1px;'>Verificación de Seguridad</h1>
            </div>

            <div style='padding: 40px; text-align: center;'>
                <p style='font-size: 16px; color: #64748b; margin-bottom: 25px;'>Has solicitado el acceso al sistema. Utiliza el siguiente código para completar tu inicio de sesión:</p>
                
                <div style='background-color: #f8fafc; border: 2px dashed #cbd5e0; border-radius: 12px; padding: 20px; display: inline-block; margin-bottom: 25px;'>
                    <span style='font-size: 36px; font-weight: bold; color: #0b1e3b; letter-spacing: 8px;'>{codigo}</span>
                </div>

                <p style='font-size: 13px; color: #94a3b8;'>Este código expirará en <strong style='color: #d62828;'>5 minutos</strong> por motivos de seguridad.</p>
            </div>

            <div style='background-color: #f1f5f9; padding: 20px; text-align: center; font-size: 12px; color: #64748b;'>
                <p style='margin: 0;'>Si no solicitaste este código, ignora este correo o contacta al administrador.</p>
                <p style='margin: 5px 0 0 0;'>&copy; {DateTime.Now.Year} SGRC - Gestión de Riesgos y Cumplimiento.</p>
            </div>
        </div>
    </div>";

            mensaje.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect("smtp.gmail.com", 465, true);
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