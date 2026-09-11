using Homer_MVC.Models;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Homer_MVC.Controllers
{
    public class AccountController : Controller

    {
        private string _strDirectorio = "LDAP://unis.local/dc=unis,dc=local";

        private string _strFiltro;
        public AccountController()
        {
        }
     

        public void Login()
        {
            var _d = Session["department"];
            var _q = Session["query"];



            //return View();
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/Service/ServiceRequest" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
            else
            {
                if (_d != null && _q != null)
                {
                    RedirectToAction("ServiceRequest", "Service", new { IsSuccess = 0, d = _d, q = _q});

                }
                else
                {
                    Response.Redirect("/Service/ServiceRequest");

                }

                   
            }
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            int error;
            string mensaje;
            string dominio = "unis"; // o el que corresponda
            
            //Conexion con = new Conexion(ldapPath);

            bool resultado = AutenticarUsuario(dominio, model.Usuario, model.Contrasena, out error, out mensaje);

            if (resultado)
            {
                Session["usuario"] = model.Usuario;
                return RedirectToAction("ServiceRequest", "Service");
            }
            else
            {
                ViewBag.Error = mensaje;
                return View(model);
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/Service/ServiceRequest" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
            else
            {
                Response.Redirect("/Service/ServiceRequest");
            }
        }


        public void SignOut()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType,
                CookieAuthenticationDefaults.AuthenticationType);
        }

        public ActionResult Error(string message)
        {
            ViewBag.ErrorMessage = message;
            return View();
        }
        public bool AutenticarUsuario(string strDominio, string strUsuario, string strPass, out int intError, out string strmensaje)
        {
            //IL_001e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0024: Expected O, but got Unknown
            //IL_002d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0033: Expected O, but got Unknown
            strmensaje = "";
            string text = strDominio + "\\" + strUsuario;
            DirectoryEntry val = new DirectoryEntry(_strDirectorio, text, strPass);
            try
            {
                object nativeObject = val.NativeObject;
                DirectorySearcher val2 = new DirectorySearcher(val);
                val2.Filter = "(SAMAccountName=" + strUsuario + ")";
                val2.PropertiesToLoad.Add("cn");
                SearchResult val3 = val2.FindOne();
                if (val3 == null)
                {
                    intError = 1;
                    strmensaje = "NO SE ENCONTRÓ EL USUARIO";
                    return false;
                }

                _strDirectorio = val3.Path;
                _strFiltro = (string)val3.Properties["cn"][0];
            }
            catch (Exception ex)
            {
                intError = 1;
                strmensaje = "ERRROR: " + ex.Message;
                return false;
            }

            intError = 0;
            strmensaje = "CORRECTO";
            return true;
        }

        public bool CambiarClave(string _strDominio, string _strusuario, string _strClaveAnterior, string _strNuevaClave, out int intError, out string strmensaje)
        {
            //IL_0015: Unknown result type (might be due to invalid IL or missing references)
            //IL_001b: Expected O, but got Unknown
            //IL_0028: Unknown result type (might be due to invalid IL or missing references)
            //IL_002e: Expected O, but got Unknown
            try
            {
                DirectoryEntry val = new DirectoryEntry(_strDirectorio, _strDominio + "\\" + _strusuario, _strClaveAnterior);
                if (val != null)
                {
                    DirectorySearcher val2 = new DirectorySearcher(val);
                    val2.Filter = "(SAMAccountName=" + _strusuario + ")";
                    SearchResult val3 = val2.FindOne();
                    if (val3 != null)
                    {
                        DirectoryEntry directoryEntry = val3.GetDirectoryEntry();
                        if (directoryEntry != null)
                        {
                            directoryEntry.Invoke("ChangePassword", new object[2] { _strClaveAnterior, _strNuevaClave });
                            directoryEntry.CommitChanges();
                            directoryEntry.Close();
                        }

                        directoryEntry.Close();
                    }

                    val.Close();
                }
            }
            catch (Exception)
            {
                intError = 1;
                strmensaje = "ERROR";
                return false;
            }

            intError = 0;
            strmensaje = "CORRECTO";
            return true;
        }

        public bool RestablecerClave(string strUsrPer, string strClvPer, string strUsuario, string strNuevaClave, out int intError, out string strmensaje)
        {
            //IL_000b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Expected O, but got Unknown
            //IL_001e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0024: Expected O, but got Unknown
            try
            {
                DirectoryEntry val = new DirectoryEntry(_strDirectorio, strUsrPer, strClvPer, (AuthenticationTypes)1);
                if (val != null)
                {
                    DirectorySearcher val2 = new DirectorySearcher(val);
                    val2.SearchRoot = val;
                    val2.Filter = "(SAMAccountName=" + strUsuario + ")";
                    SearchResult val3 = val2.FindOne();
                    if (val3 != null)
                    {
                        DirectoryEntry directoryEntry = val3.GetDirectoryEntry();
                        if (directoryEntry != null)
                        {
                            directoryEntry.Invoke("SetPassword", new object[1] { strNuevaClave });
                            directoryEntry.Properties["LockOutTime"].Value = 0;
                            directoryEntry.Close();
                        }

                        directoryEntry.Close();
                    }

                    val.Close();
                }
            }
            catch (Exception)
            {
                intError = 1;
                strmensaje = "ERROR";
                return false;
            }

            intError = 0;
            strmensaje = "CORRECTO";
            return true;
        }

        public bool DesbloquearUsuario(string strUsuario)
        {
            //IL_0013: Unknown result type (might be due to invalid IL or missing references)
            //IL_0019: Expected O, but got Unknown
            //IL_0023: Unknown result type (might be due to invalid IL or missing references)
            //IL_0029: Expected O, but got Unknown
            try
            {
                DirectoryEntry val = new DirectoryEntry(_strDirectorio, "reset", "Abril2016", (AuthenticationTypes)1);
                if (val != null)
                {
                    DirectorySearcher val2 = new DirectorySearcher(val);
                    val2.SearchRoot = val;
                    val2.Filter = "(SAMAccountName=" + strUsuario + ")";
                    SearchResult val3 = val2.FindOne();
                    if (val3 != null)
                    {
                        DirectoryEntry directoryEntry = val3.GetDirectoryEntry();
                        if (directoryEntry != null)
                        {
                            string text = directoryEntry.Properties["userAccountControl"].Value.ToString();
                        }

                        directoryEntry.Close();
                    }

                    val.Close();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public List<string> BuscarGrupos(string strUsrPer, string strClvPer, string strUsuario, out int intError)
        {
            //IL_0011: Unknown result type (might be due to invalid IL or missing references)
            //IL_0017: Expected O, but got Unknown
            //IL_0024: Unknown result type (might be due to invalid IL or missing references)
            //IL_002a: Expected O, but got Unknown
            //IL_00a6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ad: Expected O, but got Unknown
            List<string> list = new List<string>();
            try
            {
                DirectoryEntry val = new DirectoryEntry(_strDirectorio, strUsrPer, strClvPer, (AuthenticationTypes)1);
                if (val != null)
                {
                    DirectorySearcher val2 = new DirectorySearcher(val);
                    val2.SearchRoot = val;
                    val2.Filter = "(SAMAccountName=" + strUsuario + ")";
                    SearchResult val3 = val2.FindOne();
                    if (val3 != null)
                    {
                        DirectoryEntry directoryEntry = val3.GetDirectoryEntry();
                        if (directoryEntry != null)
                        {
                            object obj = directoryEntry.Invoke("Groups", new object[0]);
                            foreach (object item in (IEnumerable)obj)
                            {
                                DirectoryEntry val4 = new DirectoryEntry(item);
                                list.Add(val4.Name.ToString().Replace("CN=", ""));
                            }
                        }

                        directoryEntry.Close();
                    }

                    val.Close();
                }
            }
            catch (Exception)
            {
                intError = 1;
                return list;
            }

            intError = 0;
            return list;
        }
    }
}
