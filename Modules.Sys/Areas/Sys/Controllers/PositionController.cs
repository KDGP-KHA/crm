using ClosedXML.Excel;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class PositionController : AppController
    {
        private readonly string _positionTitle = AppProcessor.Messagor.GetMessage("Position_Title") ?? "Chức vụ";
        private readonly MN_ChucVuCache _positionCache = new MN_ChucVuCache();

        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Title = _positionTitle;
            return View();
        }

        #region Position - CRUD

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add()
        {
            var model = new MN_ChucVuModel();
            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(MN_ChucVuModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Position", model);
            }
            string response;
            var result = _positionCache.Save(model, User.UserName);
            if (result == 0)
                response = CreateMessage($"{_positionTitle} [{model.TenChucVu}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_positionTitle} [{model.MaChucVu}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_positionTitle} [{model.TenChucVu}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = result > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Edit)]
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var model = _positionCache.GetById(id);
            if (model == null)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage($"{_positionTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            }
            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(MN_ChucVuModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Position", model);
            }
            string response;
            var result = _positionCache.Save(model, User.UserName);
            if (result == 0)
                response = CreateMessage($"{_positionTitle} [{model.TenChucVu}]", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_positionTitle} [{model.MaChucVu}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_positionTitle} [{model.TenChucVu}]", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = result > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _positionCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_positionTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            ViewBag.ConfirmStatus = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete") ?? "Bạn muốn xoá {0}?",
                $"{_positionTitle} [{model.TenChucVu}]");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(MN_ChucVuModel model)
        {
            var deleted = _positionCache.Delete(model, User.UserName);
            var response = CreateMessage($"{_positionTitle} [{model.TenChucVu}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = deleted > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get()
        {
            var search = Request.Form.GetValues("search[value]")?[0];
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);
            var pageIndex = startRec / pageSize;
            var dataSearch = new MN_ChucVuSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
            var data = _positionCache.Get(out int total, dataSearch);

            var result = Json(new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data }, JsonRequestBehavior.AllowGet);
            return result;
        }

        [HttpGet]
        public ActionResult Export(string keyword, string cookieName = null)
        {
            var search = new MN_ChucVuSearchModel
            {
                Keyword = keyword,
            };
            var positions = _positionCache.Export(search);

            var ms = new MemoryStream();
            using (var wb = new XLWorkbook())
            {
                BuildSheet(wb, positions);
                wb.SaveAs(ms);
            }
            if (!string.IsNullOrEmpty(cookieName))
            {
                Response.Cookies.Add(new System.Web.HttpCookie(cookieName, "done")
                {
                    Path = "/",
                    Expires = DateTime.Now.AddMinutes(1)
                });
            }
            ms.Seek(0, SeekOrigin.Begin);
            var fileName = $"DanhSachChucVu_{DateTime.Now:ddMMyyyyHHmmss}.xlsx";
            return new FileStreamResult(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };

        }
        private static void ApplyHeader(IXLWorksheet ws, string[] headers, int[] widths)
        {
            var range = ws.Range(1, 1, 1, headers.Length);
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 11;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Row(1).Height = 22;

            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(1, c + 1).Value = headers[c];
                ws.Column(c + 1).Width = widths[c];
            }
        }
        private static void ApplyDataRowStyle(IXLWorksheet ws, int row, int colCount)
        {
            var range = ws.Range(row, 1, row, colCount);
            range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Font.FontSize = 10;
            if (row % 2 == 0)
                range.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FB");
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        private static void BuildSheet(XLWorkbook wb, List<MN_ChucVuExportModel> data)
        {
            var ws = wb.Worksheets.Add("Danh sach chuc vu");
            var headers = new[]
            {
                "STT", "Mã chức vụ", "Tên chức vụ", "Trạng thái"
            };
            var colWidths = new[] { 5, 15, 40, 18 };
            ApplyHeader(ws, headers, colWidths);

            if (data == null || !data.Any()) return;
            for (int i = 0; i < data.Count; i++)
            {
                var r = data[i];
                int row = i + 2;
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = r.MaChucVu;
                ws.Cell(row, 3).Value = r.TenChucVu;
                ws.Cell(row, 4).Value = r.TrangThai;
                ApplyDataRowStyle(ws, row, headers.Length);
            }
            ws.SheetView.FreezeRows(1);
        }
    }
}
