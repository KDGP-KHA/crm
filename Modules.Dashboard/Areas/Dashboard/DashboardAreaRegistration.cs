using System.Web.Mvc;

namespace Modules.Dashboard.Areas.Dashboard
{
    public class DashboardAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Dashboard";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "Dashboard_Chart",
                url: "Dashboard/Chart",
                defaults: new { controller = "Dashboard", action = "Chart" },
                namespaces: new[] { "Modules.Dashboard.Areas.Dashboard.Controllers" }
            );

            context.MapRoute(
                name: "Dashboard_Chart_Action",
                url: "Dashboard/Chart/{action}/{id}",
                defaults: new { controller = "Dashboard", action = "Chart", id = UrlParameter.Optional },
                namespaces: new[] { "Modules.Dashboard.Areas.Dashboard.Controllers" }
            );

            context.MapRoute(
                name: "Dashboard_default",
                url: "Dashboard/{controller}/{action}/{id}",
                defaults: new { controller = "Manager", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Modules.Dashboard.Areas.Dashboard.Controllers" }
            );
        }
    }
}