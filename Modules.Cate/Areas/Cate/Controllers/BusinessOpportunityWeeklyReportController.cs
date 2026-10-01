using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Processors;

namespace Modules.Cate.Areas.Cate.Controllers
{
    public class BusinessOpportunityWeeklyReportController : AppController
    {
        private const string AllTextMessageKey = "Common_Label_All";
        private const string DateFormat = "dd/MM/yyyy";
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string WeeklyPlanDatePattern = @"^(?:Ngày\s*)?(?<date>\d{2}/\d{2}/\d{4})(?:\s+(?<time>\d{2}:\d{2}))?(?:\s*-\s*(?<title>.+))?$";

        private readonly RM_BusinessOpportunityWeeklyReportCache _reportCache;
        private readonly MN_EmployeeCache _employeeCache;
        private readonly SysUserCache _userCache;
        private readonly SysUserBoPhanCache _userBoPhanCache;
        private readonly string _allText;
        private readonly string _reportTitle;

        public BusinessOpportunityWeeklyReportController()
        {
            _reportCache = new RM_BusinessOpportunityWeeklyReportCache();
            _employeeCache = new MN_EmployeeCache();
            _userCache = new SysUserCache();
            _userBoPhanCache = new SysUserBoPhanCache();
            _allText = AppProcessor.Messagor.GetMessage(AllTextMessageKey);
            _reportTitle = AppProcessor.Messagor.GetMessage("Report_Title");
        }

        /// <summary>
        /// Hiển thị màn hình báo cáo và khởi tạo bộ lọc mặc định theo tuần hiện tại.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Index()
        {
            return View(CreateSearchModelForCurrentWeek());
        }

        /// <summary>
        /// Lấy danh sách nhân viên theo phòng ban được chọn và giới hạn theo phạm vi user được phép xem.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public JsonResult GetEmployeeByDepartment(int? boPhanId)
        {
            var accessibleDepartments = GetAccessibleDepartments();
            var accessibleDepartmentIds = accessibleDepartments
                .Select(x => x.BoPhan_ID)
                .Distinct()
                .ToList();

            var employees = boPhanId.HasValue && boPhanId.Value > 0
                ? _employeeCache.GetByBoPhanID(boPhanId.Value)
                : _employeeCache.GetAll()
                    .Where(x => accessibleDepartmentIds.Contains(x.BoPhan_ID))
                    .ToList();

            var items = employees
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.Employee_Code)
                .Select(x => new SelectListItem
                {
                    Value = x.Employee_ID.ToString(),
                    Text = string.Format("{0} - {1}", x.FullName, x.Employee_Code)
                })
                .ToList();

            return Json(items, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Tải partial kết quả báo cáo theo bộ lọc người dùng đã chọn.
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetReport(RM_BusinessOpportunityWeeklyReportSearchModel search)
        {
            return PartialView("_Report", BuildReportResult(search));
        }

        /// <summary>
        /// Xuất Excel từ cùng một nguồn dữ liệu đang dùng cho bảng kết quả để tránh lệch logic hiển thị.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Export(RM_BusinessOpportunityWeeklyReportSearchModel search)
        {
            var report = BuildReportResult(search);
            var fileName = string.Format(
                "BaocaoTH_CohoiKD_tuan_{0}_{1}_{2}.xlsx",
                report.WeekFrom.ToString("ddMM"),
                report.WeekTo.ToString("ddMM"),
                DateTime.Now.ToString("ddMMyyyyHHmm"));

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                BuildExcelWorksheet(worksheet, report);

                return File(package.GetAsByteArray(), ExcelContentType, fileName);
            }
        }

        /// <summary>
        /// Khởi tạo bộ lọc mặc định cho tuần hiện tại và danh sách phòng ban user được phép xem.
        /// </summary>
        private RM_BusinessOpportunityWeeklyReportSearchModel CreateSearchModelForCurrentWeek()
        {
            var weekStart = GetMondayOfWeek(DateTime.Today);
            var weekEnd = weekStart.AddDays(6);

            return new RM_BusinessOpportunityWeeklyReportSearchModel
            {
                WeekDate = weekStart.ToString(DateFormat),
                FromDate = weekStart.ToString(DateFormat),
                ToDate = weekEnd.ToString(DateFormat),
                Departments = GetAccessibleDepartments()
            };
        }

        /// <summary>
        /// Đọc dữ liệu thô từ cache, chuẩn hóa lại và đóng gói thành model dùng chung cho grid và export.
        /// </summary>
        private RM_BusinessOpportunityWeeklyReportResultModel BuildReportResult(RM_BusinessOpportunityWeeklyReportSearchModel search)
        {
            var safeSearch = search ?? new RM_BusinessOpportunityWeeklyReportSearchModel();
            var weekRange = ResolveWeekRange(safeSearch);

            var rawItems = _reportCache.GetReport(
                               weekRange.Item1,
                               weekRange.Item2,
                               safeSearch.BoPhanID,
                               safeSearch.EmployeeIDs,
                               User.UserName)
                           ?? new List<RM_BusinessOpportunityWeeklyReportModel>();

            return new RM_BusinessOpportunityWeeklyReportResultModel
            {
                WeekFrom = weekRange.Item1,
                WeekTo = weekRange.Item2,
                DepartmentFilterText = BuildDepartmentFilterText(safeSearch.BoPhanID),
                EmployeeFilterText = BuildEmployeeFilterText(safeSearch.EmployeeIDs),
                Items = NormalizeReportItems(rawItems)
            };
        }

        /// <summary>
        /// Quy đổi giá trị tuần người dùng chọn về khoảng từ Thứ Hai đến Chủ Nhật.
        /// </summary>
        private Tuple<DateTime, DateTime> ResolveWeekRange(RM_BusinessOpportunityWeeklyReportSearchModel search)
        {
            var fallbackWeekStart = GetMondayOfWeek(DateTime.Today);
            var defaultRange = Tuple.Create(fallbackWeekStart, fallbackWeekStart.AddDays(6));

            if (search == null)
            {
                return defaultRange;
            }

            DateTime fromDate;
            DateTime toDate;
            if (TryParseDate(search.FromDate, out fromDate)
                && TryParseDate(search.ToDate, out toDate)
                && toDate >= fromDate)
            {
                var weekStart = GetMondayOfWeek(fromDate);
                return Tuple.Create(weekStart, weekStart.AddDays(6));
            }

            DateTime weekDate;
            if (TryParseDate(search.WeekDate, out weekDate))
            {
                var weekStart = GetMondayOfWeek(weekDate);
                return Tuple.Create(weekStart, weekStart.AddDays(6));
            }

            return defaultRange;
        }

        /// <summary>
        /// Parse ngày theo đúng định dạng bộ lọc tuần của màn hình.
        /// </summary>
        private bool TryParseDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        /// <summary>
        /// Quy đổi một ngày bất kỳ về ngày đầu tuần tương ứng.
        /// </summary>
        private DateTime GetMondayOfWeek(DateTime date)
        {
            var diff = date.DayOfWeek == DayOfWeek.Sunday
                ? -6
                : DayOfWeek.Monday - date.DayOfWeek;

            return date.Date.AddDays(diff);
        }

        /// <summary>
        /// Tạo text hiển thị cho bộ lọc phòng ban.
        /// </summary>
        private string BuildDepartmentFilterText(int boPhanId)
        {
            if (boPhanId <= 0)
            {
                return _allText;
            }

            var department = GetAccessibleDepartments().FirstOrDefault(x => x.BoPhan_ID == boPhanId);
            return department?.TenBoPhanView ?? _allText;
        }

        /// <summary>
        /// Tạo text hiển thị cho bộ lọc nhân viên.
        /// </summary>
        private string BuildEmployeeFilterText(string employeeIds)
        {
            var selectedIds = ParseEmployeeIds(employeeIds);
            if (!selectedIds.Any())
            {
                return _allText;
            }

            var employeeNames = _employeeCache.GetAll()
                .Where(x => selectedIds.Contains(x.Employee_ID))
                .OrderBy(x => x.FullName)
                .Select(x => x.FullName)
                .ToList();

            return employeeNames.Any() ? string.Join(", ", employeeNames) : _allText;
        }

        /// <summary>
        /// Parse danh sách EmployeeID từ chuỗi dạng "1;2;3" và loại bỏ giá trị trùng hoặc không hợp lệ.
        /// </summary>
        private List<int> ParseEmployeeIds(string employeeIds)
        {
            return (employeeIds ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    int id;
                    return int.TryParse(x, out id) ? id : 0;
                })
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách phòng ban mà user hiện tại được gán quyền xem qua Sys_UserBoPhan_GetByEmail.
        /// </summary>
        private List<MN_BoPhanModel> GetAccessibleDepartments()
        {
            var currentUser = _userCache.GetByUserName(User.UserName);
            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.Email))
            {
                return new List<MN_BoPhanModel>();
            }

            return (_userBoPhanCache.GetByEmail(currentUser.Email) ?? new List<MN_BoPhanModel>())
                .GroupBy(x => x.BoPhan_ID)
                .Select(x => x.First())
                .OrderBy(x => x.TenBoPhanView)
                .ToList();
        }

        /// <summary>
        /// Làm sạch dữ liệu text trước khi render hoặc export: bỏ HTML và chuẩn hóa nội dung trao đổi, kế hoạch.
        /// </summary>
        private List<RM_BusinessOpportunityWeeklyReportModel> NormalizeReportItems(IEnumerable<RM_BusinessOpportunityWeeklyReportModel> items)
        {
            return (items ?? Enumerable.Empty<RM_BusinessOpportunityWeeklyReportModel>())
                .Select(item => new RM_BusinessOpportunityWeeklyReportModel
                {
                    BoPhanID = item.BoPhanID,
                    DepartmentName = item.DepartmentName,
                    DepartmentDisplayName = string.IsNullOrWhiteSpace(item.DepartmentDisplayName)
                        ? item.DepartmentName
                        : item.DepartmentDisplayName,
                    EmployeeID = item.EmployeeID,
                    EmployeeCode = item.EmployeeCode,
                    EmployeeName = item.EmployeeName,
                    BusinessOpportunityID = item.BusinessOpportunityID,
                    CodeOpportunity = item.CodeOpportunity,
                    OpportunityName = item.OpportunityName,
                    ClosingProbability = item.ClosingProbability,
                    ExpectedValue = item.ExpectedValue,
                    StageName = item.StageName,
                    CustomerName = item.CustomerName,
                    LatestExchangeInfo = CleanRichText(item.LatestExchangeInfo),
                    WeeklyPlan = FormatWeeklyPlan(item.WeeklyPlan)
                })
                .ToList();
        }

        /// <summary>
        /// Loại bỏ HTML và chuẩn hóa xuống dòng cho nội dung rich text.
        /// </summary>
        private string CleanRichText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var text = value
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("</p>", "\n")
                .Replace("</div>", "\n")
                .Replace("</li>", "\n");

            text = Regex.Replace(text, "<[^>]+>", " ");
            text = HttpUtility.HtmlDecode(text ?? string.Empty) ?? string.Empty;
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            var lines = SplitToLines(text)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return lines.Any()
                ? string.Join(Environment.NewLine, lines).Trim()
                : string.Empty;
        }

        /// <summary>
        /// Chuẩn hóa text kế hoạch tuần về dạng ngày giờ và tên kế hoạch.
        /// </summary>
        private string FormatWeeklyPlan(string value)
        {
            var lines = SplitToLines(CleanRichText(value));
            if (!lines.Any())
            {
                return string.Empty;
            }

            var blocks = new List<string>();

            for (var index = 0; index < lines.Count; index++)
            {
                var currentLine = lines[index];
                var dateMatch = Regex.Match(currentLine, WeeklyPlanDatePattern, RegexOptions.IgnoreCase);

                if (!dateMatch.Success)
                {
                    continue;
                }

                var displayDate = BuildWeeklyPlanDateText(dateMatch);
                var title = ResolveWeeklyPlanTitle(lines, ref index, dateMatch);

                blocks.Add(
                    string.IsNullOrWhiteSpace(title)
                        ? displayDate
                        : string.Format("{0}{1}({2})", displayDate, Environment.NewLine, title));
            }

            return string.Join(Environment.NewLine + Environment.NewLine, blocks.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        /// <summary>
        /// Dựng dòng hiển thị ngày giờ kế hoạch.
        /// </summary>
        private string BuildWeeklyPlanDateText(Match dateMatch)
        {
            var dateText = dateMatch.Groups["date"].Value;
            var timeText = dateMatch.Groups["time"].Value;

            if (string.IsNullOrWhiteSpace(timeText))
            {
                return dateText;
            }

            return string.Format("{0}  {1}", dateText, timeText);
        }

        /// <summary>
        /// Xác định tiêu đề kế hoạch từ cùng dòng ngày hoặc dòng kế tiếp.
        /// </summary>
        private string ResolveWeeklyPlanTitle(IList<string> lines, ref int index, Match dateMatch)
        {
            var title = (dateMatch.Groups["title"].Value ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(title) && HasNextNonDateLine(lines, index))
            {
                index++;
                title = lines[index];
            }

            return NormalizeWeeklyPlanTitle(title);
        }

        /// <summary>
        /// Kiểm tra dòng kế tiếp có phải là nội dung kế hoạch hay đã là block ngày khác.
        /// </summary>
        private bool HasNextNonDateLine(IList<string> lines, int index)
        {
            return index + 1 < lines.Count && !IsWeeklyPlanDateLine(lines[index + 1]);
        }

        /// <summary>
        /// Kiểm tra một dòng có phải là dòng ngày kế hoạch hay không.
        /// </summary>
        private bool IsWeeklyPlanDateLine(string value)
        {
            return Regex.IsMatch(value ?? string.Empty, WeeklyPlanDatePattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Chuẩn hóa tiêu đề kế hoạch, loại bỏ ngoặc ngoài nếu đã có sẵn.
        /// </summary>
        private string NormalizeWeeklyPlanTitle(string value)
        {
            var title = NormalizeSingleLine(value);
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            if (title.StartsWith("(") && title.EndsWith(")") && title.Length > 1)
            {
                title = title.Substring(1, title.Length - 2).Trim();
            }

            return title;
        }

        /// <summary>
        /// Chuẩn hóa một dòng text về dạng một khoảng trắng giữa các từ.
        /// </summary>
        private string NormalizeSingleLine(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Tách text thành danh sách dòng và loại bỏ dòng trống.
        /// </summary>
        private List<string> SplitToLines(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { '\n' }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        /// <summary>
        /// Dựng toàn bộ nội dung worksheet báo cáo.
        /// </summary>
        private void BuildExcelWorksheet(ExcelWorksheet worksheet, RM_BusinessOpportunityWeeklyReportResultModel report)
        {
            ConfigureExcelWorksheet(worksheet);
            WriteExcelHeader(worksheet, report);
            WriteExcelBody(worksheet, report);
        }

        /// <summary>
        /// Cấu hình font, grid line và độ rộng cột của worksheet.
        /// </summary>
        private void ConfigureExcelWorksheet(ExcelWorksheet worksheet)
        {
            worksheet.Cells.Style.Font.Name = "Times New Roman";
            worksheet.Cells.Style.Font.Size = 11;
            worksheet.View.ShowGridLines = true;

            worksheet.Column(1).Width = 12;
            worksheet.Column(2).Width = 25.57;
            worksheet.Column(3).Width = 60;
            worksheet.Column(4).Width = 16.43;
            worksheet.Column(5).Width = 23.43;
            worksheet.Column(6).Width = 29.14;
            worksheet.Column(7).Width = 29.14;
            worksheet.Column(8).Width = 41.57;
            worksheet.Column(9).Width = 29.71;
        }

        /// <summary>
        /// Ghi phần tiêu đề, bộ lọc và header cột cho file Excel.
        /// </summary>
        private void WriteExcelHeader(ExcelWorksheet worksheet, RM_BusinessOpportunityWeeklyReportResultModel report)
        {
            worksheet.Cells["A1:K1"].Merge = true;
            worksheet.Cells["A1"].Value = _reportTitle;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 11;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells["A2"].Value = "Tuần";
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            worksheet.Cells["B2"].Value = string.Format(
                "{0}-{1}",
                report.WeekFrom.ToString(DateFormat),
                report.WeekTo.ToString(DateFormat));

            worksheet.Cells["A3"].Value = "Phòng";
            worksheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            worksheet.Cells["B3:K3"].Merge = true;
            worksheet.Cells["B3"].Value = report.DepartmentFilterText;

            var headers = new[]
            {
                "STT",
                "Mã kế hoạch kinh doanh",
                "Tên kế hoạch",
                "Xác suất chốt (%)",
                "Doanh thu dự kiến (triệu)",
                "Giai đoạn",
                "Khách hàng",
                "Thông tin trao đổi gần nhất",
                "Kế hoạch trong tuần"
            };

            for (var index = 0; index < headers.Length; index++)
            {
                worksheet.Cells[5, index + 1].Value = headers[index];
            }

            ApplyTableHeaderStyle(worksheet.Cells[5, 1, 5, 9]);
        }

        /// <summary>
        /// Ghi nội dung chi tiết báo cáo theo nhóm phòng ban và nhân viên.
        /// </summary>
        private void WriteExcelBody(ExcelWorksheet worksheet, RM_BusinessOpportunityWeeklyReportResultModel report)
        {
            var currentRow = 6;

            foreach (var departmentGroup in report.Items.GroupBy(x => new { x.BoPhanID, x.DepartmentDisplayName }))
            {
                worksheet.Cells[currentRow, 1, currentRow, 9].Merge = true;
                worksheet.Cells[currentRow, 1].Value = departmentGroup.Key.DepartmentDisplayName;
                ApplyDepartmentGroupStyle(worksheet.Cells[currentRow, 1, currentRow, 9]);
                currentRow++;

                foreach (var employeeGroup in departmentGroup.GroupBy(x => new { x.EmployeeID, x.EmployeeName, x.EmployeeCode }))
                {
                    worksheet.Cells[currentRow, 1, currentRow, 9].Merge = true;
                    worksheet.Cells[currentRow, 1].Value = string.Format("{0} ({1})", employeeGroup.Key.EmployeeName, employeeGroup.Key.EmployeeCode);
                    ApplyEmployeeGroupStyle(worksheet.Cells[currentRow, 1, currentRow, 9]);
                    currentRow++;

                    var order = 1;
                    foreach (var item in employeeGroup)
                    {
                        WriteExcelDetailRow(worksheet, currentRow, order, item);
                        currentRow++;
                        order++;
                    }
                }
            }

            if (report.Items.Any())
            {
                return;
            }

            worksheet.Cells[currentRow, 1, currentRow, 9].Merge = true;
            worksheet.Cells[currentRow, 1].Value = "Không có dữ liệu";
            ApplyEmployeeGroupStyle(worksheet.Cells[currentRow, 1, currentRow, 9]);
        }

        /// <summary>
        /// Ghi một dòng dữ liệu cơ hội kinh doanh vào Excel.
        /// </summary>
        private void WriteExcelDetailRow(ExcelWorksheet worksheet, int row, int order, RM_BusinessOpportunityWeeklyReportModel item)
        {
            worksheet.Row(row).CustomHeight = false;

            worksheet.Cells[row, 1].Value = order;
            worksheet.Cells[row, 2].Value = item.CodeOpportunity;
            worksheet.Cells[row, 3].Value = item.OpportunityName;
            worksheet.Cells[row, 4].Value = item.ClosingProbability.ToString("0.##", CultureInfo.InvariantCulture);
            worksheet.Cells[row, 5].Value = item.ExpectedValue.ToString("0.##", CultureInfo.InvariantCulture);
            worksheet.Cells[row, 6].Value = item.StageName;
            worksheet.Cells[row, 7].Value = item.CustomerName;
            worksheet.Cells[row, 8].Value = item.LatestExchangeInfo;
            worksheet.Cells[row, 9].Value = item.WeeklyPlan;

            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Cells[row, 3].Style.WrapText = true;

            worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            worksheet.Cells[row, 8].Style.WrapText = true;

            worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            worksheet.Cells[row, 9].Style.WrapText = true;
        }

        /// <summary>
        /// Áp dụng style cho header của bảng dữ liệu Excel.
        /// </summary>
        private void ApplyTableHeaderStyle(ExcelRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.WrapText = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(184, 204, 228));
        }

        /// <summary>
        /// Áp dụng style cho dòng nhóm phòng ban.
        /// </summary>
        private void ApplyDepartmentGroupStyle(ExcelRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        /// <summary>
        /// Áp dụng style cho dòng nhóm nhân viên.
        /// </summary>
        private void ApplyEmployeeGroupStyle(ExcelRange range)
        {
            range.Style.Font.Bold = false;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }
    }
}
