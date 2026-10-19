using GemBox.Email;
using GemBox.Email.Smtp;
using Homer_MVC.Models;
using Homer_MVC.Services;
using Oracle.ManagedDataAccess.Client;
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;


namespace Homer_MVC.Controllers
{
    public class ServiceController : Controller
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ServiceController));
        //private DCS_UNISEntities db = new DCS_UNISEntities();
        private OracleDbContext dbContext = new OracleDbContext(); // Contexto de la base de datos
        private OracleFusionService fusionService = new OracleFusionService();

        string CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;



        // GET: Service
        [Authorize]
        public ActionResult ServiceRequest(int? IsSuccess = 0, string department = "", string description = "", int? tipoSolicitud = null, string origen = "")
        {

            LeerInfoTxt();


            ViewData["EstadoCivil"] = null;// Inicializar el estado civil como falso

            



            if (IsSuccess == 1)
            {
                ViewBag.success = "success";

            }
            else
            {
                ViewBag.success = "initialize";
            }
            //MailEAModel cookieMail = ObtieneCookies("SRV-INFO");
            //if (cookieMail != null)
            //{
            //    return View(cookieMail);
            //}
            //else
            //{
            //    return View();
            //}
            var dpi = User.Identity.Name.Replace("@unis.edu.gt", "");
            var emplid = GetEmplidByDpi(dpi);

            Session.Remove("emplid");
            Session["emplid"] = emplid;
            try
            {

                if (string.IsNullOrEmpty(department))
                {
                    department = Session["department"]?.ToString() ?? "";
                }

                if (string.IsNullOrEmpty(description))
                {
                    description = Session["query"]?.ToString() ?? "";

                }

                if (tipoSolicitud == null)
                {
                    tipoSolicitud = Session["tipoSolicitud"] as int?;
                }

                if (string.IsNullOrEmpty(origen))
                {
                    origen = Session["origen"]?.ToString() ?? "";
                }
                else
                {
                    Session["origen"] = origen;
                }
            }
            catch (Exception ex) {

            }

            return MostrarInformacion(emplid,department,description,tipoSolicitud);

            //return MostrarInformacion("00000021543");
            //return MostrarInformacion("00000023478");


        }

        // Departamento (obligatorio) -> Categoria -> Solicitud; ver Helpers/CatalogoDcs.cs
        private void CargarListas(MailEAModel m)
        {
            var catalogo = new CatalogoDcs(dbContext);

            // Tipo preseleccionado (link con tipoSolicitud) sin categoria: se deducen categoria y departamento,
            // asi el form queda consistente aunque el link traiga otro departamento
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
                    m.Nombre_Producto = null;   // tipo inexistente o sin departamento: se ignora
                }
            }

            m.Departamentos = catalogo.Departamentos();
            m.Categorias = catalogo.Categorias(CatalogoDcs.ParseId(m.Nombre_Departamento));
            m.Productos = catalogo.Tipos(CatalogoDcs.ParseId(m.Nombre_Categoria));
        }


        void LeerInfoTxt()
        {
            string rutaCompleta = CurrentDirectory + "conexion.txt";
            Session.Remove("conexion_txt");
            //string line = "";
            using (StreamReader file = new StreamReader(rutaCompleta))
            {
                string line = file.ReadToEnd();
                Session["conexion_txt"] = line;
                //TxtURL.Text = line;
                file.Close();
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ServiceRequest([Bind(Include = "Nombre_Departamento,Nombre_Categoria,IdCampus,Nombre_Producto,Nombres,Apellidos,ApellidoCasada,CUI,correo,problema,ArchivoAdjunto")] MailEAModel mail)
        {
            var recaptchaHelper = this.GetRecaptchaVerificationHelper();
            var estadoCivil = ViewData["EstadoCivil"] ;
            string nombreArchivo = "";
            string storedName = "";
            if (String.IsNullOrEmpty(recaptchaHelper.Response))
            {
                ModelState.AddModelError("captcha", "Debes llenar el Captcha!");

            }

            if (mail.correo != null)
            {
                if (!mail.correo.Contains("unis.edu.gt"))
                {
                    ModelState.AddModelError("correo", "El correo electrónico debe perteneer a la UNIS");
                }
            }
            try
            {
                if (mail.ArchivoAdjunto != null && mail.ArchivoAdjunto.ContentLength > 0)
                {
                    string rutaDestino = Server.MapPath("~/ArchivosAdjuntos/");
                    string randomId = Guid.NewGuid().ToString("N");
                    nombreArchivo = Path.GetFileName(mail.ArchivoAdjunto.FileName);
                    var ext = Path.GetExtension(nombreArchivo);
                    storedName = randomId + ext;
                    string rutaCompleta = Path.Combine(rutaDestino, storedName);

                    if (!Directory.Exists(rutaDestino))
                        Directory.CreateDirectory(rutaDestino);

                    mail.ArchivoAdjunto.SaveAs(rutaCompleta);
                }
            }
            catch (Exception ex)
            {
                // Puedes registrar el error o mostrarlo en una vista para depuración:
                ModelState.AddModelError("", "Error al guardar el archivo: " + ex.Message);
                CargarListas(mail);
                return View(mail); // O como manejes tus errores
            }

            if (ModelState.IsValid)
            {
                string sub = "", sub_completo = "", cola = "";
                int? idTipoSolicitud = CatalogoDcs.ParseId(mail.Nombre_Producto);
                int? idDepartamentoForm = CatalogoDcs.ParseId(mail.Nombre_Departamento);
                try
                {
                    // Con tipo: cola de Fusion del tipo (101..108). Solo departamento: canal de Fusion del departamento.
                    var catalogo = new CatalogoDcs(dbContext);
                    var registro = idTipoSolicitud == null
                        ? null
                        : dbContext.TipoSolicitud.FirstOrDefault(t => t.ID_TIPO_SOLICITUD == idTipoSolicitud);
                    sub = registro != null ? registro.NOMBRE_TIPO_SOLICITUD : catalogo.NombreDepartamento(idDepartamentoForm);
                    cola = catalogo.CorreoDestino(registro, idDepartamentoForm, externo: false);
                }
                catch (Exception ex)
                {
                    log.Error("ServiceRequest: lookup de tipo de solicitud/categoria/departamento", ex);
                }
                string lineaAdjunto = string.IsNullOrEmpty(storedName)
                    ? ""
                    : "Archivo Adjunto: " + Request.Url.GetLeftPart(UriPartial.Authority) + Url.Content("~/ArchivosAdjuntos/" + storedName);
                string body = "Nombres: " + mail.Nombres + "\n"
                    + "Apellidos: " + mail.Apellidos + "\n"
                    + (estadoCivil != null ? "Apellido de Casada: " + mail.ApellidoCasada : "") + "\n"
                    + "CUI: " + mail.CUI + "\n"
                    + "Id Campus: " + mail.IdCampus + "\n"
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
                    // Sin departamento resuelto no hay a donde enviar. Antes esto tronaba con una
                    // excepcion al construir el MailAddress (el bug original de la categoria 25 huerfana).
                    solicitud.ESTADO = "ERROR";
                    dbContext.Solicitud.Add(solicitud);
                    dbContext.SaveChanges();
                    log.Error("ServiceRequest: sin correo de destino para ID_TIPO_SOLICITUD " + idTipoSolicitud + " / ID_DEPARTAMENTO " + idDepartamentoForm);
                    ModelState.AddModelError("", "No se pudo determinar el departamento destino. Contacta a soporte.");
                    CargarListas(mail);
                    ViewBag.success = "error";
                    return View(mail);
                }

                // Se guarda antes del envio para tener el ID: va en el asunto como [DCS-<id>] y es lo
                // que permite encontrar despues el SR que Fusion crea a partir del correo.
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
                    log.Error("ServiceRequest: fallo el envio de correo", ex);
                }

                // Solo para pruebas: ademas del correo, crea el SR via API de Fusion.
                // No afecta ESTADO (ese sigue reflejando el envio de correo).
                bool createSrViaApi = ConfigurationManager.AppSettings["OracleFusion:CreateSrViaApi"] == "true";
                if (createSrViaApi)
                {
                    try
                    {
                        var srNumber = await fusionService.CreateServiceRequestForSolicitudAsync(
                            mail.Nombres, mail.Apellidos, mail.correo, mail.CUI, sub_completo, body);
                        solicitud.NUMERO_SR = srNumber;
                        log.Info("ServiceRequest: SR creado en Fusion " + srNumber);
                    }
                    catch (Exception ex)
                    {
                        log.Error("ServiceRequest: fallo la creacion del SR via Fusion API", ex);
                    }
                }
                else if (solicitud.ESTADO == "ENVIADO")
                {
                    VincularSrDelCorreo(idSolicitud, tag, mail.Nombres, mail.Apellidos, mail.correo, mail.CUI);
                }

                dbContext.SaveChanges();

                AlmacenaCookies(mail.correo, mail.CUI, mail.Nombres, mail.Apellidos, mail.problema, mail.ApellidoCasada, mail.ArchivoAdjunto);

                return RedirectToAction("ServiceRequest", new { IsSuccess = 1 });
            }
            else
            {
                CargarListas(mail);

                ViewBag.success = "error";
                return View(mail);
            }
        }
        private static readonly TimeSpan SrLinkIntervalo = TimeSpan.FromMinutes(2);
        private const int SrLinkMaxIntentos = 15; // ~30 minutos

        /// <summary>
        /// Fusion crea el SR desde el correo de forma asincrona: se busca por el tag del asunto cada
        /// 2 minutos hasta por 30, y al encontrarlo se completa Contacto/Cuenta y se guarda NUMERO_SR.
        /// .NET 4.5.1 no tiene HostingEnvironment.QueueBackgroundWorkItem; si IIS recicla el app
        /// pool durante la espera, esa solicitud queda con NUMERO_SR null y hay que vincularla a mano.
        /// </summary>
        internal static void VincularSrDelCorreo(int idSolicitud, string tag, string firstName, string lastName, string email, string nationalId)
        {
            Task.Run(async () =>
            {
                var fusion = new OracleFusionService();
                for (int intento = 1; intento <= SrLinkMaxIntentos; intento++)
                {
                    await Task.Delay(SrLinkIntervalo);
                    try
                    {
                        var srNumber = await fusion.FindServiceRequestByTitleTagAsync(tag);
                        if (srNumber == null)
                        {
                            continue;
                        }

                        await fusion.UpdateServiceRequestForSolicitudAsync(srNumber, firstName, lastName, email, nationalId);
                        using (var db = new OracleDbContext())
                        {
                            var solicitud = db.Solicitud.First(s => s.ID_SOLICITUD == idSolicitud);
                            solicitud.NUMERO_SR = srNumber;
                            db.SaveChanges();
                        }
                        log.Info($"ServiceRequest: {tag} vinculado a {srNumber} (intento {intento})");
                        return;
                    }
                    catch (Exception ex)
                    {
                        log.Error($"ServiceRequest: fallo la vinculacion de {tag} (intento {intento})", ex);
                    }
                }
                log.Error($"ServiceRequest: no se encontro en Fusion un SR con {tag} tras {SrLinkMaxIntentos} intentos; vincular a mano");
            });
        }

        public void SendMail(string receiver, string sender, string subject, string body)
        {
            // Las credenciales ya no viven en TBL_DCS_PARAMETRO (se guardaban en texto plano).
            // Ahora salen de Web.config / secrets.
            string manageMailAccount = ConfigurationManager.AppSettings["DcsMail:Account"];
            string ManageMailPass = ConfigurationManager.AppSettings["DcsMail:Password"];

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
        public MailEAModel ObtieneCookies(string key)
        {
            HttpCookie cookieObj = Request.Cookies[key];
            if (cookieObj != null)
            {
                MailEAModel savedUser = new MailEAModel();
                savedUser.correo = cookieObj["correo"];
                savedUser.CUI = cookieObj["cui"];
                //savedUser.PrimerNombre = cookieObj["primerNombre"];
                //savedUser.SegundoNombre = cookieObj["segundoNombre"];
                //savedUser.PrimerApellido = cookieObj["primerApellido"];
                //savedUser.SegundoApellido = cookieObj["segundoApellido"];

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
        public ActionResult MostrarInformacion(string emplid, string department, string description, int? tipoSolicitud = null)
        {
            var modelo = new MailEAModel();

            // Prellenado del link (chatbot) antes de cargar las listas: Categorias/Productos dependen
            // del departamento y del tipo. Antes se cargaban vacias y el prellenado solo pasaba si
            // PeopleSoft devolvia la persona.
            if (!string.IsNullOrEmpty(department))
                modelo.Nombre_Departamento = department;
            if (!string.IsNullOrEmpty(description))
                modelo.problema = description;
            if (tipoSolicitud.HasValue)
                modelo.Nombre_Producto = tipoSolicitud.Value.ToString();

            CargarListas(modelo);


          


            string constr = Session["conexion_txt"].ToString();
            var apellidoEx = "0";
            int posicion = 0;
            int posicion2 = 0;
            int largoApellido = 0;
            int excepcionApellido = 0;
            string DPI = "";
            using (OracleConnection con = new OracleConnection(constr))
            {
                con.Open();
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = "SELECT NATIONAL_ID FROM SYSADM.PS_PERS_NID PN " +
                    "WHERE EMPLID ='" + emplid + "' ";
                    OracleDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        DPI = reader["NATIONAL_ID"].ToString();
                    }

                    cmd.Connection = con;
                    cmd.CommandText = "SELECT APELLIDO_NIT, NOMBRE_NIT, CASADA_NIT, NIT, PAIS, EMPLID,FIRST_NAME,LAST_NAME,CARNE,PHONE,DPI,CARRERA,FACULTAD,STATUS,BIRTHDATE,DIRECCION,DIRECCION2,DIRECCION3,MUNICIPIO, \r\n" +
                                        "DEPARTAMENTO, SECOND_LAST_NAME, DIRECCION1_NIT, DIRECCION2_NIT, DIRECCION3_NIT, MUNICIPIO_NIT, DEPARTAMENTO_NIT, STATE_NIT, PAIS_NIT, STATE, EMAILUNIS,EMAILPERSONAL, BIRTHCOUNTRY, MUNICIPIO_NAC, DEPARTAMENTO_NAC, BIRTHPLACE, BIRTHSTATE FROM ( \r\n" +
                                        "SELECT PD.EMPLID, PN.NATIONAL_ID CARNE,  PD.FIRST_NAME, \r\n" +
                                        "PD.LAST_NAME, PD.SECOND_LAST_NAME, PN.NATIONAL_ID DPI, PN.NATIONAL_ID_TYPE, \r\n " +
                                        "COALESCE(" +
                                        "        (SELECT PP.PHONE FROM SYSADM.PS_PERSONAL_PHONE PP WHERE PP.EMPLID = PD.EMPLID AND PP.PHONE_TYPE = 'HOME' FETCH FIRST 1 ROWS ONLY), \r\n " +
                                        "        (SELECT PP.PHONE FROM SYSADM.PS_PERSONAL_PHONE PP WHERE PP.EMPLID = PD.EMPLID AND PP.PHONE_TYPE = 'CEL1' FETCH FIRST 1 ROWS ONLY) \r\n" +
                                        "   ) AS PHONE, \r\n" +
                                        "TO_CHAR(PD.BIRTHDATE,'YYYY-MM-DD') BIRTHDATE, PD.BIRTHPLACE, PD.BIRTHSTATE, \r\n" +
                                        "(SELECT BIRTHCOUNTRY FROM SYSADM.PS_PERS_DATA_SA_VW WHERE EMPLID ='" + emplid + "') BIRTHCOUNTRY, \r\n" +
                                        "APD.DESCR CARRERA, AGT.DESCR FACULTAD, \r\n" +
                                        "CASE WHEN PD.MAR_STATUS = 'M' THEN 'Casado' WHEN PD.MAR_STATUS = 'S' THEN 'Soltero' ELSE 'No Consta' END STATUS, \r\n" +
                                        "(SELECT EXTERNAL_SYSTEM_ID FROM SYSADM.PS_EXTERNAL_SYSTEM WHERE EXTERNAL_SYSTEM = 'NRE' AND EMPLID = '" + emplid + "' ORDER BY EFFDT DESC FETCH FIRST 1 ROWS ONLY) NIT, \r\n" +
                                        "(SELECT PNA.FIRST_NAME FROM SYSADM.PS_NAMES PNA WHERE PNA.NAME_TYPE = 'REC' AND PNA.EMPLID='" + emplid + "' ORDER BY EFFDT DESC FETCH FIRST 1 ROWS ONLY) NOMBRE_NIT, \r\n" +
                                        "(SELECT PNA.LAST_NAME FROM SYSADM.PS_NAMES PNA WHERE PNA.NAME_TYPE = 'REC' AND PNA.EMPLID='" + emplid + "' ORDER BY EFFDT DESC FETCH FIRST 1 ROWS ONLY) APELLIDO_NIT, \r\n" +
                                        "(SELECT SECOND_LAST_NAME FROM SYSADM.PS_NAMES PNA WHERE PNA.NAME_TYPE = 'REC' AND PNA.EMPLID='" + emplid + "' ORDER BY PNA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) CASADA_NIT, \r\n" +
                                        "(SELECT ADDRESS1 FROM SYSADM.PS_ADDRESSES PA WHERE PA.ADDRESS_TYPE = 'REC' AND PA.EMPLID='" + emplid + "' ORDER BY PA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) DIRECCION1_NIT, \r\n" +
                                        "(SELECT ADDRESS2 FROM SYSADM.PS_ADDRESSES PA WHERE PA.ADDRESS_TYPE = 'REC' AND PA.EMPLID='" + emplid + "' ORDER BY PA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) DIRECCION2_NIT, \r\n" +
                                        "(SELECT ADDRESS3 FROM SYSADM.PS_ADDRESSES PA WHERE PA.ADDRESS_TYPE = 'REC' AND PA.EMPLID='" + emplid + "' ORDER BY PA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) DIRECCION3_NIT, \r\n" +
                                        "(SELECT C.DESCR FROM SYSADM.PS_ADDRESSES PA JOIN SYSADM.PS_COUNTRY_TBL C ON PA.COUNTRY = C.COUNTRY AND PA.ADDRESS_TYPE = 'REC' AND PA.EMPLID='" + emplid + "' ORDER BY PA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) PAIS_NIT, \r\n" +
                                        "(SELECT REGEXP_SUBSTR(ST.DESCR,'[^-]+') FROM SYSADM.PS_STATE_TBL ST JOIN SYSADM.PS_ADDRESSES PA ON ST.STATE = PA.STATE WHERE PA.ADDRESS_TYPE = 'REC' AND PA.EMPLID='" + emplid + "' ORDER BY PA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) MUNICIPIO_NIT, \r\n" +
                                        "(SELECT REGEXP_SUBSTR(ST.DESCR,'[^-]+') FROM SYSADM.PS_STATE_TBL ST JOIN SYSADM.PS_PERS_DATA_SA_VW PD ON ST.STATE = PD.BIRTHSTATE AND ST.COUNTRY = PD.BIRTHCOUNTRY WHERE PD.EMPLID='" + emplid + "' ) MUNICIPIO_NAC, \r\n" +
                                        "(SELECT SUBSTR(ST.DESCR,(INSTR(ST.DESCR,'-')+1)) FROM SYSADM.PS_STATE_TBL ST JOIN SYSADM.PS_ADDRESSES PA ON ST.STATE = PA.STATE WHERE PA.ADDRESS_TYPE = 'REC' AND PA.EMPLID='" + emplid + "' ORDER BY PA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) DEPARTAMENTO_NIT, \r\n" +
                                        "COALESCE((SELECT SUBSTR(ST.DESCR, (INSTR(ST.DESCR, '-') + 1)) FROM SYSADM.PS_STATE_TBL ST JOIN SYSADM.PS_PERS_DATA_SA_VW PD ON ST.STATE = PD.BIRTHSTATE AND ST.COUNTRY = PD.BIRTHCOUNTRY WHERE PD.EMPLID='" + emplid + "' ),' ') DEPARTAMENTO_NAC, \r\n" +
                                        "(SELECT ST.STATE FROM SYSADM.PS_STATE_TBL ST JOIN SYSADM.PS_ADDRESSES PA ON ST.STATE = PA.STATE WHERE PA.ADDRESS_TYPE = 'REC' AND PA.EMPLID='" + emplid + "' ORDER BY PA.EFFDT DESC FETCH FIRST 1 ROWS ONLY) STATE_NIT, \r\n" +
                                        "(SELECT EMAIL.EMAIL_ADDR FROM SYSADM.PS_EMAIL_ADDRESSES EMAIL WHERE EMAIL.EMPLID = '" + emplid + "' AND UPPER(EMAIL.EMAIL_ADDR) LIKE '%UNIS.EDU.GT%' ORDER BY CASE WHEN EMAIL.PREF_EMAIL_FLAG = 'Y' THEN 1 ELSE 2 END, EMAIL.EMAIL_ADDR FETCH FIRST 1 ROWS ONLY) EMAILUNIS , \r\n" +
                                        "(SELECT EMAIL.EMAIL_ADDR FROM SYSADM.PS_EMAIL_ADDRESSES EMAIL WHERE EMAIL.EMPLID = '" + emplid + "' AND EMAIL.E_ADDR_TYPE IN ('HOM1') FETCH FIRST 1 ROWS ONLY) EMAILPERSONAL , \r\n" +
                                        "A.ADDRESS1 DIRECCION, A.ADDRESS2 DIRECCION2, A.ADDRESS3 DIRECCION3, \r\n" +
                                        "NVL(REGEXP_SUBSTR(ST.DESCR, '[^-]+'), ' ') AS MUNICIPIO , NVL(SUBSTR(ST.DESCR, (INSTR(ST.DESCR, '-') + 1)), ' ') DEPARTAMENTO, ST.STATE,  \r\n" +
                                        "TT.TERM_BEGIN_DT, C.DESCR PAIS \r\n" +
                                        "FROM SYSADM.PS_PERS_DATA_SA_VW PD \r\n" +
                                        "LEFT JOIN SYSADM.PS_PERS_NID PN ON PD.EMPLID = PN.EMPLID \r\n" +
                                        "LEFT JOIN SYSADM.PS_ADDRESSES A ON PD.EMPLID = A.EMPLID AND ADDRESS_TYPE= 'HOME' \r\n" +
                                        "AND A.EFFDT =( \r\n" +
                                        "    SELECT \r\n" +
                                        "        MAX(EFFDT) \r\n" +
                                        "    FROM \r\n" +
                                        "        SYSADM.PS_ADDRESSES A2 \r\n" +
                                        "    WHERE \r\n" +
                                        "        A.EMPLID = A2.EMPLID \r\n" +
                                        "        AND A.ADDRESS_TYPE = A2.ADDRESS_TYPE \r\n" +
                                        ") \r\n" +
                                        "LEFT JOIN SYSADM.PS_PERSONAL_DATA PPD ON PD.EMPLID = PPD.EMPLID \r\n" +
                                        "LEFT JOIN SYSADM.PS_STATE_TBL ST ON PPD.STATE = ST.STATE \r\n" +
                                        "LEFT JOIN SYSADM.PS_STDNT_CAR_TERM CT ON PD.EMPLID = CT.EMPLID \r\n" +
                                        "LEFT JOIN SYSADM.PS_ACAD_PROG_TBL APD ON CT.acad_prog_primary = APD.ACAD_PROG \r\n" +
                                        "AND CT.ACAD_CAREER = APD.ACAD_CAREER \r\n" +
                                        "AND CT.INSTITUTION = APD.INSTITUTION \r\n" +
                                        "LEFT JOIN SYSADM.PS_ACAD_GROUP_TBL AGT ON APD.ACAD_GROUP = AGT.ACAD_GROUP \r\n" +
                                        "AND APD.INSTITUTION = AGT.INSTITUTION \r\n" +
                                        "LEFT JOIN SYSADM.PS_TERM_TBL TT ON CT.STRM = TT.STRM \r\n" +
                                        "AND CT.INSTITUTION = TT.INSTITUTION \r\n" +
                                        "AND (SYSDATE BETWEEN TT.TERM_BEGIN_DT AND TT.TERM_END_DT) \r\n" +
                                        //"LEFT JOIN SYSADM.PS_PERSONAL_PHONE PP ON PD.EMPLID = PP.EMPLID \r\n" +
                                        //"AND PP.PHONE_TYPE = 'HOME' \r\n" +
                                        "LEFT JOIN SYSADM.PS_COUNTRY_TBL C ON A.COUNTRY = C.COUNTRY \r\n" +
                                        "WHERE PN.NATIONAL_ID ='" + DPI + "' \r\n" +
                                        "ORDER BY CT.FULLY_ENRL_DT DESC \r\n" +
                                        "FETCH FIRST 1 ROWS ONLY \r\n" +
                                       ") ";
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        modelo.IdCampus = reader["EMPLID"].ToString();
                        modelo.Nombres = reader["FIRST_NAME"].ToString().TrimEnd();
                        modelo.Apellidos = reader["LAST_NAME"].ToString().TrimEnd();
                        modelo.ApellidoCasada = reader["SECOND_LAST_NAME"].ToString().TrimEnd();
                        if (modelo.ApellidoCasada == null || modelo.ApellidoCasada == "")
                        {
                            ViewData["EstadoCivil"] = null;
                        }
                        else
                        {
                           

                            ViewData["EstadoCivil"] = "C";
                        }
                        modelo.correo = reader["EMAILUNIS"].ToString();
                        modelo.CUI = reader["DPI"].ToString();

                    }

                    con.Close();
                }
            }
            //return emplid;
            return View(modelo);

        }
    }
}