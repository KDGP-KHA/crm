using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Processors;

namespace Modules.Cate.Areas.Cate.Controllers
{
    public class ProjectTaskReportController : AppController
    {

        private const string DateFormat = "dd/MM/yyyy";
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly string _reportTitle = AppProcessor.Messagor.GetMessage("ProjectTaskReport_Title");

        private readonly RM_ProjectTaskReportCache _reportCache;

        public ProjectTaskReportController()
        {
            _reportCache = new RM_ProjectTaskReportCache();
        }

        [HttpGet]
        [ActionType(Type = EnumActionType.View)]

        public ActionResult Index()
        {
            return View(CreateSearchModelForCurrentWeek());
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
        /// Tải partial kết quả báo cáo theo bộ lọc người dùng đã chọn.
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetReport(RM_ProjectTaskReportSearchModel search)
        {
            return PartialView("_Report", BuildReportResult(search));
        }

        /// <summary>
        /// Xuất Excel từ cùng một nguồn dữ liệu đang dùng cho bảng kết quả để tránh lệch logic hiển thị.
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Export(RM_ProjectTaskReportSearchModel search)
        {
            var report = BuildReportResult(search);
            var fileName = string.Format(
                "Bao_cao_cong_viec_du_an_theo_tuan_{0}_{1}_{2}.xlsx",
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
        /// Đọc dữ liệu thô từ cache, chuẩn hóa lại và đóng gói thành model dùng chung cho grid và export.
        /// </summary>
        private RM_ProjectTaskReportResultModel BuildReportResult(RM_ProjectTaskReportSearchModel search)
        {
            var safeSearch = search ?? new RM_ProjectTaskReportSearchModel();
            var weekRange = ResolveWeekRange(safeSearch);

            var rawItems = _reportCache.GetReport(
                               weekRange.Item1,
                               weekRange.Item2,
                               User.UserName)
                           ?? new List<RM_ProjectTaskReportModel>();

            return new RM_ProjectTaskReportResultModel
            {
                WeekFrom = weekRange.Item1,
                WeekTo = weekRange.Item2,
                Items = NormalizeReportItems(rawItems)
            };
        }

        /// <summary>
        /// Làm sạch dữ liệu text trước khi render hoặc export: bỏ HTML và chuẩn hóa nội dung trao đổi, kế hoạch.
        /// </summary>
        private List<RM_ProjectTaskReportModel> NormalizeReportItems(IEnumerable<RM_ProjectTaskReportModel> items)
            {
                return (items ?? Enumerable.Empty<RM_ProjectTaskReportModel>())
                .Select(item =>
                {
                    var comments = new List<ProjectTaskCommentDto>();

                    if (!string.IsNullOrWhiteSpace(item.LastComment))
                    {
                        try
                        {
                            comments =
                                JsonConvert.DeserializeObject<List<ProjectTaskCommentDto>>
                                (
                                    item.LastComment
                                ) ?? new List<ProjectTaskCommentDto>();
                        }
                        catch
                        {
                            comments = new List<ProjectTaskCommentDto>();
                        }
                    }

                    return new RM_ProjectTaskReportModel
                    {
                        Type = item.Type,
                        ObjectID = item.ObjectID,
                        ObjectName = NormalizeSingleLine(item.ObjectName),
                        LastTaskDate = item.LastTaskDate,
                        AMNames = NormalizeSingleLine(item.AMNames),
                        LastComment = item.LastComment,
                        CommentItems = comments,
                        LastTaskName = NormalizeSingleLine(item.LastTaskName),
                        LastCommentBy = NormalizeSingleLine(item.LastCommentBy),
                        LastCommentDate = item.LastCommentDate
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Quy đổi giá trị tuần người dùng chọn về khoảng từ Thứ Hai đến Chủ Nhật.
        /// </summary>
        private Tuple<DateTime, DateTime> ResolveWeekRange(RM_ProjectTaskReportSearchModel search)
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
        /// Chuẩn hóa một dòng text về dạng một khoảng trắng giữa các từ.
        /// </summary>
        private string NormalizeSingleLine(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Khởi tạo bộ lọc mặc định cho tuần hiện tại và danh sách phòng ban user được phép xem.
        /// </summary>
        private RM_ProjectTaskReportSearchModel CreateSearchModelForCurrentWeek()
        {
            var weekStart = GetMondayOfWeek(DateTime.Today);
            var weekEnd = weekStart.AddDays(6);

            return new RM_ProjectTaskReportSearchModel
            {
                WeekDate = weekStart.ToString(DateFormat),
                FromDate = weekStart.ToString(DateFormat),
                ToDate = weekEnd.ToString(DateFormat),
            };
        }

        private void BuildExcelWorksheet(ExcelWorksheet ws, RM_ProjectTaskReportResultModel report)
        {
            int row = 1;

            #region Title

            ws.Cells[row, 1].Value = _reportTitle;
            ws.Cells[row, 1, row, 5].Merge = true;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 16;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            row += 2;

            ws.Cells[row, 1].Value = $"Tuần: {report.WeekFrom:dd/MM/yyyy} - {report.WeekTo:dd/MM/yyyy}";
            ws.Cells[row, 1].Style.Font.Bold = true;

            row += 2;

            #endregion

            #region Header

            ws.Cells[row, 1].Value = "STT";
            ws.Cells[row, 2].Value = "Tên dự án / kế hoạch";
            ws.Cells[row, 3].Value = "AM";
            ws.Cells[row, 4].Value = "Công việc gần nhất";
            ws.Cells[row, 5].Value = "Thông tin trao đổi";

            using (var header = ws.Cells[row, 1, row, 5])
            {
                header.Style.Font.Bold = true;
                header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                header.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                header.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                header.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                header.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            row++;

            #endregion

            var projects = report.Items
                .Where(x => x.Type == 1)
                .ToList();

            var opportunities = report.Items
                .Where(x => x.Type == 2)
                .ToList();

            #region Dự án

            if (projects.Any())
            {
                ws.Cells[row, 1].Value = "DANH SÁCH DỰ ÁN";
                ws.Cells[row, 1, row, 5].Merge = true;

                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                row++;

                int stt = 1;

                foreach (var item in projects)
                {
                    ws.Cells[row, 1].Value = stt++;
                    ws.Cells[row, 2].Value = item.ObjectName;
                    ws.Cells[row, 3].Value = item.AMNames;

                    #region Công việc gần nhất

                    string taskText = item.LastTaskName ?? "";

                    if (item.LastTaskDate.HasValue)
                    {
                        taskText += Environment.NewLine +
                                    item.LastTaskDate.Value.ToString("dd/MM/yyyy HH:mm");
                    }

                    ws.Cells[row, 4].Value = taskText;

                    #endregion

                    #region Thông tin trao đổi

                    string commentText = "";

                    if (item.CommentItems != null)
                    {
                        foreach (var comment in item.CommentItems
                            .OrderByDescending(x => x.CreatedDate))
                        {
                            commentText +=
                                $"{comment.TaskName} | " +
                                $"{comment.CreatedDate:dd/MM/yyyy} | " +
                                $"{comment.EmployeeName}";

                            commentText += Environment.NewLine;

                            commentText += HtmlToText(comment.Content);

                            commentText += Environment.NewLine;
                            commentText += "--------------------------------";
                            commentText += Environment.NewLine;
                        }
                    }

                    ws.Cells[row, 5].Value = commentText;
                    ws.Cells[row, 5].Style.WrapText = true;

                    #endregion

                    row++;
                }
            }

            #endregion

            #region Cơ hội

            if (opportunities.Any())
            {
                ws.Cells[row, 1].Value = "DANH SÁCH KẾ HOẠCH";
                ws.Cells[row, 1, row, 5].Merge = true;

                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                row++;

                int stt = 1;

                foreach (var item in opportunities)
                {
                    ws.Cells[row, 1].Value = stt++;
                    ws.Cells[row, 2].Value = item.ObjectName;
                    ws.Cells[row, 3].Value = item.AMNames;
                    ws.Cells[row, 4].Value = "";

                    string commentText = "";

                    if (item.CommentItems != null)
                    {
                        foreach (var comment in item.CommentItems
                            .OrderByDescending(x => x.ExchangeDate))
                        {
                            commentText +=
                                $"{comment.EmployeeName}";

                            commentText += Environment.NewLine;

                            commentText +=
                                $"Người liên hệ: {comment.ContactPersonName}";

                            if (!string.IsNullOrWhiteSpace(
                                comment.ContactPersonPosition))
                            {
                                commentText +=
                                    $" ({comment.ContactPersonPosition})";
                            }

                            commentText += Environment.NewLine;

                            commentText +=
                                $"Thời gian liên hệ: {comment.ExchangeDate:dd/MM/yyyy HH:mm}";

                            commentText += Environment.NewLine;

                            commentText += HtmlToText(comment.ExchangeContent);

                            commentText += Environment.NewLine;
                            commentText += "--------------------------------";
                            commentText += Environment.NewLine;
                        }
                    }

                    ws.Cells[row, 5].Value = commentText;
                    ws.Cells[row, 5].Style.WrapText = true;

                    row++;
                }
            }

            #endregion

            #region Format

            using (var range = ws.Cells[5, 1, row - 1, 5])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                range.Style.WrapText = true;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }

            ws.Column(1).Width = 8;
            ws.Column(2).Width = 35;
            ws.Column(3).Width = 35;
            ws.Column(4).Width = 30;
            ws.Column(5).Width = 70;

            #endregion
        }

        private string HtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            html = html.Replace("<br>", "\n")
                       .Replace("<br/>", "\n")
                       .Replace("<br />", "\n")
                       .Replace("</p>", "\n");

            html = Regex.Replace(html, "<.*?>", "");

            return HttpUtility.HtmlDecode(html);
        }
    }
}
