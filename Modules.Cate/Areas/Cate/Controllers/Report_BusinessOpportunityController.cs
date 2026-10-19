using Core.Cate.Caches;
using Core.Cate.Services;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;
using Core.Cate.Models;

namespace Modules.Cate.Areas.Cate.Controllers
{
    public class Report_BusinessOpportunityController : AppController
    {
        private const string STATUS_KEY_PROJECT = "Project";
        private const string STATUS_KEY_OPPORTUNITY = "Opportunity";

        private readonly RM_Report_BusinessOpportunityCache _Report_BusinessOpportunityCache;
        private readonly RM_CustomerTypeCache _customerTypeCache;
        private readonly Cate_ProductServiceCache _productServiceCache;
        private readonly RM_StatusCache _statusCache;
        private readonly RM_DigitalSalesCache _digitalSalesCache;
        private readonly string _Title = AppProcessor.Messagor.GetMessage("Report_BusinessOpportunity_Title");

        public Report_BusinessOpportunityController()
        {
            _Report_BusinessOpportunityCache = new RM_Report_BusinessOpportunityCache();
            _customerTypeCache = new RM_CustomerTypeCache();
            _productServiceCache = new Cate_ProductServiceCache();
            _statusCache = new RM_StatusCache();
            _digitalSalesCache = new RM_DigitalSalesCache();
        }

        // GET: Cate/Report_BusinessOpportunity
        public ActionResult Index()
        {
            ViewBag.Title = _Title;

            var model = new RM_Report_BusinessOpportunitySearchModel
            {
                Nam = DateTime.Now.Year
            };

            BuildFilterLists(model);

            return View(model);
        }

        /// <summary>
        /// Nạp dữ liệu cho các dropdown lọc của báo cáo.
        /// </summary>
        /// <param name="model">Model tìm kiếm cần bổ sung danh sách lọc.</param>
        private void BuildFilterLists(RM_Report_BusinessOpportunitySearchModel model)
        {
            model.ListCustomerType = (_customerTypeCache.GetAll() ?? new List<RM_CustomerTypeModel>())
                .Select(x => new SelectListItem
                {
                    Value = x.CustomerTypeID.ToString(),
                    Text = x.CustomerTypeName
                })
                .OrderBy(x => x.Text)
                .ToList();

            model.ListProductService = _productServiceCache.GetAll();

            var projectStatuses = _digitalSalesCache.GetStatusList(2) ?? new List<RM_DigitalSalesStatusModel>();
            var opportunityStatuses = _digitalSalesCache.GetStatusList(1) ?? new List<RM_DigitalSalesStatusModel>();

            model.ListProjectStatus = projectStatuses
                .Select(x => new SelectListItem
                {
                    Value = x.StatusID.ToString(),
                    Text = x.StatusName
                })
                .ToList();

            model.ListOpportunityStatus = opportunityStatuses
                .Select(x => new SelectListItem
                {
                    Value = x.StatusID.ToString(),
                    Text = x.StatusName
                })
                .ToList();
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(RM_Report_BusinessOpportunitySearchModel model)
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
            var data = _Report_BusinessOpportunityCache.Get(out var total, model, dataSearch);
            var result = Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
            return result;
        }


        /// <summary>
        /// ExportExcel
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Export(RM_Report_BusinessOpportunitySearchModel model)
        {
            string urlFile = HostingEnvironment.MapPath(@"\Contents\Files\MauExport\BAOCAO_COHOIKINHDOANH.xlsx");

            var data = _Report_BusinessOpportunityCache.GetAll(model);
            if (data == null)
            {
                return Json(new { status = false, message = AppProcessor.Messagor.GetMessage("No_Data") });
            }

            var package = new ExcelPackage(new FileInfo(urlFile));
            // danh sách đơn vị xác nhận
            var worksheet = package.Workbook.Worksheets.First(); // Lấy sheet đầu tiên trong file
            int rowIndexSheet = 2;
            for (int i = 0; i < data.Count; i++)
            {
                worksheet.Cells["A" + (rowIndexSheet + i)].Value = i + 1;
                worksheet.Cells["B" + (rowIndexSheet + i)].Value = data[i].CustomerName ?? "";
                worksheet.Cells["C" + (rowIndexSheet + i)].Value = data[i].CustomerTypeName ?? "";
                worksheet.Cells["D" + (rowIndexSheet + i)].Value = data[i].CustomerGroupName ?? "";
                worksheet.Cells["E" + (rowIndexSheet + i)].Value = data[i].ProductServiceNames ?? "";
                worksheet.Cells["F" + (rowIndexSheet + i)].Value = data[i].ProjectName ?? "";
                worksheet.Cells["G" + (rowIndexSheet + i)].Value = data[i].ProjectTypeName ?? "";
                worksheet.Cells["H" + (rowIndexSheet + i)].Value = data[i].OpportunityStatusName ?? "";
                worksheet.Cells["I" + (rowIndexSheet + i)].Value = data[i].ContactPersonInfo ?? "";
                worksheet.Cells["J" + (rowIndexSheet + i)].Value = data[i].TotalExpectedValue.ToString();
                worksheet.Cells["K" + (rowIndexSheet + i)].Value = data[i].TotalVNPTValue.ToString();
                worksheet.Cells["L" + (rowIndexSheet + i)].Value = data[i].ExecutionTime ?? "";
                worksheet.Cells["O" + (rowIndexSheet + i)].Value = data[i].Note ?? "";

                /*
                //Căng giữa nội dung
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                */
            }

            //Border
            var range = worksheet.Cells[1, 1, rowIndexSheet + data.Count - 1, 16];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.WrapText = true; // Set wrap text for the range

            //int lastRow = rowIndexSheet + data.Count + 1;

            //// Gộp cột B đến F ở dòng cuối cùng
            //var range_Sum = worksheet.Cells[$"B{lastRow}:F{lastRow}"];
            //range_Sum.Merge = true;
            //range_Sum.Value = $"Tổng cộng: {data.Count} DT";
            //range_Sum.Style.Font.Bold = true;
            //range_Sum.Style.Font.Italic = true;
            //range_Sum.Style.Font.Size = 14;
            //range_Sum.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;


            var fileStream = new MemoryStream();
            package.SaveAs(fileStream);
            fileStream.Position = 0;
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fsr = new FileStreamResult(fileStream, contentType);
            fsr.FileDownloadName = "BAOCAO_COHOIKINHDOANH_" + DateTime.Now.ToString("ddMMyyyy") + ".xlsx";
            return fsr;
        }


    }
}