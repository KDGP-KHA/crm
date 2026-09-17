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
        public ActionResult Index(int? applyYear, string keyword = null)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            Core.Cate.Models.DigitalSalesDashboardOverviewModel overviewModel;
            try
            {
                overviewModel = _digitalSalesCache.GetDashboardStatusStats(year, User?.UserName, keyword);
            }
            catch (Exception ex)
            {
                TSFramework.Libs.Processors.AppProcessor.Logger.Error(ex);
                overviewModel = new Core.Cate.Models.DigitalSalesDashboardOverviewModel { ApplyYear = year, Keyword = keyword };
            }
            ViewBag.ApplyYear = year;
            ViewBag.Keyword = keyword;
            ViewBag.Title = "Dashboard";
            return View("~/Areas/Dashboard/Views/Dashboard/Chart.cshtml", overviewModel ?? new Core.Cate.Models.DigitalSalesDashboardOverviewModel { ApplyYear = year, Keyword = keyword });
        }

        [HttpGet]
        public ActionResult Chart(int? applyYear, string keyword = null)
        {
            return Index(applyYear, keyword);
        }

        [HttpGet]
        public ActionResult GetChartOverviewData(int? applyYear, string keyword = null)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            Core.Cate.Models.DigitalSalesDashboardOverviewModel overviewModel;
            try
            {
                overviewModel = _digitalSalesCache.GetDashboardStatusStats(year, User?.UserName, keyword);
            }
            catch (Exception ex)
            {
                TSFramework.Libs.Processors.AppProcessor.Logger.Error(ex);
                overviewModel = new Core.Cate.Models.DigitalSalesDashboardOverviewModel { ApplyYear = year, Keyword = keyword };
            }
            return PartialView("~/Areas/Dashboard/Views/Dashboard/_ChartOverview.cshtml", overviewModel ?? new Core.Cate.Models.DigitalSalesDashboardOverviewModel { ApplyYear = year, Keyword = keyword });
        }

        [HttpGet]
        public ActionResult Export(int? applyYear, string keyword = null)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            string kw = string.IsNullOrWhiteSpace(keyword) ? "" : Server.UrlEncode(keyword.Trim());
            return Redirect($"/Cate/DigitalSales/Export?applyYear={year}&keyword={kw}");
        }
    }
}
