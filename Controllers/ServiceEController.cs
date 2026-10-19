using GemBox.Email;
using GemBox.Email.Smtp;
using Homer_MVC.Models;
using Homer_MVC.Services;
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
using System.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Homer_MVC.Controllers
{
    public class ServiceEController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ServiceEController));
        private DCS_UNISEntities db = new DCS_UNISEntities();
        private OracleDbContext dbContext = new OracleDbContext();

        string CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;



        [AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult ServiceERequest(int? IsSuccess = 0, string department = "", string description = "", int? tipoSolicitud = null)
        {

            ViewBag.EstadoCivil = false;
            ViewBag.success = (IsSuccess == 1) ? "success" : "initialize";

            var model = ObtieneCookies("SRV-INFO") ?? new MailExt(); // NUNCA null

            if (!string.IsNullOrEmpty(department))
            {
                model.Nombre_Departamento = department; // este debe coincidir con el campo <select> del form
            }

            if (!string.IsNullOrEmpty(description))
            {
                model.problema = description; // este debe coincidir con el textarea/input de la descripción
            }

            if (tipoSolicitud.HasValue)
            {
                model.Nombre_Producto = tipoSolicitud.Value.ToString(); // debe coincidir con ID_TIPO_SOLICITUD
            }

            CargarListas(model);                                     // llena listas con seguridad
            return View(model);
        }
        // Ver ServiceController.CargarListas y Helpers/CatalogoDcs.cs
        private void CargarListas(MailExt m)
        {
            var catalogo = new CatalogoDcs(dbContext);

            var tipoId = CatalogoDcs.ParseId(m.Nombre_Producto);
            if (tipoId != null && CatalogoDcs.ParseId(m.Nombre_Categoria) == null)
            {
                var catId = catalogo.CategoriaDeTipo(tipoId.Value);
                var depId = catId != null ? catalogo.DepartamentoDeCategoria(catId.Value) : null;
                if (depId != null)
                {
                    m.Nombre_Categoria = catId.ToString();
                    m.Nombre_Departamento = depId.ToString();
                }
                else
                {
                    m.Nombre_Producto = null;
                }
            }

            m.Departamentos = catalogo.Departamentos();
            m.Categorias = catalogo.Categorias(CatalogoDcs.ParseId(m.Nombre_Departamento));
            m.Productos = catalogo.Tipos(CatalogoDcs.ParseId(m.Nombre_Categoria));
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

        public ActionResult GetForm(int? department, string description, int? tipoSolicitud, string origen, string user_rol)
        {
            try {
                Session.Remove("department");
                Session.Remove("query");
                Session.Remove("tipoSolicitud");
                Session.Remove("origen");
                Session["department"] = department;
                Session["query"] = description;
                Session["tipoSolicitud"] = tipoSolicitud;
                Session["origen"] = origen;


                if (string.IsNullOrEmpty(user_rol))
                    return Json(new { success = false, message = "No se encontró el Rol de Usuario." }, JsonRequestBehavior.AllowGet);

                var rol = user_rol.ToLower();
                if (rol != "estudiante" && rol != "administrativo" && rol != "externo")
                    return Json(new { success = false, message = "El Rol de Usuario no es válido." }, JsonRequestBehavior.AllowGet);

                if (rol == "estudiante" || rol == "administrativo")
                {
                    var sessionDepartment = Session["department"];
                    var sessionDescription = Session["query"];
                    var sessionTipoSolicitud = Session["tipoSolicitud"];
                    var sessionOrigen = Session["origen"];

                    if (sessionDepartment == null && sessionDescription == null && sessionTipoSolicitud == null && sessionOrigen == null)
                    {
                        return RedirectToAction("ServiceRequest", "Service", new { IsSuccess = 0, department = department, description = description, tipoSolicitud = tipoSolicitud, origen = origen });

                    }
                    else
                    {
                        return RedirectToAction("ServiceRequest", "Service", new { IsSuccess = 0, department = sessionDepartment, description = sessionDescription, tipoSolicitud = sessionTipoSolicitud, origen = sessionOrigen });
                    }
                }

                if (rol == "externo")
                {
                    return RedirectToAction("ServiceERequest", "ServiceE", new { IsSuccess = 0, department = department, description = description, tipoSolicitud = tipoSolicitud, origen = origen });
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("ServiceERequest", "ServiceE", new { IsSuccess = 0, department = department, description = description, tipoSolicitud = tipoSolicitud, origen = origen });
            }

            return RedirectToAction("ServiceERequest", "ServiceE", new { IsSuccess = 0, department = department, description = description, tipoSolicitud = tipoSolicitud, origen = origen });
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
        public ActionResult ServiceERequest([Bind(Include = "Nombre_Departamento,Nombre_Categoria,IdCampus,Nombre_Producto,PrimerNombre,SegundoNombre,PrimerApellido,SegundoApellido,CUI,correo,problema,ArchivoAdjunto")] MailExt mail)
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
                string sub = "", sub_completo = "", cola = "";
                int? idTipoSolicitud = CatalogoDcs.ParseId(mail.Nombre_Producto);
                int? idDepartamentoForm = CatalogoDcs.ParseId(mail.Nombre_Departamento);
                try
                {
                    // Con tipo: cola de Fusion del tipo. Solo departamento: canal de Fusion del departamento.
                    var catalogo = new CatalogoDcs(dbContext);
                    var registro = idTipoSolicitud == null
                        ? null
                        : dbContext.TipoSolicitud.FirstOrDefault(t => t.ID_TIPO_SOLICITUD == idTipoSolicitud);
                    sub = registro != null ? registro.NOMBRE_TIPO_SOLICITUD : catalogo.NombreDepartamento(idDepartamentoForm);
                    cola = catalogo.CorreoDestino(registro, idDepartamentoForm, externo: true);
                }
                catch (Exception ex)
                {
                    log.Error("ServiceERequest: lookup de tipo de solicitud/categoria/departamento", ex);
                }
                string lineaAdjunto = string.IsNullOrEmpty(storedName)
                    ? ""
                    : "Archivo Adjunto: " + Request.Url.GetLeftPart(UriPartial.Authority) + Url.Content("~/ArchivosAdjuntos/" + storedName);
                string body = "Nombres: " + mail.PrimerNombre + " " + mail.SegundoNombre + "\n"
                    + "Apellidos: " + mail.PrimerApellido + " " + mail.SegundoApellido + "\n"
                    //+ "CUI: " + mail.CUI + "\n"
                    + "Correo: " + mail.correo + "\n"
                    + "Producto: " + sub + "\n\n"
                    + "Solicitud: " + mail.problema + "\n\n"
                     + lineaAdjunto;

                var solicitud = new SolicitudModel
                {
                    ID_TIPO_SOLICITUD = idTipoSolicitud,
                    ID_DEPARTAMENTO = idDepartamentoForm,
                    EMAIL_SOLICITANTE = mail.correo,
                    DESCRIPCION = mail.problema,
                    FECHA_CREACION = DateTime.Now,
                    ESTADO = "PENDIENTE",
                    ORIGEN = Session["origen"]?.ToString()
                };

                if (string.IsNullOrEmpty(cola))
                {
                    solicitud.ESTADO = "ERROR";
                    dbContext.Solicitud.Add(solicitud);
                    dbContext.SaveChanges();
                    log.Error("ServiceERequest: sin correo de destino para ID_TIPO_SOLICITUD " + idTipoSolicitud + " / ID_DEPARTAMENTO " + idDepartamentoForm);
                    ModelState.AddModelError("", "No se pudo determinar el departamento destino. Contacta a soporte.");
                    CargarListas(mail);
                    ViewBag.success = "error";
                    return View(mail);
                }

                // Igual que ServiceController: el ID va en el asunto como [DCS-<id>] para encontrar el SR
                dbContext.Solicitud.Add(solicitud);
                dbContext.SaveChanges();
                int idSolicitud = solicitud.ID_SOLICITUD.Value;
                string tag = OracleFusionService.SolicitudTag(idSolicitud);
                sub_completo = tag + " Solicitud: " + sub;

                try
                {
                    string overrideRecipient = ConfigurationManager.AppSettings["DcsMail:OverrideRecipient"];
                    string destinatario = !string.IsNullOrEmpty(overrideRecipient) ? overrideRecipient : cola;
                    SendMail(destinatario, mail.correo, sub_completo, body); //método para enviar el correo
                    solicitud.ESTADO = "ENVIADO";
                }
                catch (Exception ex)
                {
                    solicitud.ESTADO = "ERROR";
                    log.Error("ServiceERequest: fallo el envio de correo", ex);
                }
                dbContext.SaveChanges();

                if (solicitud.ESTADO == "ENVIADO")
                {
                    string nombres = (mail.PrimerNombre + " " + mail.SegundoNombre).Trim();
                    string apellidos = (mail.PrimerApellido + " " + mail.SegundoApellido).Trim();
                    ServiceController.VincularSrDelCorreo(idSolicitud, tag, nombres, apellidos, mail.correo, null);
                }

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
            // Las credenciales ya no viven en TBL_DCS_PARAMETRO (se guardaban en texto plano).
            // Ahora salen de Web.config / secrets.
            string manageMailAccount = System.Configuration.ConfigurationManager.AppSettings["DcsMail:Account"];
            string ManageMailPass = System.Configuration.ConfigurationManager.AppSettings["DcsMail:Password"];

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

        // Dropdowns encadenados del form: {Value, Text} de SelectListItem
        public JsonResult GetCategoriasList(int departamentoId)
        {
            return Json(new CatalogoDcs(dbContext).Categorias(departamentoId), JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetProductosList(int categoriaId)
        {
            return Json(new CatalogoDcs(dbContext).Tipos(categoriaId), JsonRequestBehavior.AllowGet);
        }



    }
}