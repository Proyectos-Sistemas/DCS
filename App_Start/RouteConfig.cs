using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Homer_MVC
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
            //defaults: new { controller = "Account", action = "SignIn", id = UrlParameter.Optional }

            defaults: new { controller = "Service", action = "ServiceRequest", id = UrlParameter.Optional }
            );
            //routes.MapRoute(
            //    name: "GetForm",
            //    url: "ServiceE/GetForm/{department}/{query}/{user_rol}",
            //    defaults: new { controller = "ServiceE", action = "GetForm", department = UrlParameter.Optional, query = UrlParameter.Optional, user_rol = UrlParameter.Optional }
            //);

        }
    }
}