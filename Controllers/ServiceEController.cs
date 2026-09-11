using GemBox.Email;
using GemBox.Email.Smtp;
using Homer_MVC.Models;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Homer_MVC.Controllers
{
    public class ServiceEController : Controller
    {
        private DCS_UNISEntities db = new DCS_UNISEntities();
        private OracleDbContext dbContext = new OracleDbContext();

        string CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;



        [AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult ServiceERequest(int? IsSuccess = 0, string d = "", string q = "")
        {

            ViewBag.EstadoCivil = false;
            ViewBag.success = (IsSuccess == 1) ? "success" : "initialize";

            var model = ObtieneCookies("SRV-INFO") ?? new MailExt(); // NUNCA null

            if (!string.IsNullOrEmpty(d))
            {
                model.Nombre_Departamento = d; // este debe coincidir con el campo <select> del form
            }

            if (!string.IsNullOrEmpty(q))
            {
                model.problema = q; // este debe coincidir con el textarea/input de la descripción
            }

            CargarListas(model);                                     // llena listas con seguridad
            return View(model);
        }
        private void CargarListas(MailExt m)
        {
            m.Departamentos = dbContext.Departamento
                .Select(d => new SelectListItem { Value = d.ID_DEPARTAMENTO.ToString(), Text = d.NOMBRE_DEPTO })
                .ToList();

            int depId;
            if (int.TryParse(m.Nombre_Departamento, out depId) && depId > 0)
            {
                m.Productos = dbContext.Producto
                    .Where(p => p.ID_CATEGORIA == depId)
                    .Select(p => new SelectListItem { Value = p.ID_PRODUCTO.ToString(), Text = p.NOMBRE_PRODUCTO })
                    .ToList();
            }
            else
            {
                m.Productos = dbContext.Producto
                    .Select(p => new SelectListItem { Value = p.ID_PRODUCTO.ToString(), Text = p.NOMBRE_PRODUCTO })
                    .ToList();
            }
        }
        public string GetEmplidByDpi(string dpi)
        {
            string constr = Session["conexion_txt"].ToString();
            string emplid = "";

            using (OracleConnection con = new OracleConnection(constr))
            {
                con.Open();
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = "SELECT EMPLID FROM SYSADM.PS_PERS_NID PN WHERE PN.NATIONAL_ID = :nationalId";
                    cmd.Parameters.Add(":nationalId", dpi);

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            emplid = reader["EMPLID"].ToString();
                        }
                    }
                }
            }
            return emplid;
        }
        public ActionResult TestDepartamento(int? department, string query, string user_rol)
        {
            return Content("OK - entro a TestDepartamento");
        }
        public ActionResult GetDepartamento(int? department, string query, string user_rol)
        {
            return Content($"OK GET - dept: {department}, query: {query}, rol: {user_rol}");
        }


        public ActionResult GetForm(int? department, string query, string user_rol)
        {
            try {
                Session.Remove("department");
                Session.Remove("query");
                Session["department"] = department;
                Session["query"] = query;


                if (string.IsNullOrEmpty(user_rol))
                    return Json(new { success = false, message = "No se encontró el Rol de Usuario." }, JsonRequestBehavior.AllowGet);

                var rol = user_rol.ToLower();
                if (rol != "estudiante" && rol != "administrativo" && rol != "externo")
                    return Json(new { success = false, message = "El Rol de Usuario no es válido." }, JsonRequestBehavior.AllowGet);

                if (rol == "estudiante" || rol == "administrativo")
                {
                    var _d = Session["department"];
                    var _q = Session["query"];

                    if (_d == null && _q == null)
                    {
                        return RedirectToAction("ServiceRequest", "Service", new { IsSuccess = 0, d = department, q = query });

                    }
                    else
                    {
                        return RedirectToAction("ServiceRequest", "Service", new { IsSuccess = 0, d = _d, q = _q });
                    }
                }

                if (rol == "externo")
                {
                    return RedirectToAction("ServiceERequest", "ServiceE", new { IsSuccess = 0, d = department, q = query });
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("ServiceERequest", "ServiceE", new { IsSuccess = 0, d = department, q = query });
            }

            return RedirectToAction("ServiceERequest", "ServiceE", new { IsSuccess = 0, d = department, q = query });
        }


        private string ExtraerDepartamento(string contenido)
        {
            var match = Regex.Match(contenido, @"Departamento\s*:\s*(.+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return null;
        }

        private string ExtraerColeccion(string contenido)
        {
            var match = Regex.Match(contenido, @"Collection\s*:\s*(.+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return null;
        }

        private string ExtraerRol(string contenido)
        {
            var match = Regex.Match(contenido, @"User Role\s*:\s*(.+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return null;
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ServiceERequest([Bind(Include = "Nombre_Departamento,IdCampus,Nombre_Producto,PrimerNombre,SegundoNombre,PrimerApellido,SegundoApellido,CUI,correo,problema,ArchivoAdjunto")] MailExt mail)
        {
            var recaptchaHelper = this.GetRecaptchaVerificationHelper();
            string nombreArchivo = "";
            string storedName = "";
            if (String.IsNullOrEmpty(recaptchaHelper.Response))
            {
                ModelState.AddModelError("captcha", "Debes llenar el Captcha!");

            }

            //if (mail.correo != null)
            //{
            //    if (!mail.correo.Contains("unis.edu.gt"))//verifica que el correo sea de la UNIS
            //    {
            //        ModelState.AddModelError("correo", "El correo electrónico debe perteneer a la UNIS");
            //    }
            //}
            //se almacena el archivo adjunto
            try
            {
                if (mail.ArchivoAdjunto != null && mail.ArchivoAdjunto.ContentLength > 0)
                {
                    string rutaDestino = Server.MapPath("~/ArchivosAdjuntos/"); //ruta donde se guardará el archivo
                    string randomId = Guid.NewGuid().ToString("N"); // Genera un ID único para el archivo
                    nombreArchivo = Path.GetFileName(mail.ArchivoAdjunto.FileName); //nombre del archivo
                    var ext = Path.GetExtension(nombreArchivo);//extensión del archivo
                    storedName = randomId + ext;//nombre con el que se guardará el archivo
                    string rutaCompleta = Path.Combine(rutaDestino, storedName);//ruta completa del archivo

                    if (!Directory.Exists(rutaDestino))//si no existe la ruta, se crea
                        Directory.CreateDirectory(rutaDestino);

                    mail.ArchivoAdjunto.SaveAs(rutaCompleta);//se guarda el archivo en la ruta
                }
            }
            catch (Exception ex)
            {
                // Puedes registrar el error o mostrarlo en una vista para depuración:
                ModelState.AddModelError("", "Error al guardar el archivo: " + ex.Message);
                CargarListas(mail);
                return View(mail); // O como manejes tus errores
            }
            //var recaptchaResult = recaptchaHelper.VerifyRecaptchaResponse();

            //if (!recaptchaResult.Success)
            //{
            //    foreach (var err in recaptchaResult.ErrorCodes)
            //    {
            //        ModelState.AddModelError("captcha", err);
            //    }
            //}

            if (ModelState.IsValid)
            {
                List<ProductoModel> Subject;
                string sub = "", sub_completo = "", cola = "";
                try
                {
                    Subject = (from x in dbContext.Producto select x).ToList();//se obtiene la lista de productos
                    var registro = Subject.Where(p => p.ID_PRODUCTO == Int32.Parse(mail.Nombre_Producto)).FirstOrDefault();//se obtiene el producto seleccionado
                    sub = registro.NOMBRE_PRODUCTO;//se obtiene el nombre del producto
                    cola = (from x in dbContext.Departamento
                            where x.ID_DEPARTAMENTO == registro.ID_CATEGORIA
                            select x.CORREO_EXTERNO).FirstOrDefault();//se obtiene la cola del producto
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.InnerException);
                }
                string enlaceServidor = Request.Url.GetLeftPart(UriPartial.Authority) + Url.Content("~/ArchivosAdjuntos/" + storedName);
                string body = "Nombres: " + mail.PrimerNombre + " " + mail.SegundoNombre + "\n"
                    + "Apellidos: " + mail.PrimerApellido + " " + mail.SegundoApellido + "\n"
                    //+ "CUI: " + mail.CUI + "\n"
                    + "Correo: " + mail.correo + "\n"
                    + "Producto: " + sub + "\n\n"
                    + "Solicitud: " + mail.problema + "\n\n"
                     + "Archivo Adjunto: " + enlaceServidor;

                sub_completo = "Solicitud: " + sub;

                SendMail(cola, mail.correo, sub_completo, body); //método para enviar el correo
                AlmacenaCookies(mail.correo, "", mail.PrimerNombre, mail.SegundoNombre, mail.PrimerApellido, mail.SegundoNombre, mail.ArchivoAdjunto);
                return RedirectToAction("ServiceERequest", "ServiceE", new { IsSuccess = 1 });

            }
            else
            {
                CargarListas(mail);
                ViewBag.success = "error";
                return View(mail);
            }


        }

        public void SendMail(string receiver, string sender, string subject, string body)
        {
            string manageMailAccount = "";
            string ManageMailPass = "";
            try
            {
                var registro = dbContext.Parametro.Where(p => p.PARAMETRO.Equals("manageMailAccount")).FirstOrDefault();
                var registro2 = dbContext.Parametro.Where(p => p.PARAMETRO.Equals("manageAppGPass")).FirstOrDefault();
                manageMailAccount = registro.VALOR;
                ManageMailPass = registro2.VALOR;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException.ToString());
            }


            // Create new message.
            ComponentInfo.SetLicense("FREE-LIMITED-KEY");
            MailMessage message = new MailMessage(
                new MailAddress(sender, "Usuario DCS"),
                new MailAddress(receiver, "Destinatario"));

            var security = GemBox.Email.Security.ConnectionSecurity.Ssl;
            //System.Net.Security.RemoteCertificateValidationCallback ignoreCertificate = ("", certificate, chain, errors) => true;

            // Set subject and text body.
            message.Cc.Add(new MailAddress(sender, "Copia Solicitud"));
            message.Subject = subject;
            message.BodyText = body;



            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 465, security))
            {
                smtp.Connect();
                smtp.Authenticate(manageMailAccount, ManageMailPass);
                smtp.SendMessage(message);
            }
        }
        public ActionResult AlmacenaCookies(string correo, string cui, string nombres, string apellidos, string problema, string apellidoCasada, HttpPostedFileBase ArchivoAdjunto)
        {
            // Create the cookie object.
            HttpCookie cookie = new HttpCookie("SRV-INFO");
            cookie["correo"] = correo;
            cookie["cui"] = cui;
            cookie["nombres"] = nombres;
            cookie["apellidos"] = apellidos;
            cookie["apellidoCasada"] = apellidoCasada;
            cookie["problema"] = problema;
            // cookie["ArchivoAdjunto"] = ArchivoAdjunto;
            // This cookie will remain  for one month.
            cookie.Expires = DateTime.Now.AddMonths(1);

            // Add it to the current web response.
            Response.Cookies.Add(cookie);

            return View();
        }

        public ActionResult AlmacenaCookiesExterno(string correo, string cui, string primerNombre, string segundoNombre, string primerApellido, string segundoApellido)
        {
            // Create the cookie object.
            HttpCookie cookie = new HttpCookie("SRV-INFO");
            cookie["correo"] = correo;
            cookie["cui"] = cui;
            cookie["primerNombre"] = primerNombre;
            cookie["segundoNombre"] = segundoNombre;
            cookie["primerApellido"] = primerApellido;
            cookie["segundoApellido"] = segundoApellido;
            // This cookie will remain  for one month.
            cookie.Expires = DateTime.Now.AddMonths(1);

            // Add it to the current web response.
            Response.Cookies.Add(cookie);

            return View();
        }


        public MailExt ObtieneCookies(string key)
        {
            HttpCookie cookieObj = Request.Cookies[key];
            if (cookieObj != null)
            {
                MailExt savedUser = new MailExt();
                savedUser.correo = cookieObj["correo"];
                //savedUser.CUI = cookieObj["cui"];
                savedUser.PrimerNombre = cookieObj["primerNombre"];
                savedUser.SegundoNombre = cookieObj["segundoNombre"];
                savedUser.PrimerApellido = cookieObj["primerApellido"];
                savedUser.SegundoApellido = cookieObj["segundoApellido"];

                return savedUser;
            }
            else
            {
                return null;
            }

        }

        public JsonResult GetProductosList(int departamentoId)
        {
            var productos = dbContext.Producto
                .Where(x => x.ID_CATEGORIA == departamentoId)
                .Select(x => new
                {
                    x.ID_PRODUCTO,
                    x.NOMBRE_PRODUCTO
                })
                .ToList();

            return Json(productos, JsonRequestBehavior.AllowGet);
        }



    }
}