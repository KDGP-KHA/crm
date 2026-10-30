using ClosedXML.Excel;
using Core.Cate.Biz;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.RM.Models;
using Core.Sys.BaseApp;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.Hosting;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Helpers;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Utils;

namespace Modules.Cate.Areas.Cate.Controllers
{

    public class CustomerController : AppController
    {
        #region Fields & Constructor

        private readonly RM_CustomerCache _customerCache;
        private readonly RM_BusinessOpportunityCache _businessCache;
        private readonly Cate_ProductServiceCache _productCache;
        private readonly MN_EmployeeCache _employeesCache;
        private readonly Cate_ProductServiceCache _productServiceCache;
        private readonly RM_OpportunityStatusCache _opportunityStatusCache;
        private readonly RM_CustomerTypeCache _customerTypeCache;
        private readonly RM_CustomerContactCache _customerContactCache;
        private readonly RM_StatusCache _statusCache;
        private readonly RM_CustomerAnniversaryCache _anniversaryCache;
        private readonly RM_CustomerAnniversaryBiz _anniversaryBiz;
        private readonly RM_BusinessOpportunityFilePathCache _businessOpportunityFilePathCache;
        private readonly string _CustomerTitle = AppProcessor.Messagor.GetMessage("Customer_Title");
        private readonly string _MSTLabel = AppProcessor.Messagor.GetMessage("Customer_TaxCode_Label");
        private readonly string _CusNameLabel = AppProcessor.Messagor.GetMessage("Customer_CustomerName_Label");
        private readonly string _CHKDTitle = AppProcessor.Messagor.GetMessage("BusinessOpportunity_Title");
        private readonly string _AnniversaryTitle = AppProcessor.Messagor.GetMessage("Anniversary_Title");
        private readonly string _folderImage = ConfigurationManager.AppSettings["AppImageRoot_Path"] ?? "/Contents/imgs";

        /// <summary>
        /// Khởi tạo cache và biz phục vụ xử lý khách hàng, cơ hội kinh doanh và ngày kỷ niệm.
        /// </summary>
        public CustomerController()
        {
            _customerCache = new RM_CustomerCache();
            _businessCache = new RM_BusinessOpportunityCache();
            _productCache = new Cate_ProductServiceCache();
            _employeesCache = new MN_EmployeeCache();
            _productServiceCache = new Cate_ProductServiceCache();
            _opportunityStatusCache = new RM_OpportunityStatusCache();
            _customerTypeCache = new RM_CustomerTypeCache();
            _statusCache = new RM_StatusCache();
            _anniversaryCache = new RM_CustomerAnniversaryCache();
            _anniversaryBiz = new RM_CustomerAnniversaryBiz();
            _customerContactCache = new RM_CustomerContactCache();
            _businessOpportunityFilePathCache = new RM_BusinessOpportunityFilePathCache();
        }

        #endregion

        #region Customer - CRUD

        /// <summary>
        /// Hiển thị màn hình danh sách khách hàng và khởi tạo bộ lọc tìm kiếm.
        /// </summary>
        /// <returns>Màn hình danh sách khách hàng.</returns>
        public ActionResult Index()
        {
            var searchModel = new RM_CustomerSearchModel
            {
                ListCustomerType = _customerTypeCache.GetAll()
                    .Select(d => new SelectListItem { Text = d.CustomerTypeName, Value = d.CustomerTypeID.ToString() }).ToList(),
                ListStatus = _statusCache.GetStatusBySearchKey("Customer")
                    .Select(d => new SelectListItem { Text = d.StatusName, Value = d.ID.ToString() }).ToList()
            };
            return View(searchModel);
        }

        /// <summary>
        /// Trả danh sách khách hàng theo điều kiện tìm kiếm và phân trang.
        /// </summary>
        /// <param name="searchModel">Điều kiện tìm kiếm khách hàng.</param>
        /// <returns>Dữ liệu JSON cho lưới danh sách khách hàng.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(RM_CustomerSearchModel searchModel)
        {
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);
            var filterKeyword = Request.Form.GetValues("filterKeyword")?[0];

            var dataSearch = new RM_CustomerSearchModel
            {
                Search = string.IsNullOrEmpty(filterKeyword) ? null : filterKeyword,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize,
            };

            var data = _customerCache.Get(out var total, searchModel, dataSearch);
            if (data != null && data.Any())
            {
                var statuses = _statusCache.GetStatusBySearchKey("Customer");
                foreach (var item in data)
                {
                    var status = statuses.FirstOrDefault(s => s.ID == item.CustomerStatusID);
                    if (status != null)
                    {
                        item.StatusName = status.StatusName;
                        item.StatusClass = status.StatusClass;
                    }
                }
            }
            return Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup thêm mới khách hàng.
        /// </summary>
        /// <returns>Popup thêm mới khách hàng.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add()
        {
            Session["SessionCustomerContacts"] = null;
            var model = new RM_CustomerModel
            {
                CustomerStatusID = 3,
                ListCustomerType = _customerTypeCache.GetAll()
                    .Select(d => new SelectListItem { Text = d.CustomerTypeName, Value = d.CustomerTypeID.ToString() }).ToList(),
                ListStatus = _statusCache.GetStatusBySearchKey("Customer")
                    .Select(d => new SelectListItem { Text = d.StatusName, Value = d.ID.ToString() }).ToList()
            };
            return PartialView("_Add", model);
        }

        /// <summary>
        /// Lưu thông tin khách hàng mới từ màn hình.
        /// </summary>
        /// <param name="model">Dữ liệu khách hàng cần lưu.</param>
        /// <returns>Kết quả xử lý thêm khách hàng.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(RM_CustomerModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CustomerStatusID = 3;
                model.ListCustomerType = _customerTypeCache.GetAll()
                    .Select(d => new SelectListItem { Text = d.CustomerTypeName, Value = d.CustomerTypeID.ToString() }).ToList();
                model.ListStatus = _statusCache.GetStatusBySearchKey("Customer")
                    .Select(d => new SelectListItem { Text = d.StatusName, Value = d.ID.ToString() }).ToList();
                return PartialView("_Customer", model);
            }

            string response;
            var result = _customerCache.Save(model, User.UserName);

            bool isSuccess = true;

            if (result == 0)
            {
                response = CreateMessage($"{_CustomerTitle} [{model.CustomerName}]", EnumProcessType.Add, EnumMsgIcon.Error);
                isSuccess = false;
            }
            else if (result == -9)
            {
                response = CreateMessage($"{_MSTLabel} [{model.TaxCode}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
                isSuccess = false;
            }
            else if (result == -8)
            {
                response = CreateMessage($"{_CusNameLabel} [{model.CustomerName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
                isSuccess = false;
            }
            else
            {
                var tempContacts = Session["SessionCustomerContacts"] as List<RM_CustomerContactModel>
                                   ?? new List<RM_CustomerContactModel>();

                if (tempContacts.Any())
                {
                    foreach (var item in tempContacts)
                    {
                        item.CustomerID = result;
                        _customerContactCache.Save(item, User.UserName);
                    }
                }

                Session["SessionCustomerContacts"] = null;

                response = CreateMessage($"{_CustomerTitle} [{model.CustomerName}]", EnumProcessType.Add, EnumMsgIcon.Success);
            }

            return Json(new { status = isSuccess, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup cập nhật thông tin khách hàng.
        /// </summary>
        /// <param name="id">Mã khách hàng cần cập nhật.</param>
        /// <returns>Popup cập nhật khách hàng hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id)
        {
            var model = _customerCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage(_CustomerTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            model.ListCustomerType = _customerTypeCache.GetAll()
                .Select(d => new SelectListItem { Text = d.CustomerTypeName, Value = d.CustomerTypeID.ToString() }).ToList();
            model.ListStatus = _statusCache.GetStatusBySearchKey("Customer")
                .Select(d => new SelectListItem { Text = d.StatusName, Value = d.ID.ToString() }).ToList();
            return PartialView("_Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng theo dữ liệu từ màn hình.
        /// </summary>
        /// <param name="model">Dữ liệu khách hàng cần cập nhật.</param>
        /// <returns>Kết quả xử lý cập nhật khách hàng.</returns>
        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(RM_CustomerModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ListCustomerType = _customerTypeCache.GetAll()
                    .Select(d => new SelectListItem { Text = d.CustomerTypeName, Value = d.CustomerTypeID.ToString() }).ToList();
                model.ListStatus = _statusCache.GetStatusBySearchKey("Customer")
                    .Select(d => new SelectListItem { Text = d.StatusName, Value = d.ID.ToString() }).ToList();
                return PartialView("_Customer", model);
            }

            string response;
            var result = _customerCache.Save(model, User.UserName);

            bool isSuccess = true;

            if (result == 0)
            {
                response = CreateMessage($"{_CustomerTitle} [{model.CustomerName}]", EnumProcessType.Edit, EnumMsgIcon.Error);
                isSuccess = false;
            }
            else if (result == -8)
            {
                response = CreateMessage($"{_CusNameLabel} [{model.CustomerName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
                isSuccess = false;
            }
            else if (result == -9)
            {
                response = CreateMessage($"{_MSTLabel} [{model.TaxCode}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
                isSuccess = false;
            }
            else
            {
                response = CreateMessage($"{_CustomerTitle} [{model.CustomerName}]", EnumProcessType.Edit, EnumMsgIcon.Success);
            }

            return Json(new { status = isSuccess, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup xác nhận xóa khách hàng.
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa.</param>
        /// <returns>Popup xác nhận xóa hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _customerCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage(_CustomerTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            ViewBag.ConfirmMessage = string.Format(
                AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_CustomerTitle} [{model.CustomerName}]");
            return PartialView("_Delete", model);
        }

        /// <summary>
        /// Xóa khách hàng theo dữ liệu được chọn.
        /// </summary>
        /// <param name="model">Dữ liệu khách hàng cần xóa.</param>
        /// <returns>Kết quả xử lý xóa khách hàng.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(RM_CustomerModel model)
        {
            var deleted = _customerCache.Delete(model, User.UserName);
            var response = CreateMessage($"{_CustomerTitle} [{model.CustomerName}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        /// <summary>
        /// Tìm kiếm nhanh khách hàng theo từ khóa để phục vụ các ô chọn dữ liệu.
        /// </summary>
        /// <param name="keyword">Từ khóa tìm kiếm khách hàng.</param>
        /// <param name="page">Trang dữ liệu cần lấy.</param>
        /// <returns>Dữ liệu JSON danh sách khách hàng rút gọn.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Search(string keyword, int? page)
        {
            var all = _customerCache.GetAll();

            if (!string.IsNullOrWhiteSpace(keyword))
                all = all.Where(x =>
                    (x.CustomerName != null && x.CustomerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.ShortName != null && x.ShortName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();

            int pageSize = 20;
            int pageIndex = page ?? 1;
            int total = all.Count;

            var items = all
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new { x.CustomerID, x.CustomerName })
                .ToList();

            return Json(new
            {
                items,
                hasMore = (pageIndex * pageSize) < total
            }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Customer - Export

        /// <summary>
        /// Xuất danh sách khách hàng ra file Excel theo điều kiện lọc.
        /// </summary>
        /// <param name="keyword">Từ khóa tìm kiếm khách hàng.</param>
        /// <param name="customerTypeID">Mã loại khách hàng cần lọc.</param>
        /// <param name="customerStatusID">Mã trạng thái khách hàng cần lọc.</param>
        /// <param name="cookieName">Tên cookie dùng để báo hoàn tất tải file.</param>
        /// <returns>File Excel danh sách khách hàng.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Export(string keyword, int? customerTypeID, int? customerStatusID,
                                   string cookieName = null)
        {
            var searchModel = new RM_CustomerSearchModel
            {
                Keyword = keyword,
                CustomerTypeID = customerTypeID,
                CustomerStatusID = customerStatusID
            };

            var dataSearch = new BaseSearchModel
            {
                Order = "0",
                OrderDir = "ASC",
                StartIndex = 0,
                PageSize = -1
            };

            var data = _customerCache.Get(out _, searchModel, dataSearch);
            var cusTypes = _customerTypeCache.GetAll();
            var statuses = _statusCache.GetStatusBySearchKey("Customer");

            var typeDict = cusTypes.ToDictionary(t => t.CustomerTypeID, t => t.CustomerTypeName);
            var statusDict = statuses.ToDictionary(s => s.ID, s => s.StatusName);

            var headers = new[]
            {
                "STT", "Tên khách hàng", "Tên viết tắt", "Mã số thuế",
                "Loại khách hàng", "Loại hình công ty", "Ngày thành lập",
                "Trạng thái", "Website", "Vốn điều lệ (triệu)",
                "Địa chỉ", "Tỉnh/Thành phố", "Phường/Xã",
                "Email", "Điện thoại", "Fax"
            };

            var colWidths = new[] { 5, 35, 18, 16, 18, 18, 16, 16, 25, 18, 35, 18, 15, 25, 14, 12 };

            var ms = new MemoryStream();
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Danh sach KH");

                for (int c = 0; c < headers.Length; c++)
                {
                    ExcelRange cell = ws.Cells[1, c + 1];
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Size = 11;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(GetColor("#1F4E79"));
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    ws.Column(c + 1).Width = colWidths[c];
                }

                ws.Row(1).Height = 22;

                if (data != null && data.Any())
                {
                    int stt = 1;
                    int totalRow = data.Count;

                    for (int r = 0; r < totalRow; r++)
                    {
                        var item = data[r];
                        int excelRow = r + 2;

                        typeDict.TryGetValue(item.CustomerTypeID, out var typeName);
                        statusDict.TryGetValue(item.CustomerStatusID, out var statusName);

                        ws.Cells[excelRow, 1].Value = stt++;
                        ws.Cells[excelRow, 2].Value = item.CustomerName;
                        ws.Cells[excelRow, 3].Value = item.ShortName;
                        WriteTaxCode(ws.Cells[excelRow, 4], item.TaxCode);
                        ws.Cells[excelRow, 5].Value = typeName ?? string.Empty;
                        ws.Cells[excelRow, 6].Value = item.CompanyType;
                        ws.Cells[excelRow, 7].Value = item.EstablishmentDate.HasValue
                            ? item.EstablishmentDate.Value.ToString("dd/MM/yyyy")
                            : string.Empty;
                        ws.Cells[excelRow, 8].Value = statusName ?? string.Empty;
                        ws.Cells[excelRow, 9].Value = item.Website;
                        ws.Cells[excelRow, 10].Value = item.CharterCapital;
                        ws.Cells[excelRow, 11].Value = item.AddressCus;
                        ws.Cells[excelRow, 12].Value = item.Province;
                        ws.Cells[excelRow, 13].Value = item.Ward;
                        ws.Cells[excelRow, 14].Value = item.Email;
                        ws.Cells[excelRow, 15].Value = item.Phone;
                        ws.Cells[excelRow, 16].Value = item.Fax;
                    }

                    ExcelRange dataRange = ws.Cells[2, 1, totalRow + 1, headers.Length];
                    dataRange.Style.Font.Size = 10;
                    dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    ws.Cells[2, 10, totalRow + 1, 10].Style.Numberformat.Format = "#,##0";
                    ws.Cells[2, 1, totalRow + 1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    Color evenFill = GetColor("#EBF3FB");

                    for (int r = 2; r <= totalRow + 1; r += 2)
                    {
                        ws.Cells[r, 1, r, headers.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[r, 1, r, headers.Length].Style.Fill.BackgroundColor.SetColor(evenFill);
                    }
                }

                ws.View.FreezePanes(2, 3);
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
            var fileName = $"DanhSachKhachHang_{DateTime.Now:ddMMyyyyHHmmss}.xlsx";
            return new FileStreamResult(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        #endregion

        #region Customer - Import

        /// <summary>
        /// Hiển thị form nhập danh sách khách hàng từ file Excel
        /// </summary>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Import()
        {
            return PartialView("_Import");
        }

        /// <summary>
        /// Cắt chuỗi theo độ dài cho phép, tránh lỗi truncate khi insert DB
        /// </summary>
        /// <param name="value">Chuỗi đầu vào</param>
        /// <param name="maxLength">Độ dài tối đa</param>
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Đọc giá trị ô import và chuẩn hóa về chuỗi theo kiểu dữ liệu thực tế.
        /// </summary>
        /// <param name="ws">Worksheet đang xử lý.</param>
        /// <param name="rowIndex">Dòng Excel cần đọc.</param>
        /// <param name="colIndex">Cột Excel cần đọc.</param>
        /// <returns>Giá trị ô sau khi chuẩn hóa.</returns>
        private static string GetImportCellText(ExcelWorksheet ws, int rowIndex, int colIndex)
        {
            object value = ws.Cells[rowIndex, colIndex].Value;

            if (value == null)
            {
                return string.Empty;
            }

            if (value is double doubleValue)
            {
                return doubleValue.ToString("0");
            }

            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("0");
            }

            if (value is float floatValue)
            {
                return floatValue.ToString("0");
            }

            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue.ToString("dd/MM/yyyy");
            }

            return Convert.ToString(value)?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Đọc một dòng dữ liệu import khách hàng và kiểm tra các điều kiện hợp lệ.
        /// </summary>
        /// <param name="ws">Worksheet đang xử lý import.</param>
        /// <param name="rowIndex">Dòng Excel cần đọc.</param>
        /// <param name="cusTypes">Danh sách loại khách hàng dùng để đối chiếu.</param>
        /// <param name="statuses">Danh sách trạng thái dùng để đối chiếu.</param>
        /// <returns>Dữ liệu một dòng import kèm danh sách lỗi nếu có.</returns>
        private RM_CustomerImportRowModel ReadImportRow(
            ExcelWorksheet ws,
            int rowIndex,
            IEnumerable<dynamic> cusTypes,
            IEnumerable<dynamic> statuses)
        {
            RM_CustomerImportRowModel row = new RM_CustomerImportRowModel
            {
                RowNumber = rowIndex
            };

            row.CustomerName = Truncate(GetImportCellText(ws, rowIndex, 1), 255);
            row.ShortName = Truncate(GetImportCellText(ws, rowIndex, 2), 100);
            row.TaxCode = Truncate(GetImportCellText(ws, rowIndex, 3), 50);
            row.CompanyType = Truncate(GetImportCellText(ws, rowIndex, 5), 100);
            row.Website = Truncate(GetImportCellText(ws, rowIndex, 8), 255);
            row.AddressCus = Truncate(GetImportCellText(ws, rowIndex, 10), 500);
            row.Province = Truncate(GetImportCellText(ws, rowIndex, 11), 100);
            row.Ward = Truncate(GetImportCellText(ws, rowIndex, 12), 100);
            row.Email = Truncate(GetImportCellText(ws, rowIndex, 13), 255);
            row.Phone = Truncate(GetImportCellText(ws, rowIndex, 14), 50);
            row.Fax = Truncate(GetImportCellText(ws, rowIndex, 15), 50);

            string cusTypeName = GetImportCellText(ws, rowIndex, 4);
            string estDateStr = GetImportCellText(ws, rowIndex, 6);
            string statusName = GetImportCellText(ws, rowIndex, 7);
            string capitalStr = GetImportCellText(ws, rowIndex, 9);

            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.CustomerName))
            {
                errors.Add(AppProcessor.Messagor.GetMessage("Customer_Message_ImportCustomerNameRequired"));
            }

            if (string.IsNullOrWhiteSpace(row.ShortName))
            {
                errors.Add(AppProcessor.Messagor.GetMessage("Customer_Message_ImportShortNameRequired"));
            }

            if (string.IsNullOrWhiteSpace(row.TaxCode))
            {
                errors.Add(AppProcessor.Messagor.GetMessage("Customer_Message_ImportTaxCodeRequired"));
            }

            if (string.IsNullOrWhiteSpace(row.AddressCus))
            {
                errors.Add(AppProcessor.Messagor.GetMessage("Customer_Message_ImportAddressRequired"));
            }

            if (!string.IsNullOrEmpty(cusTypeName))
            {
                dynamic ct = cusTypes.FirstOrDefault(x =>
                    string.Equals(x.CustomerTypeName, cusTypeName, StringComparison.OrdinalIgnoreCase));

                if (ct != null)
                {
                    row.CustomerTypeID = (byte)ct.CustomerTypeID;
                    row.CustomerTypeName = ct.CustomerTypeName;
                }
                else
                {
                    errors.Add(string.Format(
                        AppProcessor.Messagor.GetMessage("Customer_Message_ImportCustomerTypeNotFound"),
                        cusTypeName));
                }
            }
            else
            {
                errors.Add(AppProcessor.Messagor.GetMessage("Customer_Message_ImportCustomerTypeRequired"));
            }

            if (!string.IsNullOrEmpty(statusName))
            {
                dynamic st = statuses.FirstOrDefault(x =>
                    string.Equals(x.StatusName, statusName, StringComparison.OrdinalIgnoreCase));

                if (st != null)
                {
                    row.CustomerStatusID = (byte)st.ID;
                    row.StatusName = st.StatusName;
                    row.StatusClass = st.StatusClass;
                }
                else
                {
                    errors.Add(string.Format(
                        AppProcessor.Messagor.GetMessage("Customer_Message_ImportStatusNotFound"),
                        statusName));
                }
            }

            if (!string.IsNullOrEmpty(estDateStr))
            {
                if (DateTime.TryParseExact(
                        estDateStr,
                        new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime dt))
                {
                    row.EstablishmentDate = dt;
                }
                else
                {
                    errors.Add(string.Format(
                        AppProcessor.Messagor.GetMessage("Customer_Message_ImportEstablishmentDateInvalid"),
                        estDateStr));
                }
            }

            if (!string.IsNullOrEmpty(capitalStr))
            {
                string cleanCapital = capitalStr.Replace(",", "").Replace(".", "");

                if (decimal.TryParse(cleanCapital, out decimal capital))
                {
                    row.CharterCapital = capital;
                }
                else
                {
                    errors.Add(string.Format(
                        AppProcessor.Messagor.GetMessage("Customer_Message_ImportCharterCapitalInvalid"),
                        capitalStr));
                }
            }

            if (!string.IsNullOrEmpty(row.Email) &&
                !System.Text.RegularExpressions.Regex.IsMatch(
                    row.Email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                errors.Add(AppProcessor.Messagor.GetMessage("Customer_Message_ImportEmailInvalid"));
            }

            row.Errors = errors;

            return row;
        }

        /// <summary>
        /// Kiểm tra một dòng import có hoàn toàn trống ở các cột chính hay không.
        /// </summary>
        /// <param name="ws">Worksheet đang xử lý import.</param>
        /// <param name="rowIndex">Dòng Excel cần kiểm tra.</param>
        /// <returns>True nếu dòng không có dữ liệu cần import.</returns>
        private bool IsImportEmptyRow(ExcelWorksheet ws, int rowIndex)
        {
            return string.IsNullOrWhiteSpace(GetImportCellText(ws, rowIndex, 1))
                && string.IsNullOrWhiteSpace(GetImportCellText(ws, rowIndex, 2))
                && string.IsNullOrWhiteSpace(GetImportCellText(ws, rowIndex, 3));
        }

        /// <summary>
        /// Nhận file Excel, lưu tạm và trả về kết quả preview (valid/error rows)
        /// Hiển thị tối đa 50 dòng trên FE, nhưng lưu toàn bộ vào Session để export
        /// </summary>
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ImportPreview(HttpPostedFileBase importFile)
        {
            if (importFile == null || importFile.ContentLength == 0)
            {
                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportFileRequired")
                });
            }

            string ext = Path.GetExtension(importFile.FileName)?.ToLower();

            if (ext != ".xlsx")
            {
                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportOnlyXlsx")
                });
            }

            string importDir = Server.MapPath(ConfigurationManager.AppSettings["ImportTempPath"]);
            string tempFileName = Guid.NewGuid().ToString("N") + ext;
            string tempFilePath = Path.Combine(importDir, tempFileName);

            if (!Directory.Exists(importDir))
            {
                Directory.CreateDirectory(importDir);
            }

            importFile.SaveAs(tempFilePath);
            Session["ImportCustomerFilePath"] = tempFilePath;

            IEnumerable<dynamic> cusTypes = _customerTypeCache.GetAll();
            IEnumerable<dynamic> statuses = _statusCache.GetStatusBySearchKey("Customer");
            List<RM_CustomerImportRowModel> allErrorRows = new List<RM_CustomerImportRowModel>();
            List<RM_CustomerImportRowModel> previewErrorRows = new List<RM_CustomerImportRowModel>();
            List<RM_CustomerImportRowModel> previewValidRows = new List<RM_CustomerImportRowModel>();
            int validCount = 0;
            int errorCount = 0;

            try
            {
                FileInfo fileInfo = new FileInfo(tempFilePath);

                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets.FirstOrDefault();

                    if (ws == null || ws.Dimension == null)
                    {
                        return Json(new
                        {
                            status = false,
                            message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportFileEmpty")
                        });
                    }

                    int lastRow = ws.Dimension.End.Row;

                    for (int r = 2; r <= lastRow; r++)
                    {
                        if (IsImportEmptyRow(ws, r))
                        {
                            continue;
                        }

                        RM_CustomerImportRowModel row = ReadImportRow(ws, r, cusTypes, statuses);

                        if (row.Errors.Count > 0)
                        {
                            errorCount++;
                            allErrorRows.Add(row);

                            if (previewErrorRows.Count < 50)
                            {
                                previewErrorRows.Add(row);
                            }
                        }
                        else
                        {
                            validCount++;

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

                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }

                Session["ImportCustomerFilePath"] = null;

                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportReadFailed")
                });
            }

            Session["ImportCustomerErrorData"] = allErrorRows;

            return Json(new
            {
                status = true,
                totalValid = validCount,
                totalError = errorCount,
                errorRows = previewErrorRows,
                validRows = previewValidRows,
                hasMoreErrors = errorCount > 50,
                hasMoreValid = validCount > 50
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Xác nhận import: đọc lại file tạm, chỉ insert các dòng hợp lệ vào DB
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ImportConfirm()
        {
            string tempFilePath = Session["ImportCustomerFilePath"] as string;

            if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
            {
                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportFileNotFound")
                });
            }

            IEnumerable<dynamic> cusTypes = _customerTypeCache.GetAll();
            IEnumerable<dynamic> statuses = _statusCache.GetStatusBySearchKey("Customer");
            List<RM_CustomerImportRowModel> validRows = new List<RM_CustomerImportRowModel>();

            // allSuccessRows: lưu toàn bộ để export
            // previewSuccessRows: chỉ lấy 50 dòng đầu để trả về FE
            List<RM_CustomerImportRowModel> allSuccessRows = new List<RM_CustomerImportRowModel>();
            List<RM_CustomerImportRowModel> previewSuccessRows = new List<RM_CustomerImportRowModel>();

            try
            {
                FileInfo fileInfo = new FileInfo(tempFilePath);

                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets.FirstOrDefault();

                    if (ws == null || ws.Dimension == null)
                    {
                        return Json(new
                        {
                            status = false,
                            message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportFileEmpty")
                        });
                    }

                    int lastRow = ws.Dimension.End.Row;

                    for (int r = 2; r <= lastRow; r++)
                    {
                        if (IsImportEmptyRow(ws, r))
                        {
                            continue;
                        }

                        RM_CustomerImportRowModel row = ReadImportRow(ws, r, cusTypes, statuses);

                        if (row.Errors.Count == 0)
                        {
                            validRows.Add(row);
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
                    message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportReadFailed")
                });
            }
            finally
            {
                // Xóa file tạm dù thành công hay lỗi
                try
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }
                catch
                {
                }

                Session["ImportCustomerFilePath"] = null;
            }

            if (!validRows.Any())
            {
                return Json(new
                {
                    status = false,
                    message = AppProcessor.Messagor.GetMessage("Customer_Message_ImportNoValidRows")
                });
            }

            var result = _customerCache.BulkImport(validRows, User.UserName);

            // Lưu danh sách nhập thành công vào Session để export đầy đủ
            allSuccessRows = validRows
                .Take(result.SuccessCount)
                .ToList();

            previewSuccessRows = allSuccessRows
                .Take(50)
                .ToList();

            Session["ImportCustomerSuccessData"] = allSuccessRows;
            Session["ImportCustomerDuplicateData"] = result.DuplicateRows;

            string message = result.FailCount > 0
                ? string.Format(
                    AppProcessor.Messagor.GetMessage("Customer_Message_ImportCompletedWithDuplicate"),
                    result.SuccessCount,
                    result.FailCount)
                : string.Format(
                    AppProcessor.Messagor.GetMessage("Customer_Message_ImportCompleted"),
                    result.SuccessCount);

            return Json(new
            {
                status = true,
                message,
                successCount = result.SuccessCount,
                failCount = result.FailCount,
                successRows = previewSuccessRows,
                hasMoreSuccess = result.SuccessCount > 50,
                duplicateRows = result.DuplicateRows
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Chuyển mã màu HTML sang đối tượng màu để dùng cho export Excel.
        /// </summary>
        /// <param name="htmlColor">Mã màu HTML cần chuyển đổi.</param>
        /// <returns>Đối tượng màu tương ứng.</returns>
        private static Color GetColor(string htmlColor)
        {
            return ColorTranslator.FromHtml(htmlColor);
        }

        /// <summary>
        /// Ghi style chung cho 1 dòng dữ liệu trong worksheet export
        /// </summary>
        /// <param name="ws">Worksheet đang xử lý</param>
        /// <param name="excelRow">Chỉ số dòng Excel</param>
        /// <param name="colCount">Số lượng cột</param>
        /// <param name="bgColor">Màu nền</param>
        private static void ApplyRowStyle(ExcelWorksheet ws, int excelRow, int colCount, Color bgColor)
        {
            for (int col = 1; col <= colCount; col++)
            {
                ExcelRange cell = ws.Cells[excelRow, col];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(bgColor);
                cell.Style.Font.Size = 10;
                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }
        }

        /// <summary>
        /// Ghi header cho worksheet export
        /// </summary>
        /// <param name="ws">Worksheet đang xử lý</param>
        /// <param name="headers">Mảng tiêu đề cột</param>
        /// <param name="colWidths">Mảng độ rộng cột</param>
        /// <param name="lastColColor">Màu nền cột cuối (cột ghi chú/lỗi)</param>
        private static void WriteExportHeader(
            ExcelWorksheet ws,
            string[] headers,
            int[] colWidths,
            Color lastColColor)
        {
            for (int c = 0; c < headers.Length; c++)
            {
                ExcelRange cell = ws.Cells[1, c + 1];
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 11;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(c == headers.Length - 1 ? lastColColor : GetColor("#1F4E79"));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws.Column(c + 1).Width = colWidths[c];
            }

            ws.Row(1).Height = 22;
        }

        /// <summary>
        /// Ghi giá trị TaxCode vào cell, ép kiểu text để tránh scientific notation
        /// </summary>
        /// <param name="cell">Cell cần ghi</param>
        /// <param name="taxCode">Giá trị mã số thuế</param>
        private static void WriteTaxCode(ExcelRange cell, string taxCode)
        {
            cell.Value = taxCode ?? string.Empty;
            cell.Style.Numberformat.Format = "@";
        }

        /// <summary>
        /// Xuất file Excel chứa các dòng lỗi từ lần preview gần nhất (toàn bộ, không giới hạn)
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportErrorRows(string cookieName = null)
        {
            List<RM_CustomerImportRowModel> errorRows =
                Session["ImportCustomerErrorData"] as List<RM_CustomerImportRowModel>;

            MemoryStream ms = new MemoryStream();

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Dong_Loi");

                string[] headers = new[]
                {
            "Dòng Excel",
            "Tên khách hàng (*)", "Tên viết tắt (*)", "Mã số thuế (*)",
            "Loại khách hàng (*)", "Loại hình công ty",
            "Ngày thành lập (dd/MM/yyyy)", "Trạng thái",
            "Website", "Vốn điều lệ (triệu)",
            "Địa chỉ (*)", "Tỉnh/Thành phố", "Phường/Xã",
            "Email", "Điện thoại", "Fax",
            "Lý do lỗi (không nhập cột này)"
        };

                int[] colWidths = new[] { 9, 35, 18, 16, 18, 18, 22, 16, 25, 16, 35, 18, 15, 25, 14, 12, 55 };

                if (errorRows == null || !errorRows.Any())
                {
                    ws.Cells[1, 1].Value = "Không có dữ liệu lỗi. Vui lòng đọc file import trước.";
                    package.SaveAs(ms);
                }
                else
                {
                    WriteExportHeader(ws, headers, colWidths, GetColor("#C00000"));

                    ws.Column(4).Style.Numberformat.Format = "@";

                    for (int r = 0; r < errorRows.Count; r++)
                    {
                        RM_CustomerImportRowModel row = errorRows[r];
                        int excelRow = r + 2;

                        ws.Cells[excelRow, 1].Value = row.RowNumber;
                        ws.Cells[excelRow, 2].Value = row.CustomerName ?? string.Empty;
                        ws.Cells[excelRow, 3].Value = row.ShortName ?? string.Empty;
                        WriteTaxCode(ws.Cells[excelRow, 4], row.TaxCode);
                        ws.Cells[excelRow, 5].Value = row.CustomerTypeName ?? string.Empty;
                        ws.Cells[excelRow, 6].Value = row.CompanyType ?? string.Empty;
                        ws.Cells[excelRow, 7].Value = row.EstablishmentDate.HasValue
                            ? row.EstablishmentDate.Value.ToString("dd/MM/yyyy") : "";
                        ws.Cells[excelRow, 8].Value = row.StatusName ?? string.Empty;
                        ws.Cells[excelRow, 9].Value = row.Website ?? string.Empty;
                        ws.Cells[excelRow, 10].Value = row.CharterCapital == 0 ? string.Empty : row.CharterCapital.ToString();
                        ws.Cells[excelRow, 11].Value = row.AddressCus ?? string.Empty;
                        ws.Cells[excelRow, 12].Value = row.Province ?? string.Empty;
                        ws.Cells[excelRow, 13].Value = row.Ward ?? string.Empty;
                        ws.Cells[excelRow, 14].Value = row.Email ?? string.Empty;
                        ws.Cells[excelRow, 15].Value = row.Phone ?? string.Empty;
                        ws.Cells[excelRow, 16].Value = row.Fax ?? string.Empty;

                        string errorText = row.Errors != null && row.Errors.Any()
                            ? string.Join("\n", row.Errors) : "";
                        ExcelRange errorCell = ws.Cells[excelRow, 17];
                        errorCell.Value = errorText;
                        errorCell.Style.Font.Color.SetColor(Color.DarkRed);
                        errorCell.Style.WrapText = true;

                        ApplyRowStyle(ws, excelRow, headers.Length, GetColor("#FFF2CC"));
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

            string fileName = $"DongLoi_Import_KhachHang_{DateTime.Now:ddMMyyyyHHmmss}.xlsx";

            return new FileStreamResult(ms,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        /// <summary>
        /// Xuất file Excel chứa các dòng nhập thành công (toàn bộ, không giới hạn)
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportSuccessRows(string cookieName = null)
        {
            List<RM_CustomerImportRowModel> successRows =
                Session["ImportCustomerSuccessData"] as List<RM_CustomerImportRowModel>;

            MemoryStream ms = new MemoryStream();

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Dong_Thanh_Cong");

                string[] headers = new[]
                {
            "Dòng Excel",
            "Tên khách hàng", "Tên viết tắt", "Mã số thuế",
            "Loại khách hàng", "Loại hình công ty",
            "Ngày thành lập", "Trạng thái",
            "Website", "Vốn điều lệ (triệu)",
            "Địa chỉ", "Tỉnh/Thành phố", "Phường/Xã",
            "Email", "Điện thoại", "Fax"
        };

                int[] colWidths = new[] { 9, 35, 18, 16, 18, 18, 22, 16, 25, 16, 35, 18, 15, 25, 14, 12 };

                if (successRows == null || !successRows.Any())
                {
                    ws.Cells[1, 1].Value = "Không có dữ liệu thành công. Vui lòng thực hiện import trước.";
                    package.SaveAs(ms);
                }
                else
                {
                    WriteExportHeader(ws, headers, colWidths, GetColor("#1F4E79"));

                    ws.Column(4).Style.Numberformat.Format = "@";

                    for (int r = 0; r < successRows.Count; r++)
                    {
                        RM_CustomerImportRowModel row = successRows[r];
                        int excelRow = r + 2;

                        ws.Cells[excelRow, 1].Value = row.RowNumber;
                        ws.Cells[excelRow, 2].Value = row.CustomerName ?? string.Empty;
                        ws.Cells[excelRow, 3].Value = row.ShortName ?? string.Empty;
                        WriteTaxCode(ws.Cells[excelRow, 4], row.TaxCode);
                        ws.Cells[excelRow, 5].Value = row.CustomerTypeName ?? string.Empty;
                        ws.Cells[excelRow, 6].Value = row.CompanyType ?? string.Empty;
                        ws.Cells[excelRow, 7].Value = row.EstablishmentDate.HasValue
                            ? row.EstablishmentDate.Value.ToString("dd/MM/yyyy") : "";
                        ws.Cells[excelRow, 8].Value = row.StatusName ?? string.Empty;
                        ws.Cells[excelRow, 9].Value = row.Website ?? string.Empty;
                        ws.Cells[excelRow, 10].Value = row.CharterCapital == 0 ? string.Empty : row.CharterCapital.ToString();
                        ws.Cells[excelRow, 11].Value = row.AddressCus ?? string.Empty;
                        ws.Cells[excelRow, 12].Value = row.Province ?? string.Empty;
                        ws.Cells[excelRow, 13].Value = row.Ward ?? string.Empty;
                        ws.Cells[excelRow, 14].Value = row.Email ?? string.Empty;
                        ws.Cells[excelRow, 15].Value = row.Phone ?? string.Empty;
                        ws.Cells[excelRow, 16].Value = row.Fax ?? string.Empty;

                        ApplyRowStyle(ws, excelRow, headers.Length, GetColor("#E2EFDA"));
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

            string fileName = $"ThanhCong_Import_KhachHang_{DateTime.Now:ddMMyyyyHHmmss}.xlsx";

            return new FileStreamResult(ms,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        /// <summary>
        /// Xuất file Excel chứa các dòng trùng sau khi import
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ExportDuplicateRows(string cookieName = null)
        {
            List<RM_CustomerDuplicateModel> duplicateRows =
                Session["ImportCustomerDuplicateData"] as List<RM_CustomerDuplicateModel>;

            MemoryStream ms = new MemoryStream();

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Dong_Trung");

                string[] headers = new[] { "STT", "Tên khách hàng", "Mã số thuế", "Lý do" };
                int[] colWidths = new[] { 6, 40, 18, 40 };

                WriteExportHeader(ws, headers, colWidths, GetColor("#C00000"));

                ws.Column(3).Style.Numberformat.Format = "@";

                if (duplicateRows != null && duplicateRows.Any())
                {
                    for (int r = 0; r < duplicateRows.Count; r++)
                    {
                        RM_CustomerDuplicateModel row = duplicateRows[r];
                        int excelRow = r + 2;

                        ws.Cells[excelRow, 1].Value = r + 1;
                        ws.Cells[excelRow, 2].Value = row.CustomerName ?? string.Empty;
                        WriteTaxCode(ws.Cells[excelRow, 3], row.TaxCode);
                        ws.Cells[excelRow, 4].Value = row.Reason ?? string.Empty;

                        ws.Cells[excelRow, 4].Style.Font.Color.SetColor(Color.DarkRed);

                        ApplyRowStyle(ws, excelRow, headers.Length, GetColor("#FFF2CC"));
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

            string fileName = $"DongTrung_Import_KhachHang_{DateTime.Now:ddMMyyyyHHmmss}.xlsx";

            return new FileStreamResult(ms,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        /// <summary>
        /// Tải file Excel mẫu kèm sheet danh mục tham chiếu (loại KH, trạng thái)
        /// </summary>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult DownloadImportTemplate()
        {
            var cusTypes = _customerTypeCache.GetAll();
            var statuses = _statusCache.GetStatusBySearchKey("Customer");

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add("Template_KhachHang");

                string[] headers = new[]
                {
            "Tên khách hàng (*)", "Tên viết tắt (*)", "Mã số thuế (*)",
            "Loại khách hàng (*)", "Loại hình công ty",
            "Ngày thành lập (dd/MM/yyyy)", "Trạng thái",
            "Website", "Vốn điều lệ (triệu)",
            "Địa chỉ (*)", "Tỉnh/Thành phố", "Phường/Xã",
            "Email", "Điện thoại", "Fax"
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

                ws.Column(3).Style.Numberformat.Format = "@";

                ws.Row(1).Height = 20;
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                ExcelWorksheet wsCat = package.Workbook.Worksheets.Add("Danh_Muc");

                ExcelRange titleType = wsCat.Cells[1, 1];
                titleType.Value = "Loại khách hàng";
                titleType.Style.Font.Bold = true;
                titleType.Style.Font.Size = 11;
                titleType.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleType.Style.Fill.BackgroundColor.SetColor(GetColor("#1F4E79"));
                titleType.Style.Font.Color.SetColor(Color.White);
                titleType.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                titleType.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                titleType.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                titleType.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                titleType.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                wsCat.Column(1).Width = 30;

                if (cusTypes != null)
                {
                    for (int i = 0; i < cusTypes.Count; i++)
                    {
                        ExcelRange cell = wsCat.Cells[i + 2, 1];
                        cell.Value = cusTypes[i].CustomerTypeName;
                        cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Font.Size = 10;

                        if (i % 2 == 1)
                        {
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(GetColor("#EBF3FB"));
                        }
                    }
                }

                ExcelRange titleStatus = wsCat.Cells[1, 3];
                titleStatus.Value = "Trạng thái";
                titleStatus.Style.Font.Bold = true;
                titleStatus.Style.Font.Size = 11;
                titleStatus.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleStatus.Style.Fill.BackgroundColor.SetColor(GetColor("#1F4E79"));
                titleStatus.Style.Font.Color.SetColor(Color.White);
                titleStatus.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                titleStatus.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                titleStatus.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                titleStatus.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                titleStatus.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                wsCat.Column(3).Width = 25;

                if (statuses != null)
                {
                    for (int i = 0; i < statuses.Count; i++)
                    {
                        ExcelRange cell = wsCat.Cells[i + 2, 3];
                        cell.Value = statuses[i].StatusName;
                        cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Font.Size = 10;

                        if (i % 2 == 1)
                        {
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(GetColor("#EBF3FB"));
                        }
                    }
                }

                wsCat.Protection.IsProtected = true;
                wsCat.Protection.SetPassword("readonly123");

                byte[] fileBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    package.SaveAs(ms);
                    fileBytes = ms.ToArray();
                }

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Template_Import_KhachHang.xlsx");
            }
        }

        #endregion

        #region Cơ hội kinh doanh (CHKD)

        /// <summary>
        /// Hiển thị danh sách cơ hội kinh doanh theo khách hàng được chọn.
        /// </summary>
        /// <param name="id">Mã khách hàng cần xem cơ hội kinh doanh.</param>
        /// <returns>Partial view danh sách cơ hội kinh doanh hoặc thông báo khi khách hàng không tồn tại.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult IndexBusinessOpportunity(int id)
        {
            var cus = _customerCache.GetById(id);
            if (cus == null)
            {
                return Json(new { status = true, message = CreateMessage(_CustomerTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) }, JsonRequestBehavior.AllowGet);
            }

            var model = new RM_BusinessOpportunitySearchModel
            {
                CustomerID = cus.CustomerID,
                CustomerName = cus.CustomerName,
                Employees = _employeesCache.GetAll(),
                ProductServices = _productServiceCache.GetAllChild(),
                StatusList = _statusCache.GetStatusBySearchKey("Opportunity")
            };
            return PartialView("_BusinessOpportunityView", model);
        }

        /// <summary>
        /// Trả danh sách cơ hội kinh doanh của khách hàng theo điều kiện tìm kiếm và phân trang.
        /// </summary>
        /// <param name="model">Điều kiện tìm kiếm cơ hội kinh doanh.</param>
        /// <returns>Dữ liệu JSON cho lưới cơ hội kinh doanh.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetBusinessOpportunity(RM_BusinessOpportunitySearchModel model)
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
            var data = _businessCache.Get(out var total, model, dataSearch);

            if (data != null && data.Count > 0)
            {
                var productServiceList = _productServiceCache.GetAllChild();
                var employeeList = _employeesCache.GetAll();
                foreach (var item in data)
                {
                    if (!string.IsNullOrEmpty(item.ProductServiceIDs))
                    {
                        var ids = item.ProductServiceIDs.Split(';').Select(int.Parse).ToList();
                        item.ProductServiceNames = string.Join(";",
                            productServiceList.Where(r => ids.Contains(r.ProductServiceID)).Select(r => r.NameProduct));
                    }
                    if (!string.IsNullOrEmpty(item.EmployeeIDs))
                    {
                        var ids = item.EmployeeIDs.Split(';').Select(int.Parse).ToList();
                        item.EmployeeNames = string.Join(";",
                            employeeList.Where(r => ids.Contains(r.Employee_ID)).Select(r => r.FullName));
                    }
                }
            }
            return Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình thêm mới cơ hội kinh doanh theo khách hàng được chọn.
        /// </summary>
        /// <param name="CustomerID">Mã khách hàng cần khởi tạo dữ liệu cơ hội kinh doanh.</param>
        /// <returns>Popup thêm mới cơ hội kinh doanh hoặc thông báo khi khách hàng không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddCHKD(int CustomerID)
        {
            var customer = _customerCache.GetById(CustomerID);
            if (customer == null)
            {
                return Json(new { status = true, message = CreateMessage(_CustomerTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            var model = new RM_BusinessOpportunityModel
            {
                CustomerID = CustomerID,
                CustomerName = customer.CustomerName,
                SPDichVus = _productServiceCache.GetAllChild()
            };
            return PartialView("_AddCHKD", model);
        }

        /// <summary>
        /// Lưu thông tin cơ hội kinh doanh từ màn hình khách hàng.
        /// </summary>
        /// <param name="model">Dữ liệu cơ hội kinh doanh cần lưu.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult AddCHKD(RM_BusinessOpportunityModel model)
        {
            if (!ModelState.IsValid)
            {
                var customer = _customerCache.GetById(model.CustomerID);
                model.CustomerName = customer?.CustomerName;
                model.SPDichVus = _productServiceCache.GetAllChild();
                return PartialView("_AddCHKD_View", model);
            }

            model.SalesStageID = 1;
            model.StatusID = 1;

            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderImage + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuAnh(file, FileAttach);
                            FileAttachs.Add(FileAttach);
                        }
                    }
                }
                model.FileAttach = string.Join("||", FileAttachs);
            }
            if (model.lst_SP != null)
            {
                model.lst_SP = model.lst_SP.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                model.ProductServiceIDs = string.Join(";", model.lst_SP);
            }

            var result = _customerCache.SaveCHKD(model, User.UserName);
            var dataUser = _customerCache.GetById(model.CustomerID);
            var customerName = dataUser?.CustomerName;
            string response;

            if (result == 0)
                response = CreateMessage($"{_CHKDTitle} [{customerName}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_CHKDTitle} [{customerName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_CHKDTitle} [{customerName}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình cập nhật cơ hội kinh doanh của khách hàng và danh sách file đính kèm hiện có.
        /// </summary>
        /// <param name="id">Mã cơ hội kinh doanh cần cập nhật.</param>
        /// <returns>Popup cập nhật cơ hội kinh doanh hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditCHKD(int id)
        {
            var model = _businessCache.GetById(id);
            if (model == null)
            {
                return Json(new { status = true, message = CreateMessage(_CHKDTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            var files = _businessOpportunityFilePathCache.GetByBOID(model.BusinessOpportunityID);

            model.SPDichVus = _productCache.GetAllChild();
            model.ExistingFiles = files.Select(f => new RM_BusinessOpportunityFilePathModel
            {
                FilePathID = f.FilePathID,
                FilePath = f.FilePath
            }).ToList();
            return PartialView("_EditCHKD", model);
        }

        /// <summary>
        /// Cập nhật thông tin cơ hội kinh doanh từ màn hình khách hàng.
        /// </summary>
        /// <param name="model">Dữ liệu cơ hội kinh doanh cần cập nhật.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult EditCHKD(RM_BusinessOpportunityModel model)
        {
            if (!ModelState.IsValid)
            {
                var files = _businessOpportunityFilePathCache.GetByBOID(model.BusinessOpportunityID);
                var customer = _customerCache.GetById(model.CustomerID);
                model.CustomerName = customer?.CustomerName;
                model.SPDichVus = _productCache.GetAllChild();
                model.ExistingFiles = files.Select(f => new RM_BusinessOpportunityFilePathModel
                {
                    FilePathID = f.FilePathID,
                    FilePath = f.FilePath
                }).ToList();
                return PartialView("_AddCHKD_View", model);
            }

            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderImage + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuAnh(file, FileAttach);
                            FileAttachs.Add(FileAttach);
                        }
                    }
                }
                model.FileAttach = string.Join("||", FileAttachs);
            }

            if (model.DeletedFileIds != null && model.DeletedFileIds.Any())
            {
                foreach (var fileId in model.DeletedFileIds)
                {
                    var file = _businessOpportunityFilePathCache.GetById(fileId);

                    if (file != null && !string.IsNullOrEmpty(file.FilePath))
                    {
                        try
                        {
                            var fullPath = Server.MapPath(file.FilePath);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppProcessor.Logger.Error(new Exception(ex.ToString()));
                        }
                        _businessOpportunityFilePathCache.Delete(fileId, User.UserName);
                    }
                }
            }

            var result = _customerCache.SaveCHKD(model, User.UserName);
            var dataUser = _customerCache.GetById(model.CustomerID);
            var customerName = dataUser?.CustomerName;
            string response;

            if (result == 0)
                response = CreateMessage($"{_CHKDTitle} [{customerName}]", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_CHKDTitle} [{customerName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_CHKDTitle} [{customerName}]", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup xác nhận xóa cơ hội kinh doanh của khách hàng.
        /// </summary>
        /// <param name="id">Mã cơ hội kinh doanh cần xóa.</param>
        /// <returns>Popup xác nhận xóa hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteCHKD(int id)
        {
            var model = _businessCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage(_CHKDTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            ViewBag.ConfirmMessage = string.Format(
                AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_CHKDTitle} [{model.CustomerName}]");
            return PartialView("_DeleteCHKD", model);
        }

        /// <summary>
        /// Xóa cơ hội kinh doanh của khách hàng theo dữ liệu được chọn.
        /// </summary>
        /// <param name="model">Dữ liệu cơ hội kinh doanh cần xóa.</param>
        /// <returns>Kết quả xử lý xóa cơ hội kinh doanh.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteCHKD(RM_BusinessOpportunityModel model)
        {
            var deleted = _businessCache.Delete(model, User.UserName);
            var response = CreateMessage($"{_CHKDTitle} [{model.CustomerName}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        #endregion

        #region Ngày kỷ niệm (Anniversary)

        /// <summary>
        /// Hiển thị danh sách ngày kỷ niệm của khách hàng được chọn.
        /// </summary>
        /// <param name="id">Mã khách hàng cần xem ngày kỷ niệm.</param>
        /// <returns>Partial view danh sách ngày kỷ niệm.</returns>
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult IndexAnniversary(int id)
        {
            var cus = _customerCache.GetById(id);
            var model = new RM_CustomerAnniversarySearchModel
            {
                CustomerID = cus.CustomerID,
                CustomerName = cus.CustomerName
            };
            return PartialView("_AnniversaryView", model);
        }

        /// <summary>
        /// Trả danh sách ngày kỷ niệm của khách hàng theo điều kiện tìm kiếm và phân trang.
        /// </summary>
        /// <param name="model">Điều kiện tìm kiếm ngày kỷ niệm.</param>
        /// <returns>Dữ liệu JSON cho lưới ngày kỷ niệm.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetAnniversary(RM_CustomerAnniversarySearchModel model)
        {
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);

            var dataSearch = new BaseSearchModel
            {
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };
            var data = _anniversaryCache.Get(out var total, model, dataSearch);
            return Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup thêm mới ngày kỷ niệm cho khách hàng.
        /// </summary>
        /// <param name="CustomerID">Mã khách hàng cần thêm ngày kỷ niệm.</param>
        /// <returns>Popup thêm mới ngày kỷ niệm.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddAnniversary(int CustomerID)
        {
            var cus = _customerCache.GetById(CustomerID);
            var model = new RM_CustomerAnniversaryModel
            {
                CustomerID = CustomerID,
                CustomerName = cus?.CustomerName,
                IsLunar = false,
                IsReminder = false,
                ListAnniversaryType = _anniversaryBiz.GetAllAnniversaryType()
            };
            return PartialView("_AddAnniversary", model);
        }

        /// <summary>
        /// Lưu ngày kỷ niệm mới của khách hàng.
        /// </summary>
        /// <param name="model">Dữ liệu ngày kỷ niệm cần lưu.</param>
        /// <returns>Kết quả xử lý thêm ngày kỷ niệm.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult AddAnniversary(RM_CustomerAnniversaryModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CustomerName = _customerCache.GetById(model.CustomerID)?.CustomerName;
                model.ListAnniversaryType = _anniversaryBiz.GetAllAnniversaryType();
                return PartialView("_Anniversary_View", model);
            }

            var result = _anniversaryCache.Save(model, User.UserName);
            var cusName = _customerCache.GetById(model.CustomerID)?.CustomerName;
            string response;

            if (result == 0)
                response = CreateMessage($"{_AnniversaryTitle} [{cusName}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_AnniversaryTitle} [{cusName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_AnniversaryTitle} [{cusName}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup cập nhật ngày kỷ niệm của khách hàng.
        /// </summary>
        /// <param name="id">Mã ngày kỷ niệm cần cập nhật.</param>
        /// <returns>Popup cập nhật ngày kỷ niệm hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditAnniversary(int id)
        {
            var model = _anniversaryCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage(_AnniversaryTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) }, JsonRequestBehavior.AllowGet);
            model.CustomerName = _customerCache.GetById(model.CustomerID)?.CustomerName;
            model.ListAnniversaryType = _anniversaryBiz.GetAllAnniversaryType();
            return PartialView("_EditAnniversary", model);
        }

        /// <summary>
        /// Cập nhật thông tin ngày kỷ niệm của khách hàng.
        /// </summary>
        /// <param name="model">Dữ liệu ngày kỷ niệm cần cập nhật.</param>
        /// <returns>Kết quả xử lý cập nhật ngày kỷ niệm.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult EditAnniversary(RM_CustomerAnniversaryModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CustomerName = _customerCache.GetById(model.CustomerID)?.CustomerName;
                model.ListAnniversaryType = _anniversaryBiz.GetAllAnniversaryType();
                return PartialView("_Anniversary_View", model);
            }

            var result = _anniversaryCache.Save(model, User.UserName);
            var cusName = _customerCache.GetById(model.CustomerID)?.CustomerName;
            string response;

            if (result == 0)
                response = CreateMessage($"{_AnniversaryTitle} [{cusName}]", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_AnniversaryTitle} [{cusName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_AnniversaryTitle} [{cusName}]", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup xác nhận xóa ngày kỷ niệm của khách hàng.
        /// </summary>
        /// <param name="id">Mã ngày kỷ niệm cần xóa.</param>
        /// <returns>Popup xác nhận xóa hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteAnniversary(int id)
        {
            var model = _anniversaryCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage(_AnniversaryTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error) }, JsonRequestBehavior.AllowGet);
            model.CustomerName = _customerCache.GetById(model.CustomerID)?.CustomerName;
            ViewBag.ConfirmMessage = string.Format(
                AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_AnniversaryTitle} [{model.CustomerName} - {model.NameAnniversaryType}]");
            return PartialView("_DeleteAnniversary", model);
        }

        /// <summary>
        /// Xóa ngày kỷ niệm của khách hàng theo dữ liệu được chọn.
        /// </summary>
        /// <param name="model">Dữ liệu ngày kỷ niệm cần xóa.</param>
        /// <returns>Kết quả xử lý xóa ngày kỷ niệm.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteAnniversary(RM_CustomerAnniversaryModel model)
        {
            var deleted = _anniversaryCache.Delete(model, User.UserName);
            var cusName = _customerCache.GetById(model.CustomerID)?.CustomerName;
            var response = CreateMessage(
                $"{_AnniversaryTitle} [{cusName}]",
                EnumProcessType.Delete,
                deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        #endregion

        #region Liên hệ khách hàng (CustomerContact)

        /// <summary>
        /// Hiển thị popup thêm người liên hệ vào khách hàng.
        /// </summary>
        /// <param name="CustomerID">Mã khách hàng cần gắn người liên hệ.</param>
        /// <returns>Popup thêm người liên hệ cho khách hàng.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddCustomerContact(int CustomerID)
        {
            var cus = _customerCache.GetById(CustomerID);  
            var model = new RM_CustomerModel
            {
                CustomerID = CustomerID,
                CustomerName = cus?.CustomerName,          
                ListCustomerType = _customerTypeCache.GetAll()
                    .Select(d => new SelectListItem { Text = d.CustomerTypeName, Value = d.CustomerTypeID.ToString() }).ToList(),
                ListStatus = _statusCache.GetStatusBySearchKey("Customer")
                    .Select(d => new SelectListItem { Text = d.StatusName, Value = d.ID.ToString() }).ToList()
            };
            return PartialView("~/Areas/Cate/Views/CustomerContact/_Add.cshtml", model);
        }

        #endregion

        #region Helper

        void LuuAnh(HttpPostedFileBase filebase, string filePath)
        {
            if (filebase != null && !string.IsNullOrEmpty(filePath))
            {
                if (!FileSecurityHelper.IsAllowedUpload(filebase.FileName) || !FileSecurityHelper.IsAllowedUpload(filePath))
                    return;
                filebase.SaveAs(HostingEnvironment.MapPath(filePath));
            }
        }

        #endregion
    }
}
