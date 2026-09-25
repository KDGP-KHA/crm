using Core.Cate.Biz;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Utils;

namespace Modules.Cate.Areas.Cate.Controllers
{
    /// <summary>
    /// Xử lý các luồng thêm, sửa, xóa và tra cứu hợp đồng.
    /// </summary>
    public class RM_ContractsController : AppController
    {
        private readonly RM_ContractsCache _contractsCache;
        private readonly RM_ContractRemindersCache _ContractRemindersCache;
        private readonly RM_ContractsBiz _contractsBiz;
        private readonly RM_CustomerCache _customerCache;
        private readonly RM_StatusCache _statusCache;
        private readonly RM_CustomerTypeCache _customerTypeCache;
        private readonly RM_ProductProjectCache _productProjectCache;
        private readonly RM_BillingCyclesCache _billingCyclesCache;
        private readonly RM_DigitalSalesCache _digitalSalesCache;
        private readonly string _folderImage = ConfigurationManager.AppSettings["AppImageRoot_Path"] ?? "/Contents/imgs";
        private readonly string _ContractsTitle = AppProcessor.Messagor.GetMessage("Contracts_Title");

        /// <summary>
        /// Khởi tạo cache và biz phục vụ xử lý hợp đồng.
        /// </summary>
        public RM_ContractsController()
        {
            _contractsCache = new RM_ContractsCache();
            _ContractRemindersCache = new RM_ContractRemindersCache();
            _contractsBiz = new RM_ContractsBiz();
            _customerCache = new RM_CustomerCache();
            _statusCache = new RM_StatusCache();
            _customerTypeCache = new RM_CustomerTypeCache();
            _productProjectCache = new RM_ProductProjectCache();
            _billingCyclesCache = new RM_BillingCyclesCache();
            _digitalSalesCache = new RM_DigitalSalesCache();
        }

        private void PopulateLists(RM_ContractsModel model)
        {
            model.ListCustomer = _customerCache.GetAll()
                .Select(d => new SelectListItem { Text = d.CustomerName, Value = d.CustomerID.ToString() })
                .ToList();
            model.ListStatus = _statusCache.GetStatusBySearchKey("Contract")
                .Select(s => new SelectListItem { Text = s.StatusName, Value = s.ID.ToString() })
                .ToList();
            model.ListBillingCycles = _billingCyclesCache.GetAll()
                .Select(s => new SelectListItem { Text = s.CycleName, Value = s.BillingCycleID.ToString() })
                .ToList();
        }

        private void LuuFile(HttpPostedFileBase file, string filePath)
        {
            if (file != null && !string.IsNullOrEmpty(filePath))
                file.SaveAs(HostingEnvironment.MapPath(filePath));
        }

        private void SaveFiles(List<HttpPostedFileBase> files, int contractID)
        {
            if (files == null || files.Count == 0) return;
            foreach (var file in files)
            {
                if (file == null || file.ContentLength == 0) continue;
                var fileName = UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName))
                    + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss")
                    + Path.GetExtension(file.FileName);
                var filePath = _folderImage + "/" + fileName;
                LuuFile(file, filePath);
                _contractsBiz.SaveFilePath(new RM_ContractFilePathModel
                {
                    FilePathID = 0,
                    ContractID = contractID,
                    FilePath = filePath
                }, User.UserName);
            }
        }

        /// <summary>
        /// Hiển thị màn hình danh sách hợp đồng và bộ lọc tìm kiếm.
        /// </summary>
        /// <returns>Màn hình danh sách hợp đồng.</returns>
        public ActionResult Index()
        {
            var model = new RM_ContractsSearchModel();
            model.Status = _statusCache.GetStatusBySearchKey("Contract") ?? new List<RM_StatusModel>();
            model.CustomerTypes = _customerTypeCache.GetAll() ?? new List<RM_CustomerTypeModel>();
            return View(model);
        }

        /// <summary>
        /// Trả danh sách hợp đồng theo điều kiện tìm kiếm và phân trang.
        /// </summary>
        /// <param name="model">Điều kiện tìm kiếm hợp đồng.</param>
        /// <returns>Dữ liệu JSON cho DataTable.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(RM_ContractsSearchModel model)
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
            model.UserName = User.UserName;
            var data = _contractsCache.Get(out var total, model, dataSearch);
            return Json(new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình thêm mới hợp đồng và khởi tạo sẵn khách hàng theo dự án nếu có.
        /// </summary>
        /// <param name="id">Mã dự án cần khởi tạo dữ liệu hợp đồng.</param>
        /// <returns>Popup thêm mới hợp đồng.</returns>
        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add(int? id, int? digitalSalesProductId)
        {
            var model = new RM_ContractsModel();
            if (digitalSalesProductId.GetValueOrDefault() > 0)
            {
                model.DigitalSalesProductID = digitalSalesProductId;
                model.ProductProjectID = 0;
            }
            else if (id.HasValue)
            {
                var productProject = _productProjectCache.GetById(id.Value);
                if (productProject == null)
                {
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage(_ContractsTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                    });
                }

                model.ProductProjectID = productProject.ProductProjectID;
            }
            PopulateLists(model);
            return PartialView("_Add", model);
        }

        /// <summary>
        /// Lưu thông tin hợp đồng mới từ màn hình.
        /// </summary>
        /// <param name="model">Dữ liệu hợp đồng cần lưu.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(RM_ContractsModel model)
        {
            if (model.ReminderType != "RECURRING")
            {
                ModelState.Remove("ReminderDayOfMonth");
            }
            else
            {
                if (model.ReminderDayOfMonth < 1 || model.ReminderDayOfMonth > 28)
                {
                    ModelState.AddModelError("ReminderDayOfMonth", AppProcessor.Messagor.GetMessage("ReminderDayOfMonth_Label") + " không hợp lệ");
                }
            }

            var requestFile = Request.Files;
            bool hasNewFile = false;
            if (requestFile != null && requestFile.Count > 0)
            {
                for (int i = 0; i < requestFile.Count; i++)
                {
                    if (requestFile[i] != null && !string.IsNullOrWhiteSpace(requestFile[i].FileName) && requestFile[i].ContentLength > 0)
                    {
                        hasNewFile = true;
                        break;
                    }
                }
            }
            bool hasModelFiles = model.DinhKemFile != null && model.DinhKemFile.Any(f => f != null && f.ContentLength > 0 && !string.IsNullOrWhiteSpace(f.FileName));
            if (hasNewFile || hasModelFiles)
            {
                ModelState.Remove("DinhKemFile");
            }
            else
            {
                var fileLabel = AppProcessor.Messagor.GetMessage("FileAttach_Label");
                var reqMsg = AppProcessor.Messagor.GetMessage("Common_RequiredMessage");
                ModelState.AddModelError("DinhKemFile", !string.IsNullOrEmpty(reqMsg) ? string.Format(reqMsg, fileLabel) : $"{fileLabel} không được để trống");
            }
            if (!ModelState.IsValid)
            {
                PopulateLists(model);
                return PartialView("_Contract", model);
            }

            DataTable ngayNhacs = new DataTable();
            ngayNhacs.Columns.Add(new DataColumn("Val1", typeof(string)));
            ngayNhacs.Columns.Add(new DataColumn("Val2", typeof(string)));
            ngayNhacs.Columns.Add(new DataColumn("Val3", typeof(string)));


            if (model.ReminderType == "SPECIFIC_DATE")
            {
                int soLuongNgayNhac = Convert.ToInt32(Request.Form.GetValues("SLDeXuat")?[0]);
                if (soLuongNgayNhac == 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage("Phải có ít nhất 1 ngày nhắc hợp đồng", EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    });
                }
                else
                {
                    for (int i = 0; i < soLuongNgayNhac; i++)
                    {
                        DataRow dataRow = ngayNhacs.NewRow();
                        dataRow["Val1"] = Request.Form.GetValues($"ReminderDate_{i}")?[0];
                        ngayNhacs.Rows.Add(dataRow);
                    }
                    var ngayTrungNhau = GetDuplicateVal1(ngayNhacs);
                    if (ngayTrungNhau.Count != 0)
                    {
                        return Json(new
                        {
                            status = false,
                            message = CreateMessage("Dữ liệu ngày nhắc đang bị trùng ở các ngày: " + string.Join(", ", ngayTrungNhau), EnumProcessType.NonFormat, EnumMsgIcon.Error)
                        });
                    }
                }
            }

            if (model.TotalAmount <= 0 && model.ContractValue > 0)
            {
                model.TotalAmount = model.ContractValue;
            }

            var result = _contractsCache.Save(model, ngayNhacs, User.UserName);
            if (result > 0)
            {
                SaveFiles(model.DinhKemFile, result);
                if (model.DigitalSalesProductID.GetValueOrDefault() > 0)
                {
                    var linked = _digitalSalesCache.LinkProductContract(model.DigitalSalesProductID.Value, result, User.UserName);
                    if (linked <= 0)
                    {
                        return Json(new
                        {
                            status = false,
                            message = CreateMessage(_ContractsTitle, EnumProcessType.NonFormat, EnumMsgIcon.Error)
                        });
                    }
                }
            }

            string response;
            if (result == 0) response = CreateMessage($"{_ContractsTitle} [{model.ContractName}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9) response = CreateMessage($"{_ContractsTitle} [{model.ContractName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else response = CreateMessage($"{_ContractsTitle} [{model.ContractName}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình cập nhật hợp đồng theo mã dữ liệu được chọn.
        /// </summary>
        /// <param name="id">Mã hợp đồng cần cập nhật.</param>
        /// <returns>Popup cập nhật hợp đồng hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id, int? digitalSalesProductId)
        {
            var model = _contractsCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage($"{_ContractsTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            model.DigitalSalesProductID = digitalSalesProductId;
            PopulateLists(model);
            model.ExistingFiles = _contractsBiz.GetFilePaths(id);
            model.ListContractReminders = _ContractRemindersCache.GetAll(id);
            var contractReminders = _ContractRemindersCache.GetAll(id);
            return PartialView("_Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin hợp đồng theo dữ liệu từ màn hình.
        /// </summary>
        /// <param name="model">Dữ liệu hợp đồng cần cập nhật.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(RM_ContractsModel model)
        {
            if (model.ReminderType != "RECURRING")
            {
                ModelState.Remove("ReminderDayOfMonth");
            }
            else
            {
                if (model.ReminderDayOfMonth < 1 || model.ReminderDayOfMonth > 28)
                {
                    ModelState.AddModelError("ReminderDayOfMonth", AppProcessor.Messagor.GetMessage("ReminderDayOfMonth_Label") + " không hợp lệ");
                }
            }

            var requestFileEdit = Request.Files;
            bool hasNewFileEdit = false;
            if (requestFileEdit != null && requestFileEdit.Count > 0)
            {
                for (int i = 0; i < requestFileEdit.Count; i++)
                {
                    if (requestFileEdit[i] != null && !string.IsNullOrWhiteSpace(requestFileEdit[i].FileName) && requestFileEdit[i].ContentLength > 0)
                    {
                        hasNewFileEdit = true;
                        break;
                    }
                }
            }
            bool hasModelFilesEdit = model.DinhKemFile != null && model.DinhKemFile.Any(f => f != null && f.ContentLength > 0 && !string.IsNullOrWhiteSpace(f.FileName));
            var existingFiles = _contractsBiz.GetFilePaths(model.ContractID);
            var remainingOldFiles = existingFiles != null 
                ? existingFiles.Count(f => model.DeletedFileIds == null || !model.DeletedFileIds.Contains(f.FilePathID)) 
                : 0;

            if (hasNewFileEdit || hasModelFilesEdit)
            {
                ModelState.Remove("DinhKemFile");
            }
            else if (remainingOldFiles > 0)
            {
                ModelState.Remove("DinhKemFile");
            }
            else
            {
                var fileLabel = AppProcessor.Messagor.GetMessage("FileAttach_Label");
                var reqMsg = AppProcessor.Messagor.GetMessage("Common_RequiredMessage");
                ModelState.AddModelError("DinhKemFile", !string.IsNullOrEmpty(reqMsg) ? string.Format(reqMsg, fileLabel) : $"{fileLabel} không được để trống");
            }
            if (!ModelState.IsValid)
            {
                PopulateLists(model);
                model.ExistingFiles = _contractsBiz.GetFilePaths(model.ContractID);
                return PartialView("_Contract", model);
            }

            DataTable ngayNhacs = new DataTable();
            ngayNhacs.Columns.Add(new DataColumn("Val1", typeof(string)));
            ngayNhacs.Columns.Add(new DataColumn("Val2", typeof(string)));
            ngayNhacs.Columns.Add(new DataColumn("Val3", typeof(string)));
            if (model.ReminderType == "SPECIFIC_DATE")
            {
                int soLuongNgayNhac = Convert.ToInt32(Request.Form.GetValues("SLDeXuat")?[0]);
                if (soLuongNgayNhac == 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage("Phải có ít nhất 1 ngày nhắc hợp đồng", EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    });
                }
                else
                {
                    for (int i = 0; i < soLuongNgayNhac; i++)
                    {
                        DataRow dataRow = ngayNhacs.NewRow();
                        dataRow["Val1"] = Request.Form.GetValues($"ReminderDate_{i}")?[0];
                        ngayNhacs.Rows.Add(dataRow);
                    }
                    var ngayTrungNhau = GetDuplicateVal1(ngayNhacs);
                    if (ngayTrungNhau.Count != 0)
                    {
                        return Json(new
                        {
                            status = false,
                            message = CreateMessage("Dữ liệu ngày nhắc đang bị trùng ở các ngày: " + string.Join(", ", ngayTrungNhau), EnumProcessType.NonFormat, EnumMsgIcon.Error)
                        });
                    }
                }
            }

            if (model.TotalAmount <= 0 && model.ContractValue > 0)
            {
                model.TotalAmount = model.ContractValue;
            }

            var result = _contractsCache.Save(model, ngayNhacs, User.UserName);
            if (result > 0)
            {
                SaveFiles(model.DinhKemFile, model.ContractID);
                if (model.DigitalSalesProductID.GetValueOrDefault() > 0)
                {
                    // Làm mới tổng doanh thu thực tế của hồ sơ DigitalSales sau khi sửa giá trị hợp đồng.
                    _digitalSalesCache.LinkProductContract(model.DigitalSalesProductID.Value, model.ContractID, User.UserName);
                }
                if (model.DeletedFileIds != null && model.DeletedFileIds.Any())
                {
                    foreach (var fileId in model.DeletedFileIds)
                    {
                        var file = _contractsBiz.GetFilePathById(fileId);

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
                            _contractsBiz.DeleteFilePath(fileId, User.UserName);
                        }
                    }
                }
            }

            string response;
            if (result == 0) response = CreateMessage($"{_ContractsTitle} [{model.ContractName}]", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9) response = CreateMessage($"{_ContractsTitle} [{model.ContractName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else response = CreateMessage($"{_ContractsTitle} [{model.ContractName}]", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình xác nhận xóa hợp đồng theo mã dữ liệu được chọn.
        /// </summary>
        /// <param name="id">Mã hợp đồng cần xóa.</param>
        /// <returns>Popup xác nhận xóa hoặc thông báo khi dữ liệu không tồn tại.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id, int? digitalSalesProductId)
        {
            var model = _contractsCache.GetById(id);
            if (model == null)
                return Json(new { status = true, message = CreateMessage($"{_ContractsTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            model.DigitalSalesProductID = digitalSalesProductId;
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_ContractsTitle} [{model.ContractName}]");
            return PartialView("_Delete", model);
        }

        /// <summary>
        /// Xóa thông tin hợp đồng theo dữ liệu được chọn.
        /// </summary>
        /// <param name="model">Dữ liệu hợp đồng cần xóa.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(RM_ContractsModel model)
        {
            var deleted = _contractsCache.Delete(model, User.UserName);
            var response = CreateMessage($"{_ContractsTitle} [{model.ContractName}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        /// <summary>
        /// Hiển thị popup xem chi tiết hợp đồng.
        /// </summary>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ViewDetail(int id)
        {
            var model = _contractsCache.GetById(id);
            if (model == null)
            {
                return Json(
                    new
                    {
                        status = false,
                        message = CreateMessage(_ContractsTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                    });
            }

            var files = _contractsBiz.GetFilePaths(id);
            model.ExistingFiles = files.Select(f => new RM_ContractFilePathModel
            {
                FilePathID = f.FilePathID,
                FilePath = f.FilePath
            }).ToList();

            return PartialView("_Detail", model);
        }
        private List<string> GetDuplicateVal1(DataTable dataTable)
        {
            var duplicates = new List<string>();
            var val1Counts = new Dictionary<string, int>();

            foreach (DataRow row in dataTable.Rows)
            {
                var val1 = row["Val1"].ToString();
                if (val1Counts.ContainsKey(val1))
                {
                    val1Counts[val1]++;
                }
                else
                {
                    val1Counts[val1] = 1;
                }
            }

            foreach (var kvp in val1Counts)
            {
                if (kvp.Value > 1)
                {
                    duplicates.Add(kvp.Key);
                }
            }

            return duplicates;
        }

    }
}
