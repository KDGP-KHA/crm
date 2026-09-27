using CaptchaMvc.Interface;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Cate.Services;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Providers;

namespace Modules.Cate.Areas.Cate.Controllers
{
    public class InvoicesController : AppController
    {
        private readonly RM_ContractsCache _ContractsCache;
        private readonly RM_InvoicesCache _InvoicesCache;
        private readonly RM_TaskManagementCache _taskManagementCache;
        private readonly SysUserCache _sysUserCache;
        private readonly RM_LogTaskCache _filePathCache;
        private readonly string _ContractTitle = AppProcessor.Messagor.GetMessage("Contracts_Title");
        private readonly string _Title = AppProcessor.Messagor.GetMessage("Invoices_Title");
        private readonly string _File = AppProcessor.Messagor.GetMessage("File_Title");
        private readonly TaskFileService _fileService;
        private readonly ProjectTaskMailService _projectTaskMailService;
        private readonly string _thuMucLuuFile = "/Contents/imgs/Invoices/";


        public InvoicesController()
        {
            _ContractsCache = new RM_ContractsCache();
            _InvoicesCache = new RM_InvoicesCache();
            _taskManagementCache = new RM_TaskManagementCache();
            _sysUserCache = new SysUserCache();
            _filePathCache = new RM_LogTaskCache();
            _fileService = new TaskFileService();
            _projectTaskMailService = new ProjectTaskMailService();
        }


        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult GetByContract(int ContractID)
        {
            var model = _ContractsCache.GetById(ContractID);
            if (model == null)
            {
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_ContractTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            }
            return PartialView("_List", model);
        }

        /// <summary>
        /// Trả danh sách khách hàng theo điều kiện tìm kiếm và phân trang.
        /// </summary>
        /// <param name="searchModel">Điều kiện tìm kiếm khách hàng.</param>
        /// <returns>Dữ liệu JSON cho lưới danh sách khách hàng.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(RM_ContractsModel searchModel)
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

            var data = _InvoicesCache.Get(out var total, searchModel.ContractID, dataSearch);
            //if (data != null && data.Any())
            //{
            //    var statuses = _statusCache.GetStatusBySearchKey("Customer");
            //    foreach (var item in data)
            //    {
            //        var status = statuses.FirstOrDefault(s => s.ID == item.CustomerStatusID);
            //        if (status != null)
            //        {
            //            item.StatusName = status.StatusName;
            //            item.StatusClass = status.StatusClass;
            //        }
            //    }
            //}
            return Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }



        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add(int ContractID)
        {
            return PartialView("_Add", new RM_InvoicesModel { ContractID = ContractID });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        //[ValidateInput(false)]
        public ActionResult Add(RM_InvoicesModel model)
        {
            if (model.TotalPayment > 0 && model.TotalAmount <= 0)
            {
                model.TotalAmount = Math.Round(model.TotalPayment / (1.0 + (model.VAT_Rate / 100.0)), 2);
                model.VAT_Amount = model.TotalPayment - model.TotalAmount;
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

            if (!hasNewFile)
            {
                var fileLabel = AppProcessor.Messagor.GetMessage("FileAttach_Label");
                var reqMsg = AppProcessor.Messagor.GetMessage("Common_RequiredMessage");
                ModelState.AddModelError("Files", !string.IsNullOrEmpty(reqMsg) ? string.Format(reqMsg, fileLabel) : $"{fileLabel} không được để trống");
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_Invoices", model);
            }

            string response;
            DataTable dt = new DataTable();
            dt.Columns.Add("Val1", typeof(string));
            dt.Columns.Add("Val2", typeof(string));
            dt.Columns.Add("Val3", typeof(string));
            if (requestFile != null && requestFile.Count != 0)
            {
                for (int i = 0; i < requestFile.Count; i++)
                {
                    var saveResult = new FileProvider().UploadFile(requestFile[i], _thuMucLuuFile);
                    if (saveResult.ErrorCode == 1)
                    {
                        DataRow dataRow = dt.NewRow();
                        dataRow["Val1"] = _thuMucLuuFile + saveResult.FileName;
                        dt.Rows.Add(dataRow);
                    }
                }
            }
            Guid myGuid = Guid.NewGuid();
            model.InvoiceID = myGuid.ToString();
            var result = _InvoicesCache.Save(model, dt, User.UserName);

            if (result == 0)
                response = CreateMessage($"{_Title} [{model.Serial} - {model.Number}]",
                    EnumProcessType.Add,
                    EnumMsgIcon.Error);

            else if (result == -9)
                response = CreateMessage($"{_Title} [{model.Serial} - {model.Number}]",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_Title} [{model.Serial} - {model.Number}]",
                    EnumProcessType.Add,
                    EnumMsgIcon.Success);
            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(string id)
        {
            var model = _InvoicesCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_Title}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(RM_InvoicesModel model)
        {
            if (model.TotalPayment > 0 && model.TotalAmount <= 0)
            {
                model.TotalAmount = Math.Round(model.TotalPayment / (1.0 + (model.VAT_Rate / 100.0)), 2);
                model.VAT_Amount = model.TotalPayment - model.TotalAmount;
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

            if (!hasNewFile)
            {
                var existingFiles = _InvoicesCache.GetFilePaths(model.InvoiceID);
                if (existingFiles == null || !existingFiles.Any())
                {
                    var fileLabel = AppProcessor.Messagor.GetMessage("FileAttach_Label");
                    var reqMsg = AppProcessor.Messagor.GetMessage("Common_RequiredMessage");
                    ModelState.AddModelError("Files", !string.IsNullOrEmpty(reqMsg) ? string.Format(reqMsg, fileLabel) : $"{fileLabel} không được để trống");
                }
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_Invoices", model);
            }
            string response;

            DataTable dt = new DataTable();
            dt.Columns.Add("Val1", typeof(string));
            dt.Columns.Add("Val2", typeof(string));
            dt.Columns.Add("Val3", typeof(string));

            if (requestFile != null && requestFile.Count != 0)
            {
                for (int i = 0; i < requestFile.Count; i++)
                {
                    var saveResult = new FileProvider().UploadFile(requestFile[i], _thuMucLuuFile);
                    if (saveResult.ErrorCode == 1)
                    {
                        DataRow dataRow = dt.NewRow();
                        dataRow["Val1"] = _thuMucLuuFile + saveResult.FileName;
                        dt.Rows.Add(dataRow);
                    }
                }
            }

            var result = _InvoicesCache.Save(model, dt, User.UserName);

            if (result == 0)
                response = CreateMessage($"{_Title} [{model.Serial} - {model.Number}]",
                    EnumProcessType.Edit,
                    EnumMsgIcon.Error);

            else if (result == -9)
                response = CreateMessage($"{_Title} [{model.Serial} - {model.Number}]",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_Title} [{model.Serial} - {model.Number}]",
                    EnumProcessType.Edit,
                    EnumMsgIcon.Success);
            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(string id)
        {
            var model = _InvoicesCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_Title}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_Title} [{model.Serial} - {model.Number}]");
            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(RM_InvoicesModel model)
        {
            var deleted = _InvoicesCache.Delete(model, User.UserName);
            var response = CreateMessage($"{_Title} [{model.Serial} - {model.Number}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }


        #region File hóa đơn
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult GetFilePathsByInvoiceID(string id)
        {
            var data = _InvoicesCache.GetFilePaths(id);
            return PartialView("_FilePaths", data);
        }
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult DeleteFilePaths(string id)
        {
            var file = _InvoicesCache.GetFilePathByID(id);
            if (file == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_File}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });

            var result = _InvoicesCache.DeleteFilePath(file, "");
            var response = CreateMessage($"{_File} [{Path.GetFileName(file.URLFile)}]",
              EnumProcessType.Delete, result > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }
        #endregion
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Detail(string id)
        {
            var model = _InvoicesCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_Title}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            return PartialView("_Detail", model);
        }

    }
}