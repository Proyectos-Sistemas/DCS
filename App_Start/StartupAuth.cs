using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

namespace DCS_UNIS
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string aadInstance = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        // Sin RedirectUri, Azure AD usa la primera Reply URL registrada (localhost)
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];

        string authority = aadInstance + tenantId;

        public void Configuration(IAppBuilder app)
        {
            IdentityModelEventSource.ShowPII = true;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            // SystemWebCookieManager: sin el, OWIN y System.Web pisan sus cookies en la misma respuesta y se
            // pierden la cookie de sesion (parametros del link guardados en Session) o la del nonce.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieManager = new SystemWebCookieManager()
            });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = authority,
                    PostLogoutRedirectUri = postLogoutRedirectUri,
                    RedirectUri = redirectUri,
                    CookieManager = new SystemWebCookieManager(),

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        AuthenticationFailed = (context) =>
                        {
                            // Sin cookie del nonce (login de Microsoft abierto mucho tiempo, o el navegador no la
                            // reenvio): se reinicia el login hacia la URL original, que trae los parametros del link.
                            if (context.Exception is OpenIdConnectProtocolInvalidNonceException)
                            {
                                context.HandleResponse();
                                context.Response.Redirect(UrlOriginal(context.ProtocolMessage?.State, context.Options, context.Request));
                            }
                            return System.Threading.Tasks.Task.FromResult(0);
                        }
                    }

                }
                );

            // This makes any middleware defined above this line run before the Authorization rule is applied in web.config
            app.UseStageMarker(PipelineStage.Authenticate);

            //app.UseCookieAuthentication(new CookieAuthenticationOptions
            //{
            //    AuthenticationType = "ApplicationCookie",
            //    LoginPath = new PathString("/Account/Login")
            //});
        }

        // El state del login trae la URL que pidio el usuario. Solo se acepta si es de este mismo sitio.
        private static string UrlOriginal(string state, OpenIdConnectAuthenticationOptions options, IOwinRequest request)
        {
            string raiz = request.PathBase + "/";
            const string prefijo = "OpenIdConnect.AuthenticationProperties=";
            if (string.IsNullOrEmpty(state) || !state.StartsWith(prefijo, StringComparison.Ordinal))
                return raiz;
            try
            {
                var props = options.StateDataFormat.Unprotect(Uri.UnescapeDataString(state.Substring(prefijo.Length)));
                string url = props?.RedirectUri;
                string sitio = request.Scheme + "://" + request.Host + request.PathBase;
                if (string.IsNullOrEmpty(url))
                    return raiz;
                if (url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal))
                    return url;
                if (url.StartsWith(sitio + "/", StringComparison.OrdinalIgnoreCase))
                    return url;
            }
            catch (Exception)
            {
                // state invalido o de otra instancia: se va a la raiz
            }
            return raiz;
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
