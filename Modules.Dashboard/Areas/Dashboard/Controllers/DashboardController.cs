using Core.Cate.Biz;
using Core.Cate.Models;
using Core.Cate.Caches;
using Core.Sys.BaseApp;
using Core.Sys.Permissions;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using Core.Sys.Caches.Sys;
using TSFramework.Libs.Processors;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Modules.Dashboard.Areas.Dashboard.Controllers
{
    /// <summary>
    /// Xử lý các luồng tra cứu và lọc dữ liệu dashboard cơ hội kinh doanh, dự án và kế hoạch.
    /// </summary>
    public class DashboardController : AppController
    {
        private readonly RM_OpportunityDashboardCache _biz;
        private readonly RM_DashboardCache _dashboardCache;
        private readonly SysUserCache _userCache;
        private readonly RM_DigitalSalesCache _digitalSalesCache;

        private const string PERMISSION_CONTEXT_ITEM_KEY = "Dashboard.AppPermissionContext";
        private const string EXCEL_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        /// <summary>
        /// Khởi tạo cache phục vụ xử lý dashboard.
        /// </summary>
        public DashboardController()
        {
            _biz = new RM_OpportunityDashboardCache();
            _dashboardCache = new RM_DashboardCache();
            _userCache = new SysUserCache();
            _digitalSalesCache = new RM_DigitalSalesCache();
        }

        /// <summary>
        /// Lấy ngữ cảnh phân quyền của người dùng hiện tại, chỉ truy vấn 1 lần cho mỗi request.
        /// </summary>
        /// <returns>Ngữ cảnh phân quyền của request hiện tại.</returns>
        private AppPermissionContext GetPermissionContext()
        {
            var httpContext = System.Web.HttpContext.Current;
            var permission = httpContext?.Items[PERMISSION_CONTEXT_ITEM_KEY] as AppPermissionContext;
            if (permission != null) return permission;

            permission = AppPermissionContext.FromPrincipal(User);
            if (httpContext != null)
            {
                httpContext.Items[PERMISSION_CONTEXT_ITEM_KEY] = permission;
            }

            return permission;
        }

        /// <summary>
        /// Khởi tạo điều kiện tìm kiếm dashboard với khoảng ngày và phạm vi nhân sự theo quyền hiện tại.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu cần áp dụng cho dashboard.</param>
        /// <param name="toDate">Ngày kết thúc cần áp dụng cho dashboard.</param>
        /// <returns>Model điều kiện tìm kiếm dashboard.</returns>
        private DashboardSearchModel BuildSearch(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var permission = GetPermissionContext();
            return new DashboardSearchModel
            {
                FromDate = fromDate ?? new DateTime(DateTime.Now.Year, 1, 1),
                ToDate = toDate ?? DateTime.Now,
                EmpFromDate = fromDate ?? new DateTime(DateTime.Now.Year, 1, 1),
                EmpToDate = toDate ?? DateTime.Now,
                EmployeeIds = permission.GetAllowedEmployeeIdsString()
            };
        }

        /// <summary>
        /// Tính ngày đầu tuần theo thứ Hai từ một ngày bất kỳ.
        /// </summary>
        /// <param name="date">Ngày cần tính tuần làm việc.</param>
        /// <returns>Ngày thứ Hai của tuần chứa ngày đầu vào.</returns>
        private DateTime GetMondayOfWeek(DateTime date)
        {
            var diff = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-diff);
        }

        /// <summary>
        /// Parse ngày bắt đầu từ chuỗi đầu vào, nếu không hợp lệ thì lấy ngày đầu năm hiện tại.
        /// </summary>
        /// <param name="fromDate">Chuỗi ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <returns>Ngày bắt đầu hợp lệ để dùng cho tìm kiếm.</returns>
        private DateTime ParseFromDateOrDefault(string fromDate)
        {
            DateTime parsedDate;
            if (!DateTime.TryParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                parsedDate = new DateTime(DateTime.Now.Year, 1, 1);
            }

            return parsedDate;
        }

        /// <summary>
        /// Parse ngày kết thúc từ chuỗi đầu vào, nếu không hợp lệ thì lấy ngày hiện tại.
        /// </summary>
        /// <param name="toDate">Chuỗi ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Ngày kết thúc hợp lệ để dùng cho tìm kiếm.</returns>
        private DateTime ParseToDateOrDefault(string toDate)
        {
            DateTime parsedDate;
            if (!DateTime.TryParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                parsedDate = DateTime.Now;
            }

            return parsedDate;
        }

        /// <summary>
        /// Chuyển hướng Dashboard sang trang Dashboard Chart mới theo yêu cầu.
        /// </summary>
        [HttpGet]
        public ActionResult Index()
        {
            return Redirect("/Dashboard/Chart");
        }

        /// <summary>
        /// Trang Dashboard mới gồm 2 tab (Tổng quát & Kế hoạch kinh doanh) và bộ lọc theo Năm áp dụng.
        /// </summary>
        [HttpGet]
        public ActionResult Chart(int? applyYear)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            var overviewModel = _digitalSalesCache.GetDashboardStatusStats(year);
            ViewBag.ApplyYear = year;
            ViewBag.Title = "Dashboard";
            return View("Chart", overviewModel);
        }

        /// <summary>
        /// Tải lại partial view tab Tổng quát khi người dùng đổi Năm áp dụng.
        /// </summary>
        [HttpGet]
        public ActionResult GetChartOverviewData(int? applyYear)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            var overviewModel = _digitalSalesCache.GetDashboardStatusStats(year);
            return PartialView("_ChartOverview", overviewModel);
        }

        /// <summary>
        /// Hiển thị popup modal danh sách cơ hội/dự án kinh doanh dịch vụ số theo trạng thái và năm áp dụng.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult DigitalSalesByStatus(int? applyYear, int? statusId, string statusName)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            var model = new RM_DigitalSalesSearchModel
            {
                ApplyYear = year,
                StatusID = statusId.GetValueOrDefault(0),
                Keyword = statusName
            };
            ViewBag.ApplyYear = year;
            ViewBag.StatusID = statusId.GetValueOrDefault(0);
            ViewBag.StatusName = string.IsNullOrEmpty(statusName) ? (statusId > 0 ? "Theo trạng thái" : "Tất cả trạng thái") : statusName;
            return PartialView("DigitalSalesByStatus", model);
        }

        /// <summary>
        /// Trả dữ liệu JSON danh sách cơ hội/dự án kinh doanh dịch vụ số theo trạng thái và năm áp dụng cho DataTable.
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetDigitalSalesByStatus(int? applyYear, int? statusId)
        {
            int year = applyYear.HasValue && applyYear.Value > 0 ? applyYear.Value : DateTime.Now.Year;
            var search = new RM_DigitalSalesSearchModel
            {
                ApplyYear = year,
                StatusID = statusId.GetValueOrDefault(0),
                PageNumber = 1,
                PageSize = 1000,
                UserName = User.UserName
            };
            var data = _digitalSalesCache.LoadList(out var total, search);
            return Json(new { recordsTotal = total, recordsFiltered = total, data }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Màn hình dashboard tổng hợp legacy.
        /// </summary>
        public ActionResult IndexLegacy()
        {
            var user = _userCache.GetByUserName(User.UserName);
            var search = BuildSearch();
            var planFromDate = GetMondayOfWeek(DateTime.Today);
            var planToDate = planFromDate.AddDays(6);
            var planSearch = BuildSearch(planFromDate, planToDate);
            var total = 0;
            search.Type = 1;

            var model = new RM_DashboardModel
            {
                FromDate = search.FromDate,
                ToDate = search.ToDate,
                EmpFromDate = search.EmpFromDate,
                EmpToDate = search.EmpToDate,
                PlanFromDate = planFromDate,
                PlanToDate = planToDate
            };

            // Dùng lớp cache (key theo from/to/employeeIds) — dữ liệu ghi sẽ tự invalidate qua DashboardCache.ClearCache()
            model.OpportunityDashboardModel = _biz.GetViewModel(search);
            model.OverviewDashboardModel = _dashboardCache.GetOverview(search);
            model.OverviewDashboardModel.StaleUpdates = _dashboardCache.GetStaleUpdates(search.EmployeeIds);
            model.OverviewDashboardModel.Projects = _dashboardCache.GetProjects(out total, search);
            model.OverviewDashboardModel.Opportunitys = _dashboardCache.GetOpportunities(out total, search);
            model.OverviewDashboardModel.Plans = GetDashboardPlans(planSearch, User.Identity.Name);
            ViewBag.PlanFromDate = planFromDate;
            ViewBag.PlanToDate = planToDate;
            ViewBag.ShowRegulation = user.RegulationViewedDate == null;

            return View(model);
        }

        /// <summary>
        /// Tải lại dữ liệu dashboard cơ hội kinh doanh theo khoảng ngày lọc.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Partial view dữ liệu dashboard theo nhân sự.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetData(string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var search = BuildSearch(dtFrom, dtTo);
            var model = _biz.GetViewModel(search);

            return PartialView("_Employee", model);
        }

        /// <summary>
        /// Tải lại dữ liệu tổng quan dashboard theo khoảng ngày lọc.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Partial view tổng quan dashboard.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult OverviewFilter(string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var search = BuildSearch(dtFrom, dtTo);
            search.Type = 1;
            var total = 0;

            var model = _dashboardCache.GetOverview(search);
            model.StaleUpdates = _dashboardCache.GetStaleUpdates(search.EmployeeIds);
            model.Projects = _dashboardCache.GetProjects(out total, search);
            model.Opportunitys = _dashboardCache.GetOpportunities(out total, search);

            return PartialView("_Overview", model);
        }

        /// <summary>
        /// Xuất danh sách cơ hội và dự án chậm cập nhật trong phạm vi nhân sự người dùng được phép xem.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportStaleUpdates()
        {
            var employeeIds = GetPermissionContext().GetAllowedEmployeeIdsString();
            var items = (_dashboardCache.GetStaleUpdates(employeeIds) ?? new List<RM_StaleUpdateModel>())
                .OrderByDescending(x => x.DaysWithoutUpdate)
                .ThenBy(x => x.LastUpdateDate)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("StaleUpdates");
                BuildStaleUpdatesWorksheet(worksheet, items);

                var fileName = $"Danh_sach_chua_cap_nhat_{DateTime.Now:yyyyMMddHHmm}.xlsx";
                return File(package.GetAsByteArray(), EXCEL_CONTENT_TYPE, fileName);
            }
        }

        /// <summary>
        /// Xuất toàn bộ danh sách cơ hội kinh doanh trên Dashboard theo bộ lọc ngày và quyền người dùng hiện tại.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportOpportunities(string fromDate, string toDate)
        {
            var search = BuildSearch(ParseFromDateOrDefault(fromDate), ParseToDateOrDefault(toDate));
            search.Type = 1;
            var items = (_dashboardCache.GetOpportunities(out var total, search) ?? new List<OpportunityDashboardModel>())
                .OrderBy(x => x.OpportunityName)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("CoHoiKinhDoanh");
                BuildOpportunitiesWorksheet(worksheet, items, search.FromDate, search.ToDate);

                var fileName = $"Danh_sach_co_hoi_{DateTime.Now:yyyyMMddHHmm}.xlsx";
                return File(package.GetAsByteArray(), EXCEL_CONTENT_TYPE, fileName);
            }
        }

        /// <summary>
        /// Xuất toàn bộ danh sách dự án trên Dashboard theo bộ lọc ngày và quyền người dùng hiện tại.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportProjects(string fromDate, string toDate)
        {
            var search = BuildSearch(ParseFromDateOrDefault(fromDate), ParseToDateOrDefault(toDate));
            search.Type = 1;
            var items = (_dashboardCache.GetProjects(out var total, search) ?? new List<ProjectDashboardModel>())
                .OrderBy(x => x.ProjectName)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DuAn");
                BuildProjectsWorksheet(worksheet, items, search.FromDate, search.ToDate);

                var fileName = $"Danh_sach_du_an_{DateTime.Now:yyyyMMddHHmm}.xlsx";
                return File(package.GetAsByteArray(), EXCEL_CONTENT_TYPE, fileName);
            }
        }

        private void BuildOpportunitiesWorksheet(
            ExcelWorksheet worksheet,
            IList<OpportunityDashboardModel> items,
            DateTime fromDate,
            DateTime toDate)
        {
            var headers = new[]
            {
                "STT", "Mã cơ hội", "Tên cơ hội", "Khách hàng", "Trạng thái", "Dịch vụ",
                "Giá trị dự kiến (triệu)", "Xác suất chốt (%)", "Ngày dự kiến", "Thành viên",
                "Mô tả", "Cập nhật gần nhất", "Người cập nhật", "Nội dung cập nhật",
                "Người liên hệ", "Đơn vị quản lý"
            };

            BuildWorksheetHeading(worksheet, "DANH SÁCH CƠ HỘI KINH DOANH", fromDate, toDate, headers);

            var row = 5;
            foreach (var item in items)
            {
                worksheet.Cells[row, 1].Value = row - 4;
                worksheet.Cells[row, 2].Value = item.CodeOpportunity;
                worksheet.Cells[row, 3].Value = item.OpportunityName;
                worksheet.Cells[row, 4].Value = item.CustomerName;
                worksheet.Cells[row, 5].Value = item.StatusName;
                worksheet.Cells[row, 6].Value = (item.ProductServices ?? string.Empty).Replace(";", Environment.NewLine);
                worksheet.Cells[row, 7].Value = item.ExpectedValue;
                worksheet.Cells[row, 8].Value = item.ClosingProbability;
                worksheet.Cells[row, 9].Value = item.ExpectedDate;
                worksheet.Cells[row, 9].Style.Numberformat.Format = "dd/MM/yyyy";
                worksheet.Cells[row, 10].Value = (item.Members ?? string.Empty).Replace(";", Environment.NewLine);
                worksheet.Cells[row, 11].Value = HtmlToPlainText(item.Description);
                worksheet.Cells[row, 12].Value = item.ExchangeDate;
                worksheet.Cells[row, 12].Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
                worksheet.Cells[row, 13].Value = item.FullName;
                worksheet.Cells[row, 14].Value = HtmlToPlainText(item.ExchangeContent);
                worksheet.Cells[row, 15].Value = string.Join(" - ", new[] { item.ContactPersonName, item.ContactPersonPosition }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
                worksheet.Cells[row, 16].Value = item.TenBoPhan;
                row++;
            }

            FormatWorksheetTable(worksheet, Math.Max(4, row - 1), headers.Length);
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 18;
            worksheet.Column(3).Width = 40;
            worksheet.Column(4).Width = 30;
            worksheet.Column(5).Width = 22;
            worksheet.Column(6).Width = 35;
            worksheet.Column(7).Width = 24;
            worksheet.Column(8).Width = 20;
            worksheet.Column(9).Width = 16;
            worksheet.Column(10).Width = 38;
            worksheet.Column(11).Width = 45;
            worksheet.Column(12).Width = 20;
            worksheet.Column(13).Width = 25;
            worksheet.Column(14).Width = 45;
            worksheet.Column(15).Width = 30;
            worksheet.Column(16).Width = 30;
        }

        private void BuildProjectsWorksheet(
            ExcelWorksheet worksheet,
            IList<ProjectDashboardModel> items,
            DateTime fromDate,
            DateTime toDate)
        {
            var headers = new[]
            {
                "STT", "Tên dự án", "Khách hàng", "Trạng thái", "Ngày bắt đầu",
                "Tổng chi phí", "Tổng doanh thu", "Thành viên", "Đơn vị quản lý"
            };

            BuildWorksheetHeading(worksheet, "DANH SÁCH DỰ ÁN", fromDate, toDate, headers);

            var row = 5;
            foreach (var item in items)
            {
                worksheet.Cells[row, 1].Value = row - 4;
                worksheet.Cells[row, 2].Value = item.ProjectName;
                worksheet.Cells[row, 3].Value = item.CustomerName;
                worksheet.Cells[row, 4].Value = item.StatusName;
                worksheet.Cells[row, 5].Value = item.StartDate;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "dd/MM/yyyy";
                worksheet.Cells[row, 6].Value = item.TotalCost;
                worksheet.Cells[row, 7].Value = item.TotalRevenue;
                worksheet.Cells[row, 6, row, 7].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 8].Value = (item.Members ?? string.Empty).Replace(";", Environment.NewLine);
                worksheet.Cells[row, 9].Value = item.TenBoPhan;
                row++;
            }

            FormatWorksheetTable(worksheet, Math.Max(4, row - 1), headers.Length);
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 45;
            worksheet.Column(3).Width = 35;
            worksheet.Column(4).Width = 22;
            worksheet.Column(5).Width = 16;
            worksheet.Column(6).Width = 20;
            worksheet.Column(7).Width = 20;
            worksheet.Column(8).Width = 40;
            worksheet.Column(9).Width = 30;
        }

        private void BuildWorksheetHeading(
            ExcelWorksheet worksheet,
            string title,
            DateTime fromDate,
            DateTime toDate,
            IList<string> headers)
        {
            worksheet.Cells[1, 1].Value = title;
            worksheet.Cells[1, 1, 1, headers.Count].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells[2, 1].Value = $"Từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy} - Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cells[2, 1, 2, headers.Count].Merge = true;
            worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            for (var column = 1; column <= headers.Count; column++)
            {
                worksheet.Cells[4, column].Value = headers[column - 1];
            }
        }

        private void FormatWorksheetTable(ExcelWorksheet worksheet, int lastRow, int columnCount)
        {
            using (var header = worksheet.Cells[4, 1, 4, columnCount])
            {
                header.Style.Font.Bold = true;
                header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            using (var dataRange = worksheet.Cells[4, 1, lastRow, columnCount])
            {
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                dataRange.Style.WrapText = true;
            }

            worksheet.Cells[4, 1, lastRow, columnCount].AutoFilter = true;
            worksheet.View.FreezePanes(5, 1);
        }

        private string HtmlToPlainText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var decoded = System.Web.HttpUtility.HtmlDecode(value);
            return System.Web.HttpUtility.HtmlDecode(Regex.Replace(decoded, "<.*?>", string.Empty)).Trim();
        }

        /// <summary>
        /// Tạo nội dung Excel từ đúng danh sách StaleUpdate đang hiển thị trên Dashboard.
        /// </summary>
        private void BuildStaleUpdatesWorksheet(ExcelWorksheet worksheet, IList<RM_StaleUpdateModel> items)
        {
            const int columnCount = 10;

            worksheet.Cells[1, 1].Value = "DANH SÁCH CƠ HỘI VÀ DỰ ÁN CHƯA CẬP NHẬT";
            worksheet.Cells[1, 1, 1, columnCount].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells[2, 1].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cells[2, 1, 2, columnCount].Merge = true;
            worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var headers = new[]
            {
                "STT", "Loại", "Mã", "Tên cơ hội / dự án", "Khách hàng",
                "Đơn vị quản lý", "Phụ trách", "Trạng thái", "Cập nhật gần nhất", "Chưa cập nhật (ngày)"
            };

            for (var column = 1; column <= headers.Length; column++)
            {
                worksheet.Cells[4, column].Value = headers[column - 1];
            }

            using (var header = worksheet.Cells[4, 1, 4, columnCount])
            {
                header.Style.Font.Bold = true;
                header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            var row = 5;
            foreach (var item in items)
            {
                worksheet.Cells[row, 1].Value = row - 4;
                worksheet.Cells[row, 2].Value = item.Type == 1 ? "Dự án" : "Cơ hội kinh doanh";
                worksheet.Cells[row, 3].Value = item.ObjectCode;
                worksheet.Cells[row, 4].Value = item.ObjectName;
                worksheet.Cells[row, 5].Value = item.CustomerName;
                worksheet.Cells[row, 6].Value = item.TenBoPhan;
                worksheet.Cells[row, 7].Value = string.IsNullOrWhiteSpace(item.AMNames) ? "Chưa phân công" : item.AMNames;
                worksheet.Cells[row, 8].Value = string.IsNullOrWhiteSpace(item.StatusName) ? "Chưa xác định" : item.StatusName;
                worksheet.Cells[row, 9].Value = item.LastUpdateDate;
                worksheet.Cells[row, 9].Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
                worksheet.Cells[row, 10].Value = item.DaysWithoutUpdate;
                row++;
            }

            var lastRow = Math.Max(4, row - 1);
            using (var dataRange = worksheet.Cells[4, 1, lastRow, columnCount])
            {
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                dataRange.Style.WrapText = true;
            }

            worksheet.Cells[4, 1, lastRow, columnCount].AutoFilter = true;
            worksheet.View.FreezePanes(5, 1);
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 20;
            worksheet.Column(3).Width = 18;
            worksheet.Column(4).Width = 45;
            worksheet.Column(5).Width = 35;
            worksheet.Column(6).Width = 30;
            worksheet.Column(7).Width = 40;
            worksheet.Column(8).Width = 25;
            worksheet.Column(9).Width = 20;
            worksheet.Column(10).Width = 22;
        }

        /// <summary>
        /// Hiển thị popup danh sách cơ hội kinh doanh theo nhân sự với khoảng ngày lọc.
        /// </summary>
        /// <param name="id">Mã nhân sự cần xem dữ liệu.</param>
        /// <param name="type">Loại dữ liệu cơ hội kinh doanh cần hiển thị.</param>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Partial view danh sách cơ hội kinh doanh theo nhân sự.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult OpportunityByUser(int id, int type, string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var model = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                EmployeeIds = id.ToString(),
                Type = type
            };
            return PartialView("OpportunityByUser", model);
        }

        /// <summary>
        /// Trả danh sách cơ hội kinh doanh theo nhân sự trong khoảng thời gian lọc.
        /// </summary>
        /// <param name="type">Loại dữ liệu cơ hội kinh doanh cần lấy.</param>
        /// <param name="userId">Danh sách mã nhân sự cần lọc.</param>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetOpportunityByUser(int type, string userId, string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var search = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                EmployeeIds = userId,
                Type = type
            };

            var data = _dashboardCache.GetOpportunities(out var total, search);

            return Json(
                new { recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup danh sách cơ hội kinh doanh theo nhân sự với khoảng ngày lọc.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="successRate">Tỷ lệ thành công</param>
        /// <returns>Partial view danh sách cơ hội kinh doanh theo nhân sự.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult OpportunityBySuccessRate(string fromDate, string toDate, string successRate, bool isGreaterOrEqual = false)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var model = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                SuccessRate = decimal.TryParse(successRate, out var rate) ? rate : 0,
                IsGreaterOrEqual = isGreaterOrEqual,
            };
            return PartialView("OpportunityBySuccessRate", model);
        }

        /// <summary>
        /// Trả danh sách cơ hội kinh doanh theo nhân sự trong khoảng thời gian lọc.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="successRate">Tỷ lệ thành công</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetOpportunityBySuccessRate(string fromDate, string toDate, string successRate, bool isGreaterOrEqual = false)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var search = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                SuccessRate = decimal.TryParse(successRate, out var rate) ? rate : 0,
                IsGreaterOrEqual = isGreaterOrEqual,
                Username = User.UserName
            };

            var data = _dashboardCache.GetOpportunitiesV2(out var total, search);

            return Json(
                new { recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup danh sách dự án theo nhân sự với khoảng ngày lọc.
        /// </summary>
        /// <param name="id">Mã nhân sự cần xem dữ liệu.</param>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Partial view danh sách dự án theo nhân sự.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ProjectByUser(int id, string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var model = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                EmployeeIds = id.ToString()
            };
            return PartialView("ProjectByUser", model);
        }

        /// <summary>
        /// Trả danh sách dự án theo nhân sự trong khoảng thời gian lọc.
        /// </summary>
        /// <param name="userId">Danh sách mã nhân sự cần lọc.</param>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProjectByUser(string userId, string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var search = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                EmployeeIds = userId
            };

            var data = _dashboardCache.GetProjects(out var total, search);

            return Json(
                new { recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup danh sách dự án theo nhân sự với khoảng ngày lọc.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="successRate">Mã nhân sự cần xem dữ liệu.</param>
        /// <returns>Partial view danh sách dự án theo nhân sự.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ProjectBySuccessRate(string fromDate, string toDate, string successRate, bool isGreaterOrEqual = false)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var model = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                SuccessRate = decimal.TryParse(successRate, out var rate) ? rate : 0,
                IsGreaterOrEqual = isGreaterOrEqual,
            };
            return PartialView("ProjectBySuccessRate", model);
        }

        /// <summary>
        /// Trả danh sách dự án theo nhân sự trong khoảng thời gian lọc.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="successRate">Danh sách mã nhân sự cần lọc.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProjectBySuccessRate(string fromDate, string toDate, string successRate, bool isGreaterOrEqual = false)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var search = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                SuccessRate = decimal.TryParse(successRate, out var rate) ? rate : 0,
                IsGreaterOrEqual = isGreaterOrEqual,
                Username = User.UserName
            };

            var data = _dashboardCache.GetProjectsV2(out var total, search);

            return Json(
                new { recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Tải lại danh sách kế hoạch kinh doanh theo khoảng ngày lọc và từ khóa tìm kiếm.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="keyword">Từ khóa tìm kiếm kế hoạch.</param>
        /// <returns>Partial view danh sách kế hoạch kinh doanh.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult PlanFilter(string fromDate, string toDate, string keyword)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var hasKeyword = !string.IsNullOrWhiteSpace(keyword);
            var searchFrom = hasKeyword ? new DateTime(DateTime.Now.Year - 5, 1, 1) : dtFrom;
            var searchTo = hasKeyword ? new DateTime(DateTime.Now.Year + 5, 12, 31) : dtTo;
            var search = BuildSearch(searchFrom, searchTo);
            var data = GetDashboardPlans(search, User.Identity.Name);

            if (hasKeyword)
            {
                var normalizedKeyword = keyword.Trim().ToLowerInvariant();
                var matched = data
                    .Where(x =>
                    {
                        var content = string.IsNullOrEmpty(x.Content) ? string.Empty : Regex.Replace(x.Content, "<.*?>", " ");
                        var text = string.Join(" ", x.PlanName, x.OpportunityName, x.CustomerName, x.AddressMeeting, x.EmployeeFullName, content).ToLowerInvariant();
                        return text.Contains(normalizedKeyword);
                    })
                    .OrderBy(x => x.WorkingDate ?? DateTime.MaxValue)
                    .FirstOrDefault();

                if (matched != null && matched.WorkingDate.HasValue)
                {
                    dtFrom = GetMondayOfWeek(matched.WorkingDate.Value);
                    dtTo = dtFrom.AddDays(6);
                    data = data.Where(x => x.WorkingDate.HasValue && x.WorkingDate.Value.Date >= dtFrom && x.WorkingDate.Value.Date <= dtTo).ToList();
                }
            }

            ViewBag.PlanFromDate = dtFrom;
            ViewBag.PlanToDate = dtTo;

            return PartialView("_PlanList", data);
        }

        /// <summary>
        /// Hiển thị popup danh sách kế hoạch kinh doanh theo nhân sự với khoảng ngày lọc.
        /// </summary>
        /// <param name="id">Mã nhân sự cần xem dữ liệu.</param>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Partial view danh sách kế hoạch theo nhân sự.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult PlanByUser(int id, string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var model = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                EmployeeIds = id.ToString()
            };

            return PartialView("PlanByUser", model);
        }

        /// <summary>
        /// Trả danh sách kế hoạch theo nhân sự trong khoảng thời gian lọc.
        /// </summary>
        /// <param name="userId">Danh sách mã nhân sự cần lọc.</param>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetPlanByUser(string userId, string fromDate, string toDate)
        {
            var dtFrom = ParseFromDateOrDefault(fromDate);
            var dtTo = ParseToDateOrDefault(toDate);

            var search = new DashboardSearchModel
            {
                FromDate = dtFrom,
                ToDate = dtTo,
                EmployeeIds = userId
            };

            // SP V2 trả kèm người liên quan — không cần truy vấn bổ sung theo từng kế hoạch
            var data = _dashboardCache.GetPlansV2(out var total, search);

            return Json(
                new { recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetPieChart(DashboardSearchModel model)
        {
            var data = _dashboardCache.GetPieChart(model.Type, model);
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Dữ liệu chart số cơ hội/dự án theo nhóm dịch vụ.
        /// </summary>
        /// <param name="model">Type = 1 lấy dự án, Type = 0 lấy cơ hội kinh doanh.</param>
        /// <returns>Dữ liệu JSON cho chart.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetGroupServiceChart(DashboardSearchModel model)
        {
            // Cùng điều kiện quyền với popup chi tiết để số trên chart khớp danh sách
            model.EmployeeIds = GetPermissionContext().GetAllowedEmployeeIdsString();
            model.Username = User.UserName;
            var data = _dashboardCache.GetGroupServiceChart(model.Type, model);
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup danh sách cơ hội kinh doanh thuộc một nhóm dịch vụ.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="groupServiceId">Mã nhóm dịch vụ.</param>
        /// <returns>Partial view danh sách cơ hội theo nhóm dịch vụ.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult OpportunityByGroupService(string fromDate, string toDate, int groupServiceId)
        {
            var model = new DashboardSearchModel
            {
                FromDate = ParseFromDateOrDefault(fromDate),
                ToDate = ParseToDateOrDefault(toDate),
                GroupServiceID = groupServiceId
            };
            return PartialView("OpportunityByGroupService", model);
        }

        /// <summary>
        /// Trả danh sách cơ hội kinh doanh thuộc một nhóm dịch vụ.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="groupServiceId">Mã nhóm dịch vụ.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetOpportunityByGroupService(string fromDate, string toDate, int groupServiceId)
        {
            var search = new DashboardSearchModel
            {
                FromDate = ParseFromDateOrDefault(fromDate),
                ToDate = ParseToDateOrDefault(toDate),
                GroupServiceID = groupServiceId,
                Username = User.UserName
            };

            var data = _dashboardCache.GetOpportunitiesByGroupService(out var total, search);

            return Json(
                new { recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup danh sách dự án thuộc một nhóm dịch vụ.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="groupServiceId">Mã nhóm dịch vụ.</param>
        /// <returns>Partial view danh sách dự án theo nhóm dịch vụ.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ProjectByGroupService(string fromDate, string toDate, int groupServiceId)
        {
            var model = new DashboardSearchModel
            {
                FromDate = ParseFromDateOrDefault(fromDate),
                ToDate = ParseToDateOrDefault(toDate),
                GroupServiceID = groupServiceId
            };
            return PartialView("ProjectByGroupService", model);
        }

        /// <summary>
        /// Trả danh sách dự án thuộc một nhóm dịch vụ.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu theo định dạng dd/MM/yyyy.</param>
        /// <param name="toDate">Ngày kết thúc theo định dạng dd/MM/yyyy.</param>
        /// <param name="groupServiceId">Mã nhóm dịch vụ.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProjectByGroupService(string fromDate, string toDate, int groupServiceId)
        {
            var search = new DashboardSearchModel
            {
                FromDate = ParseFromDateOrDefault(fromDate),
                ToDate = ParseToDateOrDefault(toDate),
                GroupServiceID = groupServiceId,
                Username = User.UserName
            };

            var data = _dashboardCache.GetProjectsByGroupService(out var total, search);

            return Json(
                new { recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Lấy danh sách kế hoạch hiển thị trên dashboard: kế hoạch của các nhân sự được phép xem
        /// cộng với kế hoạch mà người dùng hiện tại là người liên quan.
        /// Việc gộp người liên quan và so khớp alias được thực hiện ngay trong SP RM_Dashboard_Plan_Get_V2
        /// nên chỉ tốn 1 lượt truy vấn thay vì 2 + N lượt như trước.
        /// </summary>
        /// <param name="search">Điều kiện tìm kiếm kế hoạch.</param>
        /// <param name="currentUsername">Username người dùng hiện tại.</param>
        /// <returns>Danh sách kế hoạch kèm thông tin người liên quan.</returns>
        private List<PlanDashboardModel> GetDashboardPlans(DashboardSearchModel search, string currentUsername)
        {
            search.Username = currentUsername;

            int total;
            return _dashboardCache.GetPlansV2(out total, search) ?? new List<PlanDashboardModel>();
        }

        [HttpPost]
        public JsonResult MarkRegulationViewed()
        {
            try
            {
                _userCache.MarkRegulation(User.UserName);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);

                return Json(new { success = false });
            }
        }
    }
}
