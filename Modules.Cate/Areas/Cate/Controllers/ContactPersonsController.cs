using ClosedXML.Excel;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Processors;

namespace Modules.Cate.Areas.Cate.Controllers
{
    /// <summary>
    /// Xử lý các luồng thêm, sửa, xóa, tra cứu và xuất danh sách người liên hệ.
    /// </summary>
    public class ContactPersonsController : AppController
    {
        #region Fields & Constructor

        private readonly RM_ContactPersonsCache _contactPersonsCache;
        private readonly RM_CustomerCache _customerCache;
        private readonly RM_CustomerContactCache _customerContactCache;
        private readonly string _Title = AppProcessor.Messagor.GetMessage("ContactPersons_Title");
        private readonly string _activeStatusText = AppProcessor.Messagor.GetMessage("ContactPersons_Message_StatusActive");
        private readonly string _inactiveStatusText = AppProcessor.Messagor.GetMessage("ContactPersons_Message_StatusInactive");

        /// <summary>
        /// Khởi tạo cache phục vụ xử lý người liên hệ.
        /// </summary>
        public ContactPersonsController()
        {
            _contactPersonsCache = new RM_ContactPersonsCache();
            _customerCache = new RM_CustomerCache();
            _customerContactCache = new RM_CustomerContactCache();
        }

        /// <summary>
        /// Khởi tạo danh sách giới tính phục vụ hiển thị bộ lọc và form nhập liệu.
        /// </summary>
        /// <returns>Danh sách giới tính.</returns>
        public static List<SelectListItem> GetListGender()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = AppProcessor.Messagor.GetMessage("ContactPersonal_Male_Label") },
                new SelectListItem { Value = "0", Text = AppProcessor.Messagor.GetMessage("ContactPersonal_Female_Label") },
                new SelectListItem { Value = "2", Text = AppProcessor.Messagor.GetMessage("ContactPersonal_Other_Label") }
            };
        }

        #endregion

        #region ContactPersons - CRUD

        /// <summary>
        /// Hiển thị màn hình danh sách người liên hệ và khởi tạo bộ lọc tìm kiếm.
        /// </summary>
        /// <returns>Màn hình danh sách người liên hệ.</returns>
        public ActionResult Index()
        {
            var model = new RM_ContactPersonsSearchModel
            {
                ListGender = GetListGender(),
                ListStatus = new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = _activeStatusText },
                    new SelectListItem { Value = "0", Text = _inactiveStatusText }
                },
                ListCustomer = _customerCache.GetAll()
                    .Select(x => new SelectListItem
                    {
                        Value = x.CustomerID.ToString(),
                        Text = x.CustomerName
                    }).ToList()
            };
            return View(model);
        }

        /// <summary>
        /// Thống kê chỉ số KPI người liên hệ: Tổng số, Đang hoạt động, Ngừng hoạt động, Khách hàng liên kết.
        /// </summary>
        /// <returns>Dữ liệu JSON thống kê KPI.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetSummary()
        {
            var all = _contactPersonsCache.GetAll();
            int total = all?.Count ?? 0;
            int active = all?.Count(x => x.Status == 1) ?? 0;
            int inactive = total - active;
            int totalCustomers = _customerCache.GetAll()?.Count ?? 0;

            return Json(new
            {
                total,
                active,
                inactive,
                totalCustomers
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Trả danh sách người liên hệ theo điều kiện tìm kiếm và phân trang.
        /// </summary>
        /// <param name="filterModel">Điều kiện tìm kiếm người liên hệ.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(RM_ContactPersonsSearchModel filterModel)
        {
            var drawStr = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startStr = Request.Form.GetValues("start")?[0];
            var lengthStr = Request.Form.GetValues("length")?[0];

            int.TryParse(drawStr, out int draw);
            int.TryParse(startStr, out int start);
            int.TryParse(lengthStr, out int length);

            var search = new RM_ContactPersonsSearchModel
            {
                Keyword = filterModel.Keyword,
                Gender = filterModel.Gender,
                Status = filterModel.Status,
                CustomerID = filterModel.CustomerID,
                Search = filterModel.Keyword,
                Order = order,
                OrderDir = orderDir,
                StartIndex = start,
                PageSize = length
            };

            var data = _contactPersonsCache.Get(out var total, search);
            return Json(new { draw, recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình thêm mới người liên hệ.
        /// </summary>
        /// <returns>Popup thêm mới người liên hệ.</returns>
        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add()
        {
            var model = new RM_ContactPersonsModel
            {
                Gender = null,
                ListGender = GetListGender(),
                Status = 1
            };
            return PartialView("_Add", model);
        }

        /// <summary>
        /// Lưu thông tin người liên hệ mới từ màn hình.
        /// </summary>
        /// <param name="model">Dữ liệu người liên hệ cần lưu.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(RM_ContactPersonsModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ListGender = GetListGender();
                return PartialView("_ContactPersons", model);
            }

            var result = _contactPersonsCache.Save(model, User.UserName);
            string response;
            if (result == 0) response = CreateMessage($"{_Title} [{model.FullName}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9) response = CreateMessage($"{_Title} [{model.FullName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else response = CreateMessage($"{_Title} [{model.FullName}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình cập nhật người liên hệ theo mã dữ liệu được chọn.
        /// </summary>
        /// <param name="id">Mã người liên hệ cần cập nhật.</param>
        /// <returns>Popup cập nhật người liên hệ hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id)
        {
            var model = _contactPersonsCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage(_Title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            model.ListGender = GetListGender();
            return PartialView("_Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin người liên hệ theo dữ liệu từ màn hình.
        /// </summary>
        /// <param name="model">Dữ liệu người liên hệ cần cập nhật.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(RM_ContactPersonsModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ListGender = GetListGender();
                return PartialView("_ContactPersons", model);
            }

            var result = _contactPersonsCache.Save(model, User.UserName);
            string response;
            if (result == 0) response = CreateMessage($"{_Title} [{model.FullName}]", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9) response = CreateMessage($"{_Title} [{model.FullName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else response = CreateMessage($"{_Title} [{model.FullName}]", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình xác nhận xóa người liên hệ theo mã dữ liệu được chọn.
        /// </summary>
        /// <param name="id">Mã người liên hệ cần xóa.</param>
        /// <returns>Popup xác nhận xóa hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _contactPersonsCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage(_Title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            ViewBag.ConfirmMessage = string.Format(
                AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_Title} [{model.FullName}]");
            return PartialView("_Delete", model);
        }

        /// <summary>
        /// Xóa thông tin người liên hệ theo dữ liệu được chọn.
        /// </summary>
        /// <param name="model">Dữ liệu người liên hệ cần xóa.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(RM_ContactPersonsModel model)
        {
            var deleted = _contactPersonsCache.Delete(model, User.UserName);

            EnumProcessType processType;
            EnumMsgIcon msgIcon;

            if (deleted > 0)
            {
                processType = EnumProcessType.Delete;
                msgIcon = EnumMsgIcon.Success;
            }
            else if (deleted == 0)
            {
                processType = EnumProcessType.DataUsed;
                msgIcon = EnumMsgIcon.Error;
            }
            else
            {
                processType = EnumProcessType.Delete;
                msgIcon = EnumMsgIcon.Error;
            }

            var response = CreateMessage($"{_Title} [{model.FullName}]", processType, msgIcon);

            return Json(new { status = true, message = response });
        }

        #endregion

        #region ContactPersons - Export

        /// <summary>
        /// Xuất danh sách người liên hệ ra file Excel theo điều kiện lọc.
        /// Không dùng [AjaxOnly] vì gọi qua window.location.href.
        /// </summary>
        [HttpGet]
        public ActionResult Export(string keyword, int? gender, int? status, int? customerID = null, string cookieName = null)
        {
            List<RM_ContactPersonsModel> data = GetExportData(keyword, gender, status, customerID);
            byte[] fileBytes = BuildExportWorkbook(data);

            return SendExcelFile(fileBytes, cookieName);
        }

        /// <summary>
        /// Bước 1: Truy vấn dữ liệu theo điều kiện lọc.
        /// </summary>
        private List<RM_ContactPersonsModel> GetExportData(string keyword, int? gender, int? status, int? customerID = null)
        {
            RM_ContactPersonsSearchModel search = new RM_ContactPersonsSearchModel
            {
                Keyword = keyword,
                Gender = gender,
                Status = status,
                CustomerID = customerID,
                Search = keyword,
                Order = "0",
                OrderDir = "ASC",
                StartIndex = 0,
                PageSize = -1
            };

            return _contactPersonsCache.Get(out _, search);
        }

        /// <summary>
        /// Bước 2: Tạo file Excel từ dữ liệu.
        /// </summary>
        private byte[] BuildExportWorkbook(List<RM_ContactPersonsModel> data)
        {
            byte[] result;

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Nguoi lien he");

                WriteExportHeader(worksheet);
                WriteExportData(worksheet, data);
                worksheet.View.FreezePanes(2, 1);

                using (MemoryStream stream = new MemoryStream())
                {
                    package.SaveAs(stream);
                    result = stream.ToArray();
                }
            }

            return result;
        }

        /// <summary>
        /// Viết dòng tiêu đề cột.
        /// Thêm/bớt cột: sửa headers, colWidths và WriteExportData tương ứng.
        /// </summary>
        private static Color GetColor(string htmlColor)
        {
            return ColorTranslator.FromHtml(htmlColor);
        }

        private void WriteExportHeader(ExcelWorksheet worksheet)
        {
            string[] headers = new[]
            {
                "STT", "Mã NLH", "Họ tên", "Giới tính", "Chức vụ", "Đơn vị công tác",
                "Điện thoại", "Di động", "Email", "Zalo", "Địa chỉ",
                "Ngày sinh", "Khách hàng", "Trạng thái", "Ghi chú"
            };

            int[] colWidths = new[] { 5, 14, 25, 12, 20, 30, 18, 18, 28, 15, 30, 14, 30, 16, 30 };

            worksheet.Row(1).Height = 22;

            for (int colIndex = 0; colIndex < headers.Length; colIndex++)
            {
                ExcelRange cell = worksheet.Cells[1, colIndex + 1];
                cell.Value = headers[colIndex];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 11;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(GetColor("#1F4E79"));
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Column(colIndex + 1).Width = colWidths[colIndex];
            }
        }

        /// <summary>
        /// Viết từng dòng dữ liệu vào worksheet.
        /// </summary>
        private void WriteExportData(ExcelWorksheet worksheet, List<RM_ContactPersonsModel> data)
        {
            if (data == null || !data.Any())
            {
                return;
            }

            List<SelectListItem> genders = GetListGender();
            int stt = 1;

            for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
            {
                RM_ContactPersonsModel item = data[rowIndex];
                int excelRow = rowIndex + 2;

                string genderName = item.Gender.HasValue
                    ? genders.FirstOrDefault(g => g.Value == item.Gender.Value.ToString())?.Text ?? ""
                    : "";

                worksheet.Cells[excelRow, 1].Value = stt++;
                worksheet.Cells[excelRow, 2].Value = item.CodePerson ?? "";
                worksheet.Cells[excelRow, 3].Value = item.FullName ?? "";
                worksheet.Cells[excelRow, 4].Value = genderName;
                worksheet.Cells[excelRow, 5].Value = item.Position ?? "";
                worksheet.Cells[excelRow, 6].Value = item.WorkUnit ?? "";
                worksheet.Cells[excelRow, 7].Value = item.Phone ?? "";
                worksheet.Cells[excelRow, 8].Value = item.Mobile ?? "";
                worksheet.Cells[excelRow, 9].Value = item.Email ?? "";
                worksheet.Cells[excelRow, 10].Value = item.Zalo ?? "";
                worksheet.Cells[excelRow, 11].Value = item.Address ?? "";

                if (item.Birthday.HasValue)
                {
                    worksheet.Cells[excelRow, 12].Value = item.Birthday.Value.ToString("dd/MM/yyyy");
                }

                if (!string.IsNullOrEmpty(item.CustomerName))
                {
                    worksheet.Cells[excelRow, 13].Value = FormatCustomerName(item.CustomerName);
                    worksheet.Cells[excelRow, 13].Style.WrapText = true;
                }

                worksheet.Cells[excelRow, 14].Value = item.Status == 1 ? _activeStatusText : _inactiveStatusText;
                worksheet.Cells[excelRow, 15].Value = item.Note ?? "";

                ApplyExportRowStyle(worksheet, excelRow, headers: 15);
            }
        }

        /// <summary>
        /// Làm sạch chuỗi tên khách hàng
        /// tách từng khách hàng xuống dòng để dễ đọc trong Excel.
        /// </summary>
        private string FormatCustomerName(string rawCustomerName)
        {
            if (string.IsNullOrEmpty(rawCustomerName))
            {
                return "";
            }

            // Thay thế nhiều dấu ":" liên tiếp thành dấu phân cách tạm
            string cleaned = System.Text.RegularExpressions.Regex
                .Replace(rawCustomerName, @":+", " ");

            // Tách theo ", " rồi trim từng phần tử
            string[] parts = cleaned
                .Split(new[] { "|", "," }, StringSplitOptions.RemoveEmptyEntries);

            IEnumerable<string> lines = parts
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p));

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Tạo khóa đối chiếu dùng để nhận diện dòng import người liên hệ bị trùng theo họ tên và số điện thoại.
        /// </summary>
        /// <param name="fullName">Họ và tên người liên hệ.</param>
        /// <param name="phone">Số điện thoại người liên hệ.</param>
        /// <returns>Khóa chuẩn hóa phục vụ đối chiếu dữ liệu import.</returns>
        private string BuildImportDuplicateKey(string fullName, string phone)
        {
            string normalizedFullName = (fullName ?? string.Empty).Trim().ToUpperInvariant();
            string normalizedPhone = (phone ?? string.Empty).Trim().ToUpperInvariant();

            return string.Format("{0}|{1}", normalizedFullName, normalizedPhone);
        }

        /// <summary>
        /// Áp dụng style cho từng dòng dữ liệu: border, font, màu nền xen kẽ, căn lề.
        /// Sửa tại đây để thay đổi style toàn bộ file export.
        /// </summary>
        private void ApplyExportRowStyle(ExcelWorksheet worksheet, int excelRow, int headers)
        {
            ExcelRange rowRange = worksheet.Cells[excelRow, 1, excelRow, headers];
            rowRange.Style.Font.Size = 10;
            rowRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            rowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            rowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            rowRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            rowRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            if (excelRow % 2 == 0)
            {
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(GetColor("#EBF3FB"));
            }

            worksheet.Cells[excelRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[excelRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        /// <summary>
        /// Bước 3: Ghi response trả file về browser.
        /// Dùng byte[] + BinaryWrite thay vì FileStreamResult(MemoryStream) để tránh stream bị dispose sớm.
        /// </summary>
        private ActionResult SendExcelFile(byte[] fileBytes, string cookieName)
        {
            if (!string.IsNullOrEmpty(cookieName))
            {
                Response.Cookies.Add(new HttpCookie(cookieName, "done")
                {
                    Path = "/",
                    Expires = DateTime.Now.AddMinutes(1)
                });
            }

            string fileName = "NguoiLienHe_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx";

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            Response.AddHeader("Content-Length", fileBytes.Length.ToString());
            Response.BinaryWrite(fileBytes);
            Response.Flush();
            Response.End();

            return new EmptyResult();
        }

        #endregion

        #region ContactPersons - Import

        private static string GetImportCellText(ExcelWorksheet worksheet, int rowIndex, int colIndex)
        {
            object value = worksheet.Cells[rowIndex, colIndex].Value;

            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue.ToString("dd/MM/yyyy");
            }

            return Convert.ToString(value)?.Trim() ?? string.Empty;
        }

        [AjaxOnly]
        /// <summary>
        /// Hiển thị màn hình import người liên hệ.
        /// </summary>
        /// <returns>Popup import người liên hệ.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Import()
        {
            return PartialView("_Import");
        }

        /// <summary>
        /// Đọc file import người liên hệ và trả kết quả kiểm tra dữ liệu.
        /// </summary>
        /// <param name="importFile">Tập tin import từ màn hình.</param>
        /// <returns>Kết quả kiểm tra dữ liệu import.</returns>
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ImportPreview(HttpPostedFileBase importFile)
        {
            if (importFile == null || importFile.ContentLength == 0)
            {
                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportFileRequired")
                });
            }

            string ext = System.IO.Path.GetExtension(importFile.FileName)?.ToLower();

            if (ext != ".xlsx")
            {
                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportOnlyXlsx")
                });
            }

            var customers = _customerCache.GetAll();
            var genders = GetListGender();

            // allValidRows: lưu toàn bộ vào Session để import
            // allErrorRows: lưu toàn bộ vào Session để export
            // previewValidRows: chỉ lấy 50 dòng đầu để hiển thị FE
            // previewErrorRows: chỉ lấy 50 dòng đầu để hiển thị FE
            List<RM_ContactPersonsImportRowModel> allValidRows = new List<RM_ContactPersonsImportRowModel>();
            List<RM_ContactPersonsImportRowModel> allErrorRows = new List<RM_ContactPersonsImportRowModel>();
            List<RM_ContactPersonsImportRowModel> previewValidRows = new List<RM_ContactPersonsImportRowModel>();
            List<RM_ContactPersonsImportRowModel> previewErrorRows = new List<RM_ContactPersonsImportRowModel>();

            try
            {
                using (ExcelPackage package = new ExcelPackage(importFile.InputStream))
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets.FirstOrDefault();

                    if (ws == null || ws.Dimension == null)
                    {
                        return Json(new
                        {
                            status = false,
                            message = AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportFileEmpty")
                        });
                    }

                    int lastRow = ws.Dimension.End.Row;

                    for (int r = 2; r <= lastRow; r++)
                    {
                        RM_ContactPersonsImportRowModel row = new RM_ContactPersonsImportRowModel
                        {
                            RowNumber = r
                        };

                        row.FullName = GetImportCellText(ws, r, 1);
                        string genderStr = GetImportCellText(ws, r, 2);
                        row.Position = GetImportCellText(ws, r, 3);
                        row.WorkUnit = GetImportCellText(ws, r, 4);
                        row.Phone = GetImportCellText(ws, r, 5);
                        row.Mobile = GetImportCellText(ws, r, 6);
                        row.Email = GetImportCellText(ws, r, 7);
                        row.Zalo = GetImportCellText(ws, r, 8);
                        row.Address = GetImportCellText(ws, r, 9);
                        string birthdayStr = GetImportCellText(ws, r, 10);
                        string shortNameInput = GetImportCellText(ws, r, 11);
                        row.Note = GetImportCellText(ws, r, 12);

                        if (string.IsNullOrWhiteSpace(row.FullName) && string.IsNullOrWhiteSpace(row.Phone))
                        {
                            continue;
                        }

                        List<string> errors = new List<string>();

                        if (string.IsNullOrWhiteSpace(row.FullName))
                            errors.Add(AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportFullNameRequired"));

                        if (!string.IsNullOrWhiteSpace(shortNameInput))
                        {
                            dynamic cus = customers.FirstOrDefault(x =>
                                string.Equals(x.ShortName, shortNameInput, StringComparison.OrdinalIgnoreCase));

                            if (cus != null)
                            {
                                row.CustomerShortName = cus.ShortName;
                                row.CustomerName = cus.CustomerName;
                            }
                            else
                                errors.Add(string.Format(
                                    AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportCustomerShortNameNotFound"),
                                    shortNameInput));
                        }

                        if (!string.IsNullOrEmpty(genderStr))
                        {
                            dynamic g = genders.FirstOrDefault(x =>
                                string.Equals(x.Text, genderStr, StringComparison.OrdinalIgnoreCase));

                            if (g != null)
                            {
                                row.Gender = int.Parse(g.Value);
                                row.GenderName = g.Text;
                            }
                            else
                                errors.Add(string.Format(
                                    AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportGenderInvalid"),
                                    genderStr));
                        }

                        if (!string.IsNullOrEmpty(birthdayStr))
                        {
                            if (DateTime.TryParseExact(
                                    birthdayStr,
                                    new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.None,
                                    out DateTime bd))
                                row.Birthday = bd;
                            else
                                errors.Add(string.Format(
                                    AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportBirthdayInvalid"),
                                    birthdayStr));
                        }

                        if (!string.IsNullOrEmpty(row.Email) &&
                            !System.Text.RegularExpressions.Regex.IsMatch(
                                row.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            errors.Add(AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportEmailInvalid"));

                        row.Errors = errors;
                        row.Status = 1;

                        if (errors.Count > 0)
                        {
                            allErrorRows.Add(row);

                            if (previewErrorRows.Count < 50)
                            {
                                previewErrorRows.Add(row);
                            }
                        }
                        else
                        {
                            allValidRows.Add(row);

                            if (previewValidRows.Count < 50)
                            {
                                previewValidRows.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);

                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportReadFailed")
                });
            }

            // Lưu toàn bộ vào Session để import và export
            Session["ImportContactPersonsData"] = allValidRows;
            Session["ImportContactPersonsErrorData"] = allErrorRows;

            return Json(new
            {
                status = true,
                totalValid = allValidRows.Count,
                totalError = allErrorRows.Count,
                validRows = previewValidRows,
                errorRows = previewErrorRows,
                hasMoreValid = allValidRows.Count > 50,
                hasMoreErrors = allErrorRows.Count > 50
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Xác nhận import người liên hệ hợp lệ vào hệ thống.
        /// </summary>
        /// <returns>Kết quả xử lý import.</returns>
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ImportConfirm()
        {
            var rows = Session["ImportContactPersonsData"] as List<RM_ContactPersonsImportRowModel>;

            if (rows == null || !rows.Any())
            {
                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportNoDataToImport")
                });
            }

            var result = _contactPersonsCache.BulkImport(rows, User.UserName);
            var customers = _customerCache.GetAll();
            var contactPersons = _contactPersonsCache.GetAll();

            var duplicateKeys = new HashSet<string>(
                result.DuplicateRows.Select(d => BuildImportDuplicateKey(d.FullName, d.Phone)),
                StringComparer.OrdinalIgnoreCase);

            foreach (RM_ContactPersonsImportRowModel row in rows.Where(
                r => !duplicateKeys.Contains(BuildImportDuplicateKey(r.FullName, r.Phone))))
            {
                if (string.IsNullOrEmpty(row.CustomerShortName))
                {
                    continue;
                }

                dynamic cus = customers.FirstOrDefault(x =>
                    string.Equals(x.ShortName, row.CustomerShortName, StringComparison.OrdinalIgnoreCase));

                if (cus == null)
                {
                    continue;
                }

                dynamic cp = contactPersons.FirstOrDefault(x =>
                    x.IsDeleted == false
                    && string.Equals(x.FullName, row.FullName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Phone ?? string.Empty, row.Phone ?? string.Empty, StringComparison.OrdinalIgnoreCase));

                if (cp == null && !string.IsNullOrWhiteSpace(row.Email))
                {
                    cp = contactPersons.FirstOrDefault(x =>
                        x.IsDeleted == false
                        && string.Equals(x.FullName, row.FullName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.Email ?? string.Empty, row.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase));
                }

                if (cp == null)
                {
                    continue;
                }

                RM_CustomerContactModel link = new RM_CustomerContactModel
                {
                    CustomerID = cus.CustomerID,
                    ContactPersonID = cp.ContactPerson_ID,
                    Position = row.Position,
                    Note = row.Note,
                    IsDeleted = false
                };
                _customerContactCache.Save(link, User.UserName);
            }

            Session["ImportContactPersonsData"] = null;
            Session["ImportContactPersonsDuplicateData"] = result.DuplicateRows;

            string message = result.FailCount > 0
                ? string.Format(
                    AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportCompletedWithDuplicate"),
                    result.SuccessCount,
                    result.FailCount)
                : string.Format(
                    AppProcessor.Messagor.GetMessage("ContactPersons_Message_ImportCompleted"),
                    result.SuccessCount);

            return Json(new
            {
                successCount = result.SuccessCount,
                failCount = result.FailCount,
                duplicateRows = result.DuplicateRows,
                message
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Xuất danh sách dòng lỗi của file import người liên hệ ra Excel.
        /// </summary>
        /// <param name="cookieName">Tên cookie dùng để báo hoàn tất tải file.</param>
        /// <returns>File Excel danh sách dòng lỗi.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportErrorRows(string cookieName = null)
        {
            List<RM_ContactPersonsImportRowModel> errorRows =
                Session["ImportContactPersonsErrorData"] as List<RM_ContactPersonsImportRowModel>;

            MemoryStream ms = new MemoryStream();

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Dong_Loi");

                if (errorRows == null || !errorRows.Any())
                {
                    ws.Cells[1, 1].Value = AppProcessor.Messagor.GetMessage("ContactPersons_Message_NoDataR");
                    package.SaveAs(ms);
                }
                else
                {
                    string[] headers = new[]
                    {
                "Dòng Excel",
                "Họ và tên (*)", "Giới tính (Nam/Nữ/Khác)", "Chức vụ",
                "Đơn vị công tác", "Điện thoại", "Di động",
                "Email", "Zalo", "Địa chỉ",
                "Ngày sinh (dd/MM/yyyy)", "Mã khách hàng", "Ghi chú",
                "Lý do lỗi (không nhập cột này)"
            };

                    int[] colWidths = new[] { 9, 25, 18, 20, 30, 18, 18, 28, 15, 30, 22, 20, 25, 55 };

                    for (int c = 0; c < headers.Length; c++)
                    {
                        ExcelRange cell = ws.Cells[1, c + 1];
                        cell.Value = headers[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.Size = 11;
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(c == headers.Length - 1
                            ? GetColor("#C00000")
                            : GetColor("#1F4E79"));
                        cell.Style.Font.Color.SetColor(Color.White);
                        ws.Column(c + 1).Width = colWidths[c];
                    }

                    ws.Row(1).Height = 22;

                    for (int r = 0; r < errorRows.Count; r++)
                    {
                        RM_ContactPersonsImportRowModel row = errorRows[r];
                        int excelRow = r + 2;

                        ws.Cells[excelRow, 1].Value = row.RowNumber;
                        ws.Cells[excelRow, 2].Value = row.FullName ?? "";
                        ws.Cells[excelRow, 3].Value = row.GenderName ?? "";
                        ws.Cells[excelRow, 4].Value = row.Position ?? "";
                        ws.Cells[excelRow, 5].Value = row.WorkUnit ?? "";
                        ws.Cells[excelRow, 6].Value = row.Phone ?? "";
                        ws.Cells[excelRow, 7].Value = row.Mobile ?? "";
                        ws.Cells[excelRow, 8].Value = row.Email ?? "";
                        ws.Cells[excelRow, 9].Value = row.Zalo ?? "";
                        ws.Cells[excelRow, 10].Value = row.Address ?? "";
                        ws.Cells[excelRow, 11].Value = row.Birthday.HasValue
                            ? row.Birthday.Value.ToString("dd/MM/yyyy") : "";
                        ws.Cells[excelRow, 12].Value = row.CustomerShortName ?? "";
                        ws.Cells[excelRow, 13].Value = row.Note ?? "";

                        string errorText = row.Errors != null && row.Errors.Any()
                            ? string.Join("\n", row.Errors) : "";
                        ExcelRange errorCell = ws.Cells[excelRow, 14];
                        errorCell.Value = errorText;
                        errorCell.Style.Font.Color.SetColor(Color.DarkRed);
                        errorCell.Style.WrapText = true;

                        ExcelRange rowRange = ws.Cells[excelRow, 1, excelRow, headers.Length];
                        rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        rowRange.Style.Fill.BackgroundColor.SetColor(GetColor("#FFF2CC"));
                        rowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Font.Size = 10;
                    }

                    ws.View.FreezePanes(2, 1);
                    package.SaveAs(ms);
                }
            }

            if (!string.IsNullOrEmpty(cookieName))
            {
                Response.Cookies.Add(new HttpCookie(cookieName, "done")
                {
                    Path = "/",
                    Expires = DateTime.Now.AddMinutes(1)
                });
            }

            ms.Seek(0, SeekOrigin.Begin);

            return new FileStreamResult(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"DongLoi_Import_NguoiLienHe_{DateTime.Now:ddMMyyyyHHmmss}.xlsx"
            };
        }

        /// <summary>
        /// Xuất danh sách dòng trùng của file import người liên hệ ra Excel.
        /// </summary>
        /// <param name="cookieName">Tên cookie dùng để báo hoàn tất tải file.</param>
        /// <returns>File Excel danh sách dòng trùng.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportDuplicateRows(string cookieName = null)
        {
            List<RM_ContactPersonsDuplicateModel> duplicateRows =
                Session["ImportContactPersonsDuplicateData"] as List<RM_ContactPersonsDuplicateModel>;

            MemoryStream ms = new MemoryStream();

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Dong_Trung");

                string[] headers = new[] { "STT", "Họ và tên", "Điện thoại", "Lý do" };
                int[] colWidths = new[] { 6, 30, 18, 45 };

                for (int c = 0; c < headers.Length; c++)
                {
                    ExcelRange cell = ws.Cells[1, c + 1];
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Size = 11;
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(GetColor("#C00000"));
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    ws.Column(c + 1).Width = colWidths[c];
                }

                ws.Row(1).Height = 22;

                if (duplicateRows != null && duplicateRows.Any())
                {
                    for (int r = 0; r < duplicateRows.Count; r++)
                    {
                        RM_ContactPersonsDuplicateModel row = duplicateRows[r];
                        int excelRow = r + 2;

                        ws.Cells[excelRow, 1].Value = r + 1;
                        ws.Cells[excelRow, 2].Value = row.FullName ?? "";
                        ws.Cells[excelRow, 3].Value = row.Phone ?? "";
                        ws.Cells[excelRow, 4].Value = row.Reason ?? "";

                        ws.Cells[excelRow, 4].Style.Font.Color.SetColor(Color.DarkRed);

                        ExcelRange rowRange = ws.Cells[excelRow, 1, excelRow, headers.Length];
                        rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        rowRange.Style.Fill.BackgroundColor.SetColor(GetColor("#FFF2CC"));
                        rowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        rowRange.Style.Font.Size = 10;
                    }
                }

                ws.View.FreezePanes(2, 1);
                package.SaveAs(ms);
            }

            if (!string.IsNullOrEmpty(cookieName))
            {
                Response.Cookies.Add(new HttpCookie(cookieName, "done")
                {
                    Path = "/",
                    Expires = DateTime.Now.AddMinutes(1)
                });
            }

            ms.Seek(0, SeekOrigin.Begin);

            return new FileStreamResult(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"DongTrung_Import_NguoiLienHe_{DateTime.Now:ddMMyyyyHHmmss}.xlsx"
            };
        }

        /// <summary>
        /// Tải file mẫu import người liên hệ.
        /// </summary>
        /// <returns>File Excel mẫu import người liên hệ.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult DownloadImportTemplate()
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Template_NguoiLienHe");

                string[] headers = new[]
                {
            "Họ và tên (*)", "Giới tính (Nam/Nữ/Khác)", "Chức vụ",
            "Đơn vị công tác", "Điện thoại", "Di động",
            "Email", "Zalo", "Địa chỉ",
            "Ngày sinh (dd/MM/yyyy)", "Mã khách hàng", "Ghi chú"
        };

                for (int c = 0; c < headers.Length; c++)
                {
                    ExcelRange cell = ws.Cells[1, c + 1];
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(GetColor("#1F4E79"));
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                ws.Row(1).Height = 20;
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                MemoryStream ms = new MemoryStream();
                package.SaveAs(ms);
                ms.Seek(0, SeekOrigin.Begin);

                return new FileStreamResult(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    FileDownloadName = "Template_Import_NguoiLienHe.xlsx"
                };
            }
        }

        #endregion
    }
}
