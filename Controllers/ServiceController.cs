using GemBox.Email;
using GemBox.Email.Smtp;
using Homer_MVC.Models;
using Oracle.ManagedDataAccess.Client;
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Homer_MVC.Controllers
{
    public class ServiceController : Controller
    {
        //private DCS_UNISEntities db = new DCS_UNISEntities();
        private OracleDbContext dbContext = new OracleDbContext(); // Contexto de la base de datos

        string CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;



        // GET: Service
        [Authorize]
        public ActionResult ServiceRequest(int? IsSuccess = 0, string d = "", string q = "")
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

                if (string.IsNullOrEmpty(d))
                {
                    d = Session["department"].ToString();
                }

                if (string.IsNullOrEmpty(q))
                {
                    q = Session["query"].ToString();

                }
            }
            catch (Exception ex) { 
            
            }

            return MostrarInformacion(emplid,d,q);

            //return MostrarInformacion("00000021543");
            //return MostrarInformacion("00000023478");


        }

        private void CargarListas(MailEAModel m)
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
        public ActionResult ServiceRequest([Bind(Include = "Nombre_Departamento,IdCampus,Nombre_Producto,Nombres,Apellidos,ApellidoCasada,CUI,correo,problema,ArchivoAdjunto")] MailEAModel mail)
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
                List<ProductoModel> Subject;
                string sub = "", sub_completo = "", cola = "";
                try
                {
                    Subject = (from x in dbContext.Producto select x).ToList();
                    var registro = Subject.Where(p => p.ID_PRODUCTO == Int32.Parse(mail.Nombre_Producto)).FirstOrDefault();
                    sub = registro.NOMBRE_PRODUCTO;
                    cola = cola = (from x in dbContext.Departamento
                                   where x.ID_DEPARTAMENTO == registro.ID_CATEGORIA
                                   select x.CORREO_NOTIFICACION).FirstOrDefault();//se obtiene la cola del producto;
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.InnerException);
                }
                string enlaceServidor = Request.Url.GetLeftPart(UriPartial.Authority) + Url.Content("~/ArchivosAdjuntos/" + storedName);
                string body = "Nombres: " + mail.Nombres + "\n"
                    + "Apellidos: " + mail.Apellidos + "\n"
                    + (estadoCivil != null ? "Apellido de Casada: " + mail.ApellidoCasada : "") + "\n"
                    + "CUI: " + mail.CUI + "\n"
                    + "Id Campus: " + mail.IdCampus + "\n"
                    + "Correo: " + mail.correo + "\n"
                    + "Producto: " + sub + "\n\n"
                    + "Solicitud: " + mail.problema + "\n\n"
                     + "Archivo Adjunto: " + enlaceServidor;

                sub_completo = "Solicitud: " + sub;

                SendMail(cola, mail.correo, sub_completo, body); //método para enviar el correo
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
        public ActionResult MostrarInformacion(string emplid, string d, string q)
        {
            var modelo = new MailEAModel();

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
                        if (!string.IsNullOrEmpty(d))
                        {
                            modelo.Nombre_Departamento = d; // este debe coincidir con el campo <select> del form
                        }

                        if (!string.IsNullOrEmpty(q))
                        {
                            modelo.problema = q; // este debe coincidir con el textarea/input de la descripción
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