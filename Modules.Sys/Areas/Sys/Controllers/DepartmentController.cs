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
    public class DepartmentController : AppController
    {
        private readonly string _departmentTitle = AppProcessor.Messagor.GetMessage("Department_Title") ?? "Phòng ban";
        private readonly MN_BoPhanCache _departmentCache = new MN_BoPhanCache();

        public DepartmentController()
        {
            _departmentCache = new MN_BoPhanCache();
        }

        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        #region Department - CRUD

        private List<SelectListItem> GetDepartmentSelectList(int? currentId = null)
        {
            var list = _departmentCache.GetAll();
            var items = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "0",
                    Text = AppProcessor.Messagor.GetMessage("Department_Parent_Select_Option") ?? "-- Không chọn (Cấp cao nhất) --"
                }
            };
            if (list != null)
            {
                foreach (var item in list)
                {
                    if (currentId.HasValue && item.BoPhan_ID == currentId.Value) continue;
                    var prefix = item.Level > 0 ? new string('-', item.Level * 2) + " " : "";
                    items.Add(new SelectListItem
                    {
                        Value = item.BoPhan_ID.ToString(),
                        Text = prefix + item.TenBoPhan
                    });
                }
            }
            return items;
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add()
        {
            var model = new MN_BoPhanModel
            {
                ListBoPhanCha = GetDepartmentSelectList()
            };
            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(MN_BoPhanModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ListBoPhanCha = GetDepartmentSelectList();
                return PartialView("_Department", model);
            }
            string response;
            var result = _departmentCache.Save(model, User.UserName);
            if (result == 0)
                response = CreateMessage($"{_departmentTitle} [{model.TenBoPhan}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_departmentTitle} [{model.MaBoPhan}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_departmentTitle} [{model.TenBoPhan}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = result > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Edit)]
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var model = _departmentCache.GetById(id);
            if (model == null)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage($"{_departmentTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            }
            model.ListBoPhanCha = GetDepartmentSelectList(id);
            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(MN_BoPhanModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ListBoPhanCha = GetDepartmentSelectList(model.BoPhan_ID);
                return PartialView("_Department", model);
            }
            string response;
            var result = _departmentCache.Save(model, User.UserName);
            if (result == 0)
                response = CreateMessage($"{_departmentTitle} [{model.TenBoPhan}]", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_departmentTitle} [{model.MaBoPhan}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_departmentTitle} [{model.TenBoPhan}]", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = result > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _departmentCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_departmentTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            ViewBag.ConfirmStatus = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete") ?? "Bạn muốn xoá {0}?",
                $"{_departmentTitle} [{model.TenBoPhan}]");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(MN_BoPhanModel model)
        {
            var deleted = _departmentCache.Delete(model, User.UserName);
            var response = CreateMessage($"{_departmentTitle} [{model.TenBoPhan}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = deleted > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(string search)
        {
            var data = _departmentCache.Get(search);

            return Json(new
            {
                data = data
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Export(string keyword, string cookieName = null)
        {
            var search = new MN_BoPhanSearchModel
            {
                Keyword = keyword,
            };
            var departments = _departmentCache.Export(search);

            var ms = new MemoryStream();
            using (var wb = new XLWorkbook())
            {
                BuildSheet(wb, departments);
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
            var fileName = $"DanhSachBoPhan_{DateTime.Now:ddMMyyyyHHmmss}.xlsx";
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
        private static void BuildSheet(XLWorkbook wb, List<MN_BoPhanExportModel> data)
        {
            var ws = wb.Worksheets.Add("Danh sach bo phan");
            var headers = new[]
            {
                "STT", "Mã bộ phận", "Tên bộ phận", "Bộ phận cha", "Cấp quản lý", "Trạng thái"
            };
            var colWidths = new[] { 5, 10, 35, 35, 16, 18};
            ApplyHeader(ws, headers, colWidths);

            if (data == null || !data.Any()) return;
            for (int i = 0; i < data.Count; i++)
            {
                var r = data[i];
                int row = i + 2;
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = r.MaBoPhan;
                ws.Cell(row, 3).Value = r.TenBoPhan;
                ws.Cell(row, 4).Value = r.TenBoPhanCha;
                ws.Cell(row, 5).Value = r.CapQuanLy;
                ws.Cell(row, 6).Value = r.TrangThai;
                ApplyDataRowStyle(ws, row, headers.Length);
            }
            ws.SheetView.FreezeRows(1);
        }

    }
}
