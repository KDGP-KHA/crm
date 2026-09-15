using Core.Cate.Models;
using Core.Cate.Caches;
using System.Web.Mvc;
using Core.Sys.BaseApp;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using System;
using TSFramework.Libs.Models.Base;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TSFramework.Libs.Processors;


namespace Modules.Cate.Areas.Cate.Controllers
{
    public class ReviewReportController : AppController
    {
        private readonly ReviewReportCache _reviewReportCache;
        private readonly RM_ReviewBatchCache _reviewBatchCache;
        public ReviewReportController()
        {
            _reviewReportCache = new ReviewReportCache();
            _reviewBatchCache = new RM_ReviewBatchCache();
        }

        public ActionResult Index(int? id)
        {
            var model = new ReviewReportSearchModel
            {
                ListReviewPatch = _reviewBatchCache.GetAll()
            };
            if (id != null)
            {
                model.ReviewBatchID = id.Value;
            }
            return View(model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(ReviewReportSearchModel searchModel)
        {
            var search = Request.Form.GetValues("search[value]")?[0];
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);
            var dataSearch = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };

            var data = _reviewReportCache.Get(out var total, searchModel, dataSearch);
            return Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportExcel(ReviewReportSearchModel searchModel)
        {
            try
            {
                var dataSearch = new BaseSearchModel
                {
                    Search = searchModel.Search,
                    Order = "0",
                    OrderDir = "ASC",
                    StartIndex = 0,
                    PageSize = -1
                };

                var data = _reviewReportCache.Get(
                    out var total,
                    searchModel,
                    dataSearch);

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("BaoCaoRaSoat");

                    //---------------------------------------------------
                    // HEADER
                    //---------------------------------------------------

                    ws.Cells[1, 1].Value = AppProcessor.Messagor.GetMessage("Column_No");
                    ws.Cells[1, 2].Value = AppProcessor.Messagor.GetMessage("ReviewReport_DigitalSales");
                    ws.Cells[1, 3].Value = AppProcessor.Messagor.GetMessage("ReviewConclusion_Final_Label");
                    ws.Cells[1, 4].Value = AppProcessor.Messagor.GetMessage("ReviewReport_Level4");
                    ws.Cells[1, 5].Value = AppProcessor.Messagor.GetMessage("ReviewReport_Level3");
                    ws.Cells[1, 6].Value = AppProcessor.Messagor.GetMessage("ReviewReport_Level2");

                    using (var range = ws.Cells[1, 1, 1, 6])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.HorizontalAlignment =
                            ExcelHorizontalAlignment.Center;

                        range.Style.VerticalAlignment =
                            ExcelVerticalAlignment.Center;

                        range.Style.WrapText = true;
                    }

                    //---------------------------------------------------
                    // DATA
                    //---------------------------------------------------

                    int row = 2;

                    for (int i = 0; i < data.Count; i++)
                    {
                        var item = data[i];

                        ws.Cells[row, 1].Value = i + 1;
                        ws.Cells[row, 2].Value = string.IsNullOrWhiteSpace(item.ObjectCode)
                            ? item.ObjectName
                            : $"{item.ObjectCode} - {item.ObjectName}";
                        ws.Cells[row, 3].Value = GetConclusionText(item.FinalReviewConclusion);

                        ws.Cells[row, 4].Value =
                            FormatReviewForExcel(item.Level4Reviewer,
                                                 item.Level4Date,
                                                 item.Level4Comment,
                                                 item.Level4Status);

                        ws.Cells[row, 5].Value =
                            FormatReviewForExcel(item.Level3Reviewer,
                                                 item.Level3Date,
                                                 item.Level3Comment,
                                                 item.Level3Status);

                        ws.Cells[row, 6].Value =
                            FormatReviewForExcel(item.Level2Reviewer,
                                                 item.Level2Date,
                                                 item.Level2Comment,
                                                 item.Level2Status);

                        row++;
                    }

                    //---------------------------------------------------
                    // STYLE
                    //---------------------------------------------------

                    ws.Cells[ws.Dimension.Address].Style.WrapText = true;

                    ws.Column(1).Width = 8;
                    ws.Column(2).Width = 40;
                    ws.Column(3).Width = 24;
                    ws.Column(4).Width = 50;
                    ws.Column(5).Width = 50;
                    ws.Column(6).Width = 50;

                    ws.Cells[ws.Dimension.Address]
                        .Style.VerticalAlignment =
                            ExcelVerticalAlignment.Top;

                    ws.Cells[ws.Dimension.Address]
                        .Style.Border.Top.Style =
                            ExcelBorderStyle.Thin;

                    ws.Cells[ws.Dimension.Address]
                        .Style.Border.Left.Style =
                            ExcelBorderStyle.Thin;

                    ws.Cells[ws.Dimension.Address]
                        .Style.Border.Right.Style =
                            ExcelBorderStyle.Thin;

                    ws.Cells[ws.Dimension.Address]
                        .Style.Border.Bottom.Style =
                            ExcelBorderStyle.Thin;

                    //---------------------------------------------------
                    // EXPORT
                    //---------------------------------------------------

                    var fileName =
                        $"BaoCaoRaSoat_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                    return File(
                        package.GetAsByteArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior.AllowGet);
            }
        }

        // Hàm format chuỗi html
        private string FormatReviewForExcel(string reviewer, DateTime? createdDate, string reviewComment, bool? isConfirmed)
        {
            // nếu toàn bộ rỗng thì return rỗng
            if (string.IsNullOrWhiteSpace(reviewer)
                && createdDate == null
                && string.IsNullOrWhiteSpace(reviewComment)
                && isConfirmed == null)
            {
                return string.Empty;
            }

            // decode html
            var decoded = System.Web.HttpUtility.HtmlDecode(reviewComment ?? "");

            // remove html tag
            var plainText = Regex.Replace(decoded, "<.*?>", string.Empty);

            // clean
            plainText = plainText
                .Replace("&nbsp;", " ")
                .Trim();

            var lines = new List<string>();

            // reviewer + date
            if (!string.IsNullOrWhiteSpace(reviewer) || createdDate != null)
            {
                lines.Add(reviewer + (createdDate != null
                    ? " - " + createdDate.Value.ToString("dd/MM/yyyy HH:mm")
                    : ""));
            }

            // comment
            if (!string.IsNullOrWhiteSpace(plainText))
            {
                lines.Add(plainText);
            }

            // status
            if (isConfirmed != null)
            {
                lines.Add(
                    isConfirmed == true
                        ? AppProcessor.Messagor.GetMessage("ReviewDigitalSales_Confirmed")
                        : AppProcessor.Messagor.GetMessage("ReviewDigitalSales_Commented")
                );
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string GetConclusionText(byte? conclusion)
        {
            if (conclusion == RM_ReviewConclusion.Accepted)
                return AppProcessor.Messagor.GetMessage("ReviewConclusion_Accepted");
            if (conclusion == RM_ReviewConclusion.Interested)
                return AppProcessor.Messagor.GetMessage("ReviewConclusion_Interested");
            if (conclusion == RM_ReviewConclusion.Rejected)
                return AppProcessor.Messagor.GetMessage("ReviewConclusion_Rejected");
            return string.Empty;
        }
    }

}
