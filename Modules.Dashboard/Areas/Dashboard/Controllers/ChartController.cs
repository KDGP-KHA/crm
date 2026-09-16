using Core.Cate.Caches;
using Core.Sys.BaseApp;
using System;
using System.Web.Mvc;

namespace Modules.Dashboard.Areas.Dashboard.Controllers
{
    /// <summary>
    /// Controller phục vụ trực tiếp cho route Dashboard/Chart
    /// </summary>
    public class ChartController : AppController
    {
        private readonly RM_DigitalSalesCache _digitalSalesCache = new RM_DigitalSalesCache();

        [HttpGet]
        public ActionResult Index(int? applyYear)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            var overviewModel = _digitalSalesCache.GetDashboardStatusStats(year, User?.UserName);
            ViewBag.ApplyYear = year;
            ViewBag.Title = "Dashboard";
            return View("~/Areas/Dashboard/Views/Dashboard/Chart.cshtml", overviewModel);
        }

        [HttpGet]
        public ActionResult Chart(int? applyYear)
        {
            return Index(applyYear);
        }

        [HttpGet]
        public ActionResult GetChartOverviewData(int? applyYear)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            var overviewModel = _digitalSalesCache.GetDashboardStatusStats(year, User?.UserName);
            return PartialView("~/Areas/Dashboard/Views/Dashboard/_ChartOverview.cshtml", overviewModel);
        }
    }
}
