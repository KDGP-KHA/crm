using ClosedXML.Excel;
using Core.Cate.Biz;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Cate.Services;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class DigitalSalesController : AppController
    {
        private readonly RM_DigitalSalesCache _salesCache;
        private readonly RM_CustomerCache _customerCache;
        private readonly Cate_ProductServiceCache _productServiceCache;
        private readonly MN_EmployeeCache _employeeCache;
        private readonly MN_BoPhanCache _departmentCache;
        private readonly RM_ContactPersonsCache _contactPersonCache;
        private readonly RM_ContractsCache _contractCache;
        private readonly RM_RolesCache _rolesCache;
        private readonly SysUserCache _userCache;
        private readonly SysUserBoPhanCache _userBoPhanCache;
        private readonly NotificationService _notificationService;
        private readonly RM_ReviewBatchItemCache _reviewBatchItemCache;
        private readonly RM_ReviewBatchItemBiz _reviewBatchItemBiz;
        private readonly SysConfigCache _sysConfigCache;
        private readonly RM_DigitalSalesWorkflowCache _workflowCache;

        private string _title => AppProcessor.Messagor.GetMessage("DigitalSales_Title");
        private readonly string _folderUpload = "/Contents/Uploads/DigitalSales";
        private string GetAppMessage(string labelKey, string defaultMessage = null)
        {
            var msg = AppProcessor.Messagor.GetMessage(labelKey);
            return !string.IsNullOrEmpty(msg) ? msg : (defaultMessage ?? labelKey);
        }


        private string FormatHtmlContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;
            var text = RM_DigitalSalesBiz.FixVietnameseMojibake(content.Trim());
            if (text.Contains("&lt;") && text.Contains("&gt;"))
            {
                text = HttpUtility.HtmlDecode(text);
            }
            return text;
        }

        public DigitalSalesController()
        {
            _salesCache = new RM_DigitalSalesCache();
            _customerCache = new RM_CustomerCache();
            _productServiceCache = new Cate_ProductServiceCache();
            _employeeCache = new MN_EmployeeCache();
            _departmentCache = new MN_BoPhanCache();
            _contactPersonCache = new RM_ContactPersonsCache();
            _contractCache = new RM_ContractsCache();
            _rolesCache = new RM_RolesCache();
            _userCache = new SysUserCache();
            _userBoPhanCache = new SysUserBoPhanCache();
            _notificationService = new NotificationService();
            _reviewBatchItemCache = new RM_ReviewBatchItemCache();
            _reviewBatchItemBiz = new RM_ReviewBatchItemBiz();
            _sysConfigCache = new SysConfigCache();
            _workflowCache = new RM_DigitalSalesWorkflowCache();
        }

        #region 1. List & Search
        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Index(int? customerId, byte? businessType, int? statusId)
        {
            var model = new RM_DigitalSalesSearchModel
            {
                CustomerID = customerId.GetValueOrDefault(0),
                BusinessType = businessType.GetValueOrDefault(0),
                StatusID = statusId.GetValueOrDefault(0),
                PageNumber = 1,
                PageSize = 20
            };

            PrepareSearchDropdowns(model);
            ViewBag.Title = _title;
            ViewBag.IsQTHT = IsUserQTHT(User.UserName);
            return View(model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(RM_DigitalSalesSearchModel model)
        {
            var search = Request.Form.GetValues("search[value]")?[0];
            var draw = Request.Form.GetValues("draw")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0] ?? "0");
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0] ?? "20");

            if (!string.IsNullOrWhiteSpace(search))
            {
                model.Keyword = search;
            }

            model.PageNumber = (startRec / (pageSize <= 0 ? 20 : pageSize)) + 1;
            model.PageSize = pageSize <= 0 ? 20 : pageSize;
            model.UserName = User.UserName;

            var data = _salesCache.LoadList(out int total, model);

            // Kiểm tra phân quyền sửa / xóa tối ưu: tránh lặp IsUserQTHT và N+1 queries
            bool isQTHT = IsUserQTHT(User.UserName);
            bool canSystemEdit = isQTHT || AppProcessor.Author.IsAllow(HttpContext, User.UserName, "Cate", "DigitalSales", "Edit");
            bool canSystemDelete = isQTHT || AppProcessor.Author.IsAllow(HttpContext, User.UserName, "Cate", "DigitalSales", "Delete");

            if (data != null && data.Count > 0)
            {
                if (isQTHT)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        data[i].CanEdit = canSystemEdit;
                        data[i].CanDelete = canSystemDelete;
                    }
                }
                else
                {
                    var currentUser = _userCache.GetByUserName(User.UserName);
                    int currentUserId = currentUser?.UserId ?? 0;

                    for (int i = 0; i < data.Count; i++)
                    {
                        var item = data[i];
                        bool hasRecordPerm = (!string.IsNullOrEmpty(item.CreatedBy) && item.CreatedBy.Equals(User.UserName, StringComparison.OrdinalIgnoreCase))
                                           || (currentUserId > 0 && item.AssignedEmployeeID == currentUserId);

                        if (!hasRecordPerm && item.DigitalSalesID > 0)
                        {
                            var members = item.Members;
                            if (members == null || members.Count == 0)
                            {
                                members = _salesCache.GetMembersBySalesID(item.DigitalSalesID);
                            }
                            if (members != null && members.Count > 0)
                            {
                                hasRecordPerm = members.Any(m => m.IsAM && (
                                    (!string.IsNullOrEmpty(m.UserName) && m.UserName.Equals(User.UserName, StringComparison.OrdinalIgnoreCase)) ||
                                    (currentUserId > 0 && m.UserID == currentUserId)
                                ));
                            }
                        }

                        item.CanEdit = canSystemEdit && hasRecordPerm;
                        item.CanDelete = canSystemDelete && hasRecordPerm;
                    }
                }
            }

            return Json(new
            {
                draw = Convert.ToInt32(draw ?? "1"),
                recordsTotal = total,
                recordsFiltered = total,
                data = data
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Export(string keyword, byte? businessType, int? statusID,
                                   int? departmentID, int? employeeID, string fromDate, string toDate, int? customerID,
                                   bool? isKeyProject, bool? isFollowed, string statusIDs = null)
        {
            var searchModel = new RM_DigitalSalesSearchModel
            {
                Keyword = keyword,
                BusinessType = businessType.GetValueOrDefault(0),
                StatusID = statusID.GetValueOrDefault(0),
                StatusIDs = statusIDs,
                DepartmentID = departmentID.GetValueOrDefault(0),
                EmployeeID = employeeID.GetValueOrDefault(0),
                FromDate = fromDate,
                ToDate = toDate,
                CustomerID = customerID.GetValueOrDefault(0),
                IsKeyProject = isKeyProject,
                IsFollowed = isFollowed,
                PageNumber = 1,
                PageSize = 999999,
                UserName = User.UserName
            };

            var data = _salesCache.LoadList(out _, searchModel);
            byte[] fileBytes = BuildExportWorkbook(data);
            string fileName = $"Danh_sach_SPDV_So_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private byte[] BuildExportWorkbook(List<RM_DigitalSalesModel> data)
        {
            byte[] result;
            using (var workbook = new XLWorkbook())
            {
                var sheetTitle = GetAppMessage("DigitalSales_Export_SheetName", "DS Kinh doanh SPDV So");
                var worksheet = workbook.Worksheets.Add(sheetTitle);

                string[] headers = new[]
                {
                    GetAppMessage("DigitalSales_Export_STT", "STT"),
                    GetAppMessage("DigitalSales_Export_BusinessType", "Loai hinh"),
                    GetAppMessage("DigitalSales_Export_Status", "Trang thai"),
                    GetAppMessage("DigitalSales_Export_Code", "Ma ho so"),
                    GetAppMessage("DigitalSales_Export_Title", "Tieu de co hoi / Du an"),
                    GetAppMessage("DigitalSales_Export_Customer", "Khach hang / Doanh nghiep"),
                    GetAppMessage("DigitalSales_Export_ContactPerson", "Nguoi lien he"),
                    GetAppMessage("DigitalSales_Export_ContactPhone", "SDT lien he"),
                    GetAppMessage("DigitalSales_Export_AM", "Nhan su AM chu tri"),
                    GetAppMessage("DigitalSales_Export_Department", "Don vi / Phong ban"),
                    GetAppMessage("DigitalSales_Export_ExpectedRevenue", "Doanh thu du kien (VND)"),
                    GetAppMessage("DigitalSales_Export_ActualRevenue", "Doanh thu thuc te (VND)"),
                    GetAppMessage("DigitalSales_Export_Probability", "Xac suat chot (%)"),
                    GetAppMessage("DigitalSales_Export_Products", "SPDV so quan tam"),
                    GetAppMessage("DigitalSales_Export_StartDate", "Ngay bat dau"),
                    GetAppMessage("DigitalSales_Export_ExpectedDate", "Ngay ket thuc du kien"),
                    GetAppMessage("DigitalSales_Export_CreatedBy", "Nguoi tao"),
                    GetAppMessage("DigitalSales_Export_CreatedDate", "Ngay tao")
                };

                for (int colIndex = 0; colIndex < headers.Length; colIndex++)
                {
                    var cell = worksheet.Cell(1, colIndex + 1);
                    cell.Value = headers[colIndex];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 11;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                worksheet.Row(1).Height = 26;

                if (data != null && data.Count > 0)
                {
                    int stt = 1;
                    for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
                    {
                        var item = data[rowIndex];
                        int r = rowIndex + 2;

                        worksheet.Cell(r, 1).Value = stt++;
                        worksheet.Cell(r, 2).Value = item.BusinessType == 2 ? GetAppMessage("DigitalSales_BusinessType_Project", "Du an") : GetAppMessage("DigitalSales_BusinessType_Opportunity", "Co hoi");
                        worksheet.Cell(r, 3).Value = item.StatusName ?? "";
                        worksheet.Cell(r, 4).Value = item.Code ?? "";
                        worksheet.Cell(r, 5).Value = item.Title ?? "";
                        worksheet.Cell(r, 6).Value = item.CustomerName ?? "";
                        worksheet.Cell(r, 7).Value = item.ContactPersonName ?? "";
                        worksheet.Cell(r, 8).Value = item.ContactPersonPhone ?? "";
                        worksheet.Cell(r, 9).Value = item.AssignedEmployeeName ?? "";
                        worksheet.Cell(r, 10).Value = item.DepartmentName ?? "";

                        if (item.TotalExpectedRevenue.HasValue)
                        {
                            worksheet.Cell(r, 11).Value = item.TotalExpectedRevenue.Value;
                            worksheet.Cell(r, 11).Style.NumberFormat.Format = "#,##0";
                        }
                        else
                        {
                            worksheet.Cell(r, 11).Value = 0;
                        }

                        if (item.TotalActualRevenue.HasValue)
                        {
                            worksheet.Cell(r, 12).Value = item.TotalActualRevenue.Value;
                            worksheet.Cell(r, 12).Style.NumberFormat.Format = "#,##0";
                        }
                        else
                        {
                            worksheet.Cell(r, 12).Value = 0;
                        }

                        worksheet.Cell(r, 13).Value = (item.ClosingProbability ?? 0) + "%";
                        worksheet.Cell(r, 14).Value = item.ProductServiceNames ?? "";
                        worksheet.Cell(r, 15).Value = item.StartDate.HasValue ? item.StartDate.Value.ToString("dd/MM/yyyy") : "";
                        worksheet.Cell(r, 16).Value = item.ExpectedDate.HasValue ? item.ExpectedDate.Value.ToString("dd/MM/yyyy") : "";
                        worksheet.Cell(r, 17).Value = item.CreatedByName ?? item.CreatedBy ?? "";
                        worksheet.Cell(r, 18).Value = item.CreatedDate != DateTime.MinValue ? item.CreatedDate.ToString("dd/MM/yyyy HH:mm") : "";

                        worksheet.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        worksheet.Cell(r, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        worksheet.Cell(r, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(r, 18).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        for (int c = 1; c <= headers.Length; c++)
                        {
                            worksheet.Cell(r, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            worksheet.Cell(r, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#D9D9D9");
                        }
                    }
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    result = stream.ToArray();
                }
            }
            return result;
        }
        #endregion

        #region 2. Add & Edit
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add(int? customerId, byte? businessType)
        {
            var model = new RM_DigitalSalesModel
            {
                Code = _salesCache.GenerateNextCode(),
                BusinessType = businessType ?? 1,
                StatusID = 1, // Default: Business status 1
                CustomerID = customerId.GetValueOrDefault(0),
                StartDate = DateTime.Today,
                ExpectedDate = DateTime.Today.AddMonths(1),
                ClosingProbability = 50
            };

            var currentUser = _userCache.GetByUserName(User.UserName);
            if (currentUser != null)
            {
                model.AssignedEmployeeID = currentUser.UserId;
                model.DepartmentID = GetDepartmentIdByUserId(currentUser.UserId);
            }

            PrepareSalesDropdowns(model);
            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Add(RM_DigitalSalesModel model, HttpPostedFileBase fileUpload)
        {
            if (model.CustomerID <= 0)
            {
                ModelState.AddModelError("CustomerID", GetAppMessage("DigitalSales_Msg_CustomerRequired"));
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError("Title", GetAppMessage("DigitalSales_Msg_TitleRequired"));
            }

            if (!model.AssignedEmployeeID.HasValue || model.AssignedEmployeeID.Value <= 0)
            {
                ModelState.AddModelError("AssignedEmployeeID", GetAppMessage("DigitalSales_Msg_AMRequired"));
            }

            if (!ModelState.IsValid)
            {
                PrepareSalesDropdowns(model);
                return PartialView("_DigitalSales", model);
            }

            var uploadedFiles = new List<string>();
            if (Request.Files.Count > 0)
            {
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        var path = SaveUploadedFile(file);
                        if (!string.IsNullOrEmpty(path))
                        {
                            uploadedFiles.Add(path);
                        }
                    }
                }
            }

            if (uploadedFiles.Count > 0)
            {
                model.FileAttach = string.Join(";", uploadedFiles);
            }

            if ((!model.DepartmentID.HasValue || model.DepartmentID.Value <= 0) && model.AssignedEmployeeID.HasValue)
            {
                model.DepartmentID = GetDepartmentIdByUserId(model.AssignedEmployeeID.Value);
            }

            if (string.IsNullOrWhiteSpace(model.Note))
            {
                var rawNote = Request.Unvalidated.Form["Note"];
                if (!string.IsNullOrWhiteSpace(rawNote))
                {
                    model.Note = rawNote;
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Note))
            {
                model.Note = FormatHtmlContent(model.Note);
            }

            var rawSignDate = Request.Form["ContractSignDate"];
            if (!string.IsNullOrWhiteSpace(rawSignDate) && DateTime.TryParseExact(rawSignDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sd))
            {
                model.ContractSignDate = sd;
            }

            var rawContractValue = Request.Form["ContractValue"];
            if (!string.IsNullOrWhiteSpace(rawContractValue))
            {
                var cleanVal = rawContractValue.Trim().Replace(" ", "").Replace(",", "");
                if (decimal.TryParse(cleanVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cv))
                {
                    model.ContractValue = cv;
                }
            }

            var id = _salesCache.Save(model, User.UserName);
            if (id > 0)
            {
                return Json(new
                {
                    status = true,
                    id = id,
                    message = CreateMessage(_title, EnumProcessType.Add, EnumMsgIcon.Success)
                });
            }

            return Json(new
            {
                status = false,
                message = CreateMessage(_title, EnumProcessType.Add, EnumMsgIcon.Error)
            });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id)
        {
            if (!HasDetailPermission(id, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                }, JsonRequestBehavior.AllowGet);
            }

            var model = _salesCache.GetByID(id);
            if (model == null)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            }

            model.Note = FormatHtmlContent(model.Note);
            PrepareSalesDropdowns(model);
            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RM_DigitalSalesModel model, HttpPostedFileBase fileUpload)
        {
            if (model.DigitalSalesID <= 0)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            }

            if (!HasDetailPermission(model.DigitalSalesID, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            if (model.CustomerID <= 0)
            {
                ModelState.AddModelError("CustomerID", GetAppMessage("DigitalSales_Msg_CustomerRequired"));
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError("Title", GetAppMessage("DigitalSales_Msg_TitleRequired"));
            }

            if (!model.AssignedEmployeeID.HasValue || model.AssignedEmployeeID.Value <= 0)
            {
                ModelState.AddModelError("AssignedEmployeeID", GetAppMessage("DigitalSales_Msg_AMRequired"));
            }

            if (!ModelState.IsValid)
            {
                PrepareSalesDropdowns(model);
                return PartialView("_DigitalSales", model);
            }

            var uploadedFiles = new List<string>();
            if (Request.Files.Count > 0)
            {
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        var path = SaveUploadedFile(file);
                        if (!string.IsNullOrEmpty(path))
                        {
                            uploadedFiles.Add(path);
                        }
                    }
                }
            }

            if (uploadedFiles.Count > 0)
            {
                var newPaths = string.Join(";", uploadedFiles);
                model.FileAttach = !string.IsNullOrEmpty(model.FileAttach)
                    ? model.FileAttach + ";" + newPaths
                    : newPaths;
            }

            if ((!model.DepartmentID.HasValue || model.DepartmentID.Value <= 0) && model.AssignedEmployeeID.HasValue)
            {
                model.DepartmentID = GetDepartmentIdByUserId(model.AssignedEmployeeID.Value);
            }

            if (string.IsNullOrWhiteSpace(model.Note))
            {
                var rawNote = Request.Unvalidated.Form["Note"];
                if (!string.IsNullOrWhiteSpace(rawNote))
                {
                    model.Note = rawNote;
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Note))
            {
                model.Note = FormatHtmlContent(model.Note);
            }

            var rawSignDate = Request.Form["ContractSignDate"];
            if (!string.IsNullOrWhiteSpace(rawSignDate) && DateTime.TryParseExact(rawSignDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sd))
            {
                model.ContractSignDate = sd;
            }

            var rawContractValue = Request.Form["ContractValue"];
            if (!string.IsNullOrWhiteSpace(rawContractValue))
            {
                var cleanVal = rawContractValue.Trim().Replace(" ", "").Replace(",", "");
                if (decimal.TryParse(cleanVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cv))
                {
                    model.ContractValue = cv;
                }
            }

            var result = _salesCache.Save(model, User.UserName);
            if (result > 0)
            {
                return Json(new
                {
                    status = true,
                    message = CreateMessage(_title, EnumProcessType.Edit, EnumMsgIcon.Success)
                });
            }

            return Json(new
            {
                status = false,
                message = CreateMessage(_title, EnumProcessType.Edit, EnumMsgIcon.Error)
            });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            if (!HasDetailPermission(id, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            var result = _salesCache.Delete(id, User.UserName);
            if (result > 0)
            {
                return Json(new
                {
                    status = true,
                    message = CreateMessage(_title, EnumProcessType.Delete, EnumMsgIcon.Success)
                });
            }

            return Json(new
            {
                status = false,
                message = CreateMessage(_title, EnumProcessType.Delete, EnumMsgIcon.Error)
            });
        }

        #region Attachment Operations
        [HttpGet]
        public ActionResult DownloadAttachment(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return HttpNotFound();
            }

            var cleanPath = filePath.Trim().Replace("~", "");
            if (!cleanPath.StartsWith("/Contents/", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidPath") }, JsonRequestBehavior.AllowGet);
            }

            var physicalPath = HostingEnvironment.MapPath(cleanPath);
            if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
            {
                var fileNameOnly = Path.GetFileName(cleanPath);
                var subFolder = DateTime.Now.ToString("yyyyMM");
                var fallbackPath = HostingEnvironment.MapPath($"{_folderUpload}/{subFolder}/{fileNameOnly}");
                if (System.IO.File.Exists(fallbackPath))
                {
                    physicalPath = fallbackPath;
                }
                else
                {
                    var fallbackPathRoot = HostingEnvironment.MapPath($"{_folderUpload}/{fileNameOnly}");
                    if (System.IO.File.Exists(fallbackPathRoot))
                    {
                        physicalPath = fallbackPathRoot;
                    }
                    else
                    {
                        return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_FileNotFound") }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            var fileName = Path.GetFileName(physicalPath);
            var mimeType = MimeMapping.GetMimeMapping(physicalPath);
            if (Path.GetExtension(physicalPath).Equals(".webp", StringComparison.OrdinalIgnoreCase))
            {
                mimeType = "image/webp";
            }

            return File(physicalPath, mimeType, fileName);
        }

        [HttpGet]
        public ActionResult ViewAttachment(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return HttpNotFound();
            }

            var cleanPath = filePath.Trim().Replace("~", "");
            if (!cleanPath.StartsWith("/Contents/", StringComparison.OrdinalIgnoreCase))
            {
                return HttpNotFound();
            }

            var physicalPath = HostingEnvironment.MapPath(cleanPath);
            if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
            {
                var fileNameOnly = Path.GetFileName(cleanPath);
                var subFolder = DateTime.Now.ToString("yyyyMM");
                var fallbackPath = HostingEnvironment.MapPath($"{_folderUpload}/{subFolder}/{fileNameOnly}");
                if (System.IO.File.Exists(fallbackPath))
                {
                    physicalPath = fallbackPath;
                }
                else
                {
                    var fallbackPathRoot = HostingEnvironment.MapPath($"{_folderUpload}/{fileNameOnly}");
                    if (System.IO.File.Exists(fallbackPathRoot))
                    {
                        physicalPath = fallbackPathRoot;
                    }
                    else
                    {
                        return HttpNotFound();
                    }
                }
            }

            var mimeType = MimeMapping.GetMimeMapping(physicalPath);
            if (Path.GetExtension(physicalPath).Equals(".webp", StringComparison.OrdinalIgnoreCase))
            {
                mimeType = "image/webp";
            }

            return File(physicalPath, mimeType);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult UploadAttachment(int id, IEnumerable<HttpPostedFileBase> fileUpload)
        {
            if (id <= 0)
            {
                return Json(new { status = false, message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            if (!HasDetailPermission(id, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            var model = _salesCache.GetByID(id);
            if (model == null)
            {
                return Json(new { status = false, message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            var files = fileUpload?.Where(f => f != null && f.ContentLength > 0).ToList() ?? new List<HttpPostedFileBase>();
            if (Request.Files.Count > 0 && files.Count == 0)
            {
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var f = Request.Files[i];
                    if (f != null && f.ContentLength > 0)
                    {
                        files.Add(f);
                    }
                }
            }

            if (files.Count == 0)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_UploadEmpty") });
            }

            var uploadedPaths = new List<string>();
            foreach (var f in files)
            {
                var p = SaveUploadedFile(f);
                if (!string.IsNullOrEmpty(p))
                {
                    uploadedPaths.Add(p);
                }
            }

            if (uploadedPaths.Count == 0)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_UploadEmpty") });
            }

            var currentFiles = string.IsNullOrEmpty(model.FileAttach)
                ? new List<string>()
                : model.FileAttach.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            currentFiles.AddRange(uploadedPaths);
            model.FileAttach = string.Join(";", currentFiles.Distinct());

            var saveResult = _salesCache.Save(model, User.UserName);
            if (saveResult > 0)
            {
                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Msg_UploadSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = CreateMessage(_title, EnumProcessType.Edit, EnumMsgIcon.Error)
            });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult DeleteAttachment(int id, string filePath)
        {
            if (id <= 0)
            {
                return Json(new { status = false, message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            if (!HasDetailPermission(id, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            var model = _salesCache.GetByID(id);
            if (model == null)
            {
                return Json(new { status = false, message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            if (string.IsNullOrEmpty(model.FileAttach))
            {
                return Json(new { status = true, message = GetAppMessage("DigitalSales_Msg_DeleteFileSuccess") });
            }

            var currentFiles = model.FileAttach.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.Equals(s, filePath.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            model.FileAttach = currentFiles.Count > 0 ? string.Join(";", currentFiles) : null;

            var saveResult = _salesCache.Save(model, User.UserName);
            if (saveResult > 0)
            {
                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Msg_DeleteFileSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = CreateMessage(_title, EnumProcessType.Edit, EnumMsgIcon.Error)
            });
        }
        #endregion
        #endregion

        #region 3. Detail 360
        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Detail(int id, int? reviewBatchID)
        {
            try
            {
                var model = _salesCache.GetByID(id, User.UserName);
                if (model == null)
                {
                    return RedirectToAction("Index");
                }

                model.Note = FormatHtmlContent(model.Note);
                model.ReviewHistory = BuildDigitalSalesReviewHistory(id);

                if (string.IsNullOrEmpty(model.CreatedByName) && !string.IsNullOrEmpty(model.CreatedBy))
                {
                    model.CreatedByName = _userCache.GetByUserName(model.CreatedBy)?.FullName;
                }

                ViewBag.Title = $"{AppProcessor.Messagor.GetMessage("DigitalSales_RecordPrefix")}: {model.Code} - {model.Title}";
                ViewBag.ReviewBatchID = reviewBatchID.GetValueOrDefault(0);
                try
                {
                    ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
                }
                catch
                {
                    ViewBag.CanEdit = false;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [AjaxOnly]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetReviewHistoryPartial(int? id)
        {
            return PartialView(
                "~/Areas/Cate/Views/ReviewBatchItem/_ReviewHistory.cshtml",
                BuildDigitalSalesReviewHistory(id.GetValueOrDefault()));
        }

        private List<RM_ReviewHistoryModel> BuildDigitalSalesReviewHistory(int digitalSalesID)
        {
            var histories = _reviewBatchItemCache.GetDigitalSalesHistory(digitalSalesID) ?? new List<RM_ReviewHistoryModel>();
            foreach (var item in histories)
            {
                item.ExistingFiles = _reviewBatchItemBiz.GetFilePaths(item.ReviewHistoryID) ?? new List<RM_ReviewBatchFilePathModel>();
                item.CanEdit = string.Equals(item.CreatedBy, User.UserName, StringComparison.OrdinalIgnoreCase);
            }
            return histories;
        }

        [HttpPost]
        [AjaxOnly]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ToggleKeyProject(int id, bool isKeyProject)
        {
            if (!HasDetailPermission(id, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Toggle_NoPermission")
                });
            }

            var success = _salesCache.ToggleKeyProject(id, isKeyProject, User.UserName);
            if (success)
            {
                string msg = isKeyProject
                    ? GetAppMessage("DigitalSales_ToggleKeyProject_Success_On")
                    : GetAppMessage("DigitalSales_ToggleKeyProject_Success_Off");
                return Json(new { status = true, message = msg });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Toggle_Error")
            });
        }

        [HttpPost]
        [AjaxOnly]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult ToggleFollow(int id, bool isFollowed)
        {
            var success = _salesCache.ToggleFollow(id, isFollowed, User.UserName);
            if (success)
            {
                string msg = isFollowed
                    ? GetAppMessage("DigitalSales_ToggleFollow_Success_On")
                    : GetAppMessage("DigitalSales_ToggleFollow_Success_Off");
                return Json(new { status = true, message = msg });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Toggle_Error")
            });
        }

        #region 3.1 Partial Component Async Endpoints
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetMetricsPartial(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();
            return PartialView("_DetailMetrics", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetOverviewPartial(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();
            model.Note = FormatHtmlContent(model.Note);
            if (string.IsNullOrEmpty(model.CreatedByName) && !string.IsNullOrEmpty(model.CreatedBy))
            {
                model.CreatedByName = _userCache.GetByUserName(model.CreatedBy)?.FullName;
            }
            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailOverview", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetMembersPartial(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();
            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailMembers", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetAttachmentsPartial(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();
            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailAttachments", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProductsPartial(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();
            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailProducts", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetTrackingPartial(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();
            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailTracking", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetTimelinePartial(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();
            return PartialView("_DetailTimeline", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetDiscussionsPartial(int id, byte? activityType = null)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null) return HttpNotFound();

            var viewModel = new RM_DigitalSalesModel
            {
                DigitalSalesID = model.DigitalSalesID,
                Code = model.Code,
                Title = model.Title,
                CustomerID = model.CustomerID,
                CustomerName = model.CustomerName,
                StatusID = model.StatusID,
                StatusName = model.StatusName,
                Members = model.Members,
                CreatedBy = model.CreatedBy,
                CreatedByName = model.CreatedByName,
                Activities = _salesCache.GetActivitiesBySalesID(id, activityType)
            };

            ViewBag.CurrentFilter = activityType;
            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailDiscussions", viewModel);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetMembersForMention(int id)
        {
            var members = _salesCache.GetMembersBySalesID(id);
            var result = members.Select(m => new
            {
                userId = m.UserID,
                userName = m.UserName,
                fullName = m.FullName,
                roleTitle = m.RoleTitle
            }).ToList();

            return Json(new { status = true, data = result }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult PostDiscussion(int digitalSalesId, string content, string mentionedUserIds, string mentionedNames)
        {
            if (digitalSalesId <= 0)
            {
                return Json(new { status = false, message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            var digitalSales = _salesCache.GetByID(digitalSalesId, User.UserName);
            if (digitalSales == null)
            {
                return Json(new { status = false, message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Discussion_ContentRequired") });
            }

            var plainTextContent = Regex.Replace(content ?? string.Empty, "<.*?>", " ");
            plainTextContent = System.Web.HttpUtility.HtmlDecode(plainTextContent).Trim();
            var words = string.IsNullOrWhiteSpace(plainTextContent)
                ? new string[0]
                : plainTextContent.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 500)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Discussion_WordLimitExceeded", "Nội dung trao đổi không được vượt quá 500 từ!") });
            }

            var uploadedFiles = new List<ActivityAttachmentItem>();
            if (Request.Files.Count > 0)
            {
                var forbiddenExts = new[] { 
                    ".exe", ".dll", ".bat", ".cmd", ".vbs", ".ps1", 
                    ".sh", ".com", ".msi", ".vbe", ".jse", ".wsf", 
                    ".wsh", ".scr", ".pif", ".jar", ".app", ".gadget" 
                };

                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                        if (forbiddenExts.Contains(ext))
                        {
                            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidFileFormat") });
                        }

                        if (file.ContentLength > 52428800) // 50MB
                        {
                            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_FileSizeExceeded", "Dung lượng tệp đính kèm không được vượt quá 50MB!") });
                        }

                        var relPath = SaveUploadedFile(file, i);
                        if (!string.IsNullOrEmpty(relPath))
                        {
                            uploadedFiles.Add(new ActivityAttachmentItem
                            {
                                FileName = Path.GetFileName(file.FileName),
                                FilePath = relPath,
                                FileSize = file.ContentLength,
                                FileSizeFormatted = file.ContentLength > 1048576 
                                    ? $"{(file.ContentLength / 1048576.0):0.0} MB" 
                                    : $"{(file.ContentLength / 1024.0):0.0} KB",
                                Extension = ext,
                                IsImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" }.Contains(ext)
                            });
                        }
                    }
                }
            }

            var requestedMentionUserIds = new HashSet<int>();
            foreach (var userIdValue in (mentionedUserIds ?? string.Empty).Split(','))
            {
                int userId;
                if (int.TryParse(userIdValue.Trim(), out userId) && userId > 0)
                {
                    requestedMentionUserIds.Add(userId);
                }
            }

            var contentMentionUserIds = new HashSet<int>();
            var mentionMatches = Regex.Matches(
                content,
                @"data-user-id\s*=\s*[""'](?<userId>\d+)[""']",
                RegexOptions.IgnoreCase);
            foreach (Match mentionMatch in mentionMatches)
            {
                int userId;
                if (int.TryParse(mentionMatch.Groups["userId"].Value, out userId) && userId > 0)
                {
                    contentMentionUserIds.Add(userId);
                }
            }
            requestedMentionUserIds.IntersectWith(contentMentionUserIds);

            var mentionedMembers = (_salesCache.GetMembersBySalesID(digitalSalesId)
                ?? new List<RM_DigitalSalesMemberModel>())
                .Where(member => requestedMentionUserIds.Contains(member.UserID)
                    && !string.IsNullOrWhiteSpace(member.UserName))
                .GroupBy(member => member.UserID)
                .Select(group => group.First())
                .ToList();

            var activity = new RM_DigitalSalesActivityModel
            {
                DigitalSalesID = digitalSalesId,
                ActivityType = 1,
                Content = content.Trim(),
                Attachments = uploadedFiles.Count > 0 ? Newtonsoft.Json.JsonConvert.SerializeObject(uploadedFiles) : null,
                MentionedUserIDs = mentionedMembers.Count > 0
                    ? string.Join(",", mentionedMembers.Select(member => member.UserID))
                    : null,
                MentionedNames = mentionedMembers.Count > 0
                    ? string.Join(",", mentionedMembers.Select(member => member.FullName))
                    : null
            };

            var saveRes = _salesCache.AddActivity(activity, User.UserName);
            if (saveRes > 0)
            {
                var notificationReceivers = mentionedMembers
                    .Select(member => member.UserName)
                    .Where(userName => !userName.Equals(User.UserName, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (notificationReceivers.Count > 0)
                {
                    var currentUser = _userCache.GetByUserName(User.UserName);
                    var actionByFullName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.FullName)
                        ? currentUser.FullName
                        : User.UserName;
                    var notificationTitle = string.Format(
                        GetAppMessage(
                            "DigitalSales_Discussion_MentionNotificationTitle",
                            "{0} đã nhắc đến bạn trong trao đổi"),
                        actionByFullName);
                    var notificationContent = string.Format(
                        GetAppMessage(
                            "DigitalSales_Discussion_MentionNotificationContent",
                            "{0}: {1}"),
                        digitalSales.Title,
                        plainTextContent);

                    _notificationService.PushDigitalSalesNotification(
                        digitalSalesId,
                        notificationTitle,
                        notificationContent,
                        notificationReceivers,
                        "DIGITAL_SALES_DISCUSSION_MENTION",
                        User.UserName,
                        actionByFullName);
                }

                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Discussion_PostSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = CreateMessage(_title, EnumProcessType.Create, EnumMsgIcon.Error)
            });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult DeleteDiscussion(int activityId)
        {
            if (activityId <= 0)
            {
                return Json(new { status = false, message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error) });
            }

            var res = _salesCache.DeleteActivity(activityId, User.UserName);
            if (res > 0)
            {
                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Discussion_DeleteSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Msg_NoPermission")
            });
        }
        #endregion
        #endregion

        #region 4. Change Status Gatekeeper
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ChangeStatusModal(int id)
        {
            if (!HasDetailPermission(id, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                }, JsonRequestBehavior.AllowGet);
            }

            var sales = _salesCache.GetByID(id);
            if (sales == null)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage(_title, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            }

            var allStatuses = _salesCache.GetStatusList(null);
            var excludedCodes = GetExcludedStatusCodes();
            var model = new RM_DigitalSalesChangeStatusViewModel
            {
                DigitalSalesID = sales.DigitalSalesID,
                Title = sales.Title,
                CurrentBusinessType = sales.BusinessType,
                CurrentBusinessTypeName = sales.BusinessType == 1
                    ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity")
                    : (sales.BusinessType == 2
                        ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project")
                        : sales.BusinessTypeName),
                CurrentStatusName = sales.StatusName,
                AvailableStatuses = allStatuses
                    .Where(s => s.StatusID != sales.StatusID && !IsStatusExcluded(s, excludedCodes))
                    .Select(s => new SelectListItem
                    {
                        Value = s.StatusID.ToString(),
                        Text = $"[{(s.BusinessType == 1 ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity") : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project"))}] {s.StatusName}"
                    }).ToList()
            };

            return PartialView("_ChangeStatusModal", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeStatus(RM_DigitalSalesChangeStatusViewModel model, HttpPostedFileBase attachmentFile)
        {
            try
            {
            if (model.DigitalSalesID <= 0)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidData") });
            }

            if (!HasDetailPermission(model.DigitalSalesID, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            if (!ModelState.IsValid)
            {
                PrepareChangeStatusForm(model);
                return PartialView("_ChangeStatusForm", model);
            }

            var currentSales = _salesCache.GetByID(model.DigitalSalesID);
            var excludedCodes = GetExcludedStatusCodes();
            var newStatus = _salesCache.GetStatusList(null)?.FirstOrDefault(s => s.StatusID == model.NewStatusID);
            if (IsStatusExcluded(newStatus, excludedCodes) || (currentSales != null && currentSales.StatusID == model.NewStatusID))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_StatusInvalid")
                });
            }

            string attachmentPath = null;
            var uploadedFiles = new List<ActivityAttachmentItem>();

            if (Request.Files != null && Request.Files.Count > 0)
            {
                var forbiddenExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".exe", ".bat", ".cmd", ".sh", ".msi", ".dll", ".com", ".vbs", ".ps1"
                };

                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                        if (forbiddenExts.Contains(ext))
                        {
                            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidFileFormat") });
                        }

                        if (file.ContentLength > 52428800) // 50MB
                        {
                            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_FileSizeExceeded", "Dung lượng tệp đính kèm không được vượt quá 50MB!") });
                        }

                        var relPath = SaveUploadedFile(file, i);
                        if (!string.IsNullOrEmpty(relPath))
                        {
                            if (attachmentPath == null)
                            {
                                attachmentPath = relPath;
                            }

                            uploadedFiles.Add(new ActivityAttachmentItem
                            {
                                FileName = Path.GetFileName(file.FileName),
                                FilePath = relPath,
                                FileSize = file.ContentLength,
                                FileSizeFormatted = file.ContentLength > 1048576 
                                    ? $"{(file.ContentLength / 1048576.0):0.0} MB" 
                                    : $"{(file.ContentLength / 1024.0):0.0} KB",
                                Extension = ext,
                                IsImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" }.Contains(ext)
                            });
                        }
                    }
                }
            }
            else if (attachmentFile != null && attachmentFile.ContentLength > 0)
            {
                attachmentPath = SaveUploadedFile(attachmentFile);
            }

            string attachmentPayload = attachmentPath;
            if (uploadedFiles.Count > 0)
            {
                attachmentPayload = Newtonsoft.Json.JsonConvert.SerializeObject(uploadedFiles);
            }

            var code = _salesCache.ChangeStatus(model.DigitalSalesID, model.NewStatusID, model.Note, attachmentPayload, User.UserName);

            if (code == 1)
            {
                // 1. Cập nhật tệp đính kèm trực tiếp vào Activity chuyển trạng thái (ActivityType = 2) vừa được tạo bởi SP
                if (uploadedFiles.Count > 0)
                {
                    try
                    {
                        var attachmentsJson = Newtonsoft.Json.JsonConvert.SerializeObject(uploadedFiles);
                        _salesCache.UpdateLatestStatusChangeActivityAttachments(model.DigitalSalesID, attachmentsJson, User.UserName);
                    }
                    catch (Exception ex)
                    {
                        AppProcessor.Logger.Error(ex);
                    }
                }

                // 1.1 Tự động đánh dấu hoàn thành toàn bộ tiến trình và công việc con của trạng thái trước đó
                // Theo đặc tả: Khi chuyển trạng thái thì những Tiến trình và Công việc con của tiến trình trong Quy trình
                // của Trạng thái trước đó đều được đánh dấu hoàn thành.
                // Thời gian hoàn thành là thời gian thực hiện Xác nhận chuyển trạng thái.
                // Người hoàn thành là người thực hiện Xác nhận chuyển trạng thái.
                // Các nội dung này được lưu vào Audit Log của Tiến trình và công việc con.
                int oldStatusId = currentSales != null ? currentSales.StatusID : 0;
                if (oldStatusId > 0 && oldStatusId != model.NewStatusID)
                {
                    try
                    {
                        var allTasks = _salesCache.GetTrackingTasks(model.DigitalSalesID);
                        if (allTasks != null && allTasks.Count > 0)
                        {
                            var oldTasks = allTasks.Where(t => t.StatusID.HasValue && t.StatusID.Value != model.NewStatusID).ToList();
                            var uncompletedItems = oldTasks.SelectMany(t => (t.TodoList ?? new List<RM_DigitalSalesTrackingModel>()).Concat(new[] { t }))
                                                           .Where(x => x.Status != 3)
                                                           .ToList();

                            foreach (var item in uncompletedItems)
                            {
                                _salesCache.UpdateTrackingStatus(
                                    item.TrackingID,
                                    3, // Hoàn thành
                                    "Tự động hoàn thành khi chuyển trạng thái sang " + (newStatus?.StatusName ?? ("ID " + model.NewStatusID)),
                                    null,
                                    null,
                                    null,
                                    User.UserName
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppProcessor.Logger.Error(ex);
                    }
                }

                // 2. Lưu danh sách tiến trình vào Checklist (RM_DigitalSalesTracking)
                try
                {
                    List<ChangeStatusTrackingItemDTO> items = null;
                    if (!string.IsNullOrWhiteSpace(model.TrackingItemsJson))
                    {
                        items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ChangeStatusTrackingItemDTO>>(model.TrackingItemsJson);
                    }

                    // Fallback 1: Nếu TrackingItemsJson rỗng nhưng người dùng có chọn quy trình (SelectedProcessID)
                    if ((items == null || items.Count == 0) && model.SelectedProcessID.HasValue && model.SelectedProcessID.Value > 0)
                    {
                        var progs = _workflowCache.GetProgressesByProcess(model.SelectedProcessID.Value);
                        if (progs != null && progs.Count > 0)
                        {
                            items = new List<ChangeStatusTrackingItemDTO>();
                            int sortIdx = 1;
                            var today = DateTime.Today;
                            foreach (var p in progs.Where(x => x.IsActive).OrderBy(x => x.SortOrder))
                            {
                                var days = p.DefaultDurationDays > 0 ? p.DefaultDurationDays : 3;
                                items.Add(new ChangeStatusTrackingItemDTO
                                {
                                    ProcessID = model.SelectedProcessID.Value,
                                    ProgressID = p.ProgressID,
                                    TaskName = p.ProgressName,
                                    SortOrder = p.SortOrder > 0 ? p.SortOrder : sortIdx++,
                                    StartDate = today,
                                    DurationDays = days,
                                    Deadline = today.AddDays(days),
                                    AssignedUserID = null,
                                    IsCustomTask = false
                                });
                            }
                        }
                    }

                    // Fallback 2: Nếu cả TrackingItemsJson và SelectedProcessID đều rỗng, tự động lấy quy trình đầu tiên của NewStatusID
                    if ((items == null || items.Count == 0) && model.NewStatusID > 0)
                    {
                        int totalProcCount = 0;
                        var allProcs = _workflowCache.GetProcesses(out totalProcCount, statusId: model.NewStatusID);
                        var activeProcs = allProcs?.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
                        if (activeProcs != null && activeProcs.Count > 0)
                        {
                            var firstProc = activeProcs[0];
                            var progs = _workflowCache.GetProgressesByProcess(firstProc.ProcessID);
                            if (progs != null && progs.Count > 0)
                            {
                                items = new List<ChangeStatusTrackingItemDTO>();
                                int sortIdx = 1;
                                var today = DateTime.Today;
                                foreach (var p in progs.Where(x => x.IsActive).OrderBy(x => x.SortOrder))
                                {
                                    var days = p.DefaultDurationDays > 0 ? p.DefaultDurationDays : 3;
                                    items.Add(new ChangeStatusTrackingItemDTO
                                    {
                                        ProcessID = firstProc.ProcessID,
                                        ProgressID = p.ProgressID,
                                        TaskName = p.ProgressName,
                                        SortOrder = p.SortOrder > 0 ? p.SortOrder : sortIdx++,
                                        StartDate = today,
                                        DurationDays = days,
                                        Deadline = today.AddDays(days),
                                        AssignedUserID = null,
                                        IsCustomTask = false
                                    });
                                }
                            }
                        }
                    }

                    if (items != null && items.Count > 0)
                    {
                        var currentTasks = _salesCache.GetTrackingTasks(model.DigitalSalesID) ?? new List<RM_DigitalSalesTrackingModel>();

                        // Xóa các task cha của trạng thái mới (bất kể trạng thái là chưa làm hay đã hoàn thành trước đó) để thiết lập danh sách mới
                        var tasksToDelete = currentTasks.Where(t => t.StatusID == model.NewStatusID && (!t.ParentID.HasValue || t.ParentID.Value <= 0)).ToList();
                        foreach (var ot in tasksToDelete)
                        {
                            _salesCache.DeleteTracking(ot.TrackingID, User.UserName);
                        }

                        // Tìm processId mặc định của trạng thái nếu có task bị thiếu ProcessID
                        int? defaultProcId = model.SelectedProcessID;
                        if (!defaultProcId.HasValue || defaultProcId.Value <= 0)
                        {
                            int totalDefaultProcs = 0;
                            var defaultProc = _workflowCache.GetProcesses(out totalDefaultProcs, statusId: model.NewStatusID)?.FirstOrDefault(p => p.IsActive);
                            if (defaultProc != null) defaultProcId = defaultProc.ProcessID;
                        }

                        int sort = 1;
                        foreach (var it in items)
                        {
                            if (string.IsNullOrWhiteSpace(it.TaskName)) continue;

                            var duration = it.DurationDays.HasValue && it.DurationDays.Value > 0 ? it.DurationDays.Value : 3;
                            var startDate = it.StartDate ?? DateTime.Today;
                            var deadline = it.Deadline ?? startDate.AddDays(duration);
                            var procId = (it.ProcessID.HasValue && it.ProcessID.Value > 0) ? it.ProcessID : defaultProcId;

                            var trackingModel = new RM_DigitalSalesTrackingModel
                            {
                                TrackingID = 0,
                                DigitalSalesID = model.DigitalSalesID,
                                ProcessID = procId,
                                ProgressID = it.ProgressID,
                                TaskName = it.TaskName.Trim(),
                                AssignedUserID = it.AssignedUserID,
                                StartDate = startDate,
                                DurationDays = duration,
                                Deadline = deadline,
                                Status = 1, // Chưa thực hiện
                                IsCustomTask = it.IsCustomTask,
                                SortOrder = it.SortOrder > 0 ? it.SortOrder : sort++,
                                ResultNote = null,
                                AttachmentFile = null
                            };

                            _salesCache.SaveTracking(trackingModel, User.UserName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppProcessor.Logger.Error(ex);
                }

                var updated = _salesCache.GetByID(model.DigitalSalesID, User.UserName);
                return Json(new
                {
                    status = true,
                    code = 1,
                    message = GetAppMessage("DigitalSales_Msg_ChangeStatusSuccess"),
                    businessType = updated?.BusinessType,
                    businessTypeName = updated?.BusinessType == 2
                        ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project")
                        : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity"),
                    statusName = updated?.StatusName
                });
            }
            else if (code == -3)
            {
                return Json(new
                {
                    status = false,
                    code = -3,
                    message = GetAppMessage("DigitalSales_Msg_ReqProductBeforeProject")
                });
            }
            else if (code == -4)
            {
                return Json(new
                {
                    status = false,
                    code = -4,
                    message = GetAppMessage("DigitalSales_Msg_ReqMemberBeforeProject")
                });
            }
            else if (code == -1)
            {
                return Json(new
                {
                    status = false,
                    code = -1,
                    message = GetAppMessage("DigitalSales_Msg_NotFound")
                });
            }
            else if (code == -2)
            {
                return Json(new
                {
                    status = false,
                    code = -2,
                    message = GetAppMessage("DigitalSales_Msg_StatusInvalid")
                });
            }

            return Json(new
            {
                status = false,
                code = 0,
                message = GetAppMessage("DigitalSales_Msg_ChangeStatusFail")
            });
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return Json(new
                {
                    status = false,
                    code = -999,
                    message = "Lỗi hệ thống: " + ex.Message,
                    detail = ex.StackTrace
                });
            }
        }
        #endregion

        private void PrepareChangeStatusForm(RM_DigitalSalesChangeStatusViewModel model)
        {
            var sales = _salesCache.GetByID(model.DigitalSalesID);
            if (sales != null)
            {
                model.Title = sales.Title;
                model.CurrentBusinessType = sales.BusinessType;
                model.CurrentBusinessTypeName = sales.BusinessTypeName;
                model.CurrentStatusName = sales.StatusName;
            }

            var excludedCodes = GetExcludedStatusCodes();
            model.AvailableStatuses = (_salesCache.GetStatusList(null) ?? new List<RM_DigitalSalesStatusModel>())
                .Where(s => (sales == null || s.StatusID != sales.StatusID) && !IsStatusExcluded(s, excludedCodes))
                .Select(s => new SelectListItem
                {
                    Value = s.StatusID.ToString(),
                    Text = $"[{(s.BusinessType == 1 ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity") : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project"))}] {s.StatusName}"
                }).ToList();
        }

        private HashSet<string> GetExcludedStatusCodes()
        {
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UNCAPTURED", "1" };
            try
            {
                var cfg = _sysConfigCache.GetViaKey("DIGITAL_SALES_EXCLUDE_CHANGE_STATUS_CODES");
                if (cfg != null && !string.IsNullOrWhiteSpace(cfg.ConfigValue))
                {
                    var parts = cfg.ConfigValue.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var code = p.Trim();
                        if (!string.IsNullOrEmpty(code))
                        {
                            excluded.Add(code);
                        }
                    }
                }
            }
            catch
            {
                // Fallback an toàn
            }
            return excluded;
        }

        private bool IsStatusExcluded(RM_DigitalSalesStatusModel status, HashSet<string> excludedCodes)
        {
            if (status == null) return false;
            if (excludedCodes == null || excludedCodes.Count == 0) return false;

            if (!string.IsNullOrEmpty(status.StatusCode) && excludedCodes.Contains(status.StatusCode)) return true;
            if (excludedCodes.Contains(status.StatusID.ToString())) return true;
            if (!string.IsNullOrEmpty(status.StatusName) && excludedCodes.Contains(status.StatusName)) return true;

            return false;
        }

        #region 4.1 Workflow Processes & Progresses for Status Change
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetWorkflowProcessesAndProgresses(int statusId, int digitalSalesId)
        {
            try
            {
                if (statusId <= 0)
                {
                    return Json(new { status = false, message = "StatusID không hợp lệ" }, JsonRequestBehavior.AllowGet);
                }

                // 1. Lấy danh sách quy trình theo StatusID
                var processes = _workflowCache.GetProcesses(out _, search: null, businessType: null, statusId: statusId);
                var activeProcesses = processes != null
                    ? processes.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList()
                    : new List<RM_DigitalSalesProcessModel>();

                // 2. Lấy danh sách nhân sự (CHỈ gồm thành viên thuộc hồ sơ kinh doanh số và AM phụ trách)
                var membersList = new List<object>();
                var existingUserIds = new HashSet<int>();

                if (digitalSalesId > 0)
                {
                    var sales = _salesCache.GetByID(digitalSalesId);
                    if (sales != null && sales.AssignedEmployeeID.HasValue && sales.AssignedEmployeeID.Value > 0 && !existingUserIds.Contains(sales.AssignedEmployeeID.Value))
                    {
                        var amUserId = sales.AssignedEmployeeID.Value;
                        existingUserIds.Add(amUserId);
                        membersList.Add(new
                        {
                            userId = amUserId,
                            fullName = !string.IsNullOrWhiteSpace(sales.AssignedEmployeeName) ? sales.AssignedEmployeeName : ("ID " + amUserId),
                            userName = "",
                            roleTitle = "AM chủ trì",
                            isProjectMember = true
                        });
                    }

                    var salesMembers = _salesCache.GetMembersBySalesID(digitalSalesId);
                    if (salesMembers != null)
                    {
                        foreach (var m in salesMembers)
                        {
                            if (m.UserID > 0 && !existingUserIds.Contains(m.UserID))
                            {
                                existingUserIds.Add(m.UserID);
                                membersList.Add(new
                                {
                                    userId = m.UserID,
                                    fullName = m.FullName,
                                    userName = m.UserName,
                                    roleTitle = m.RoleTitle,
                                    isProjectMember = true
                                });
                            }
                        }
                    }
                }

                // 3. Chuẩn bị thông tin quy trình & tiến trình
                var processList = new List<object>();
                int? defaultSelectedProcessId = null;
                var defaultProgressList = new List<object>();

                if (activeProcesses.Count > 0)
                {
                    var firstProcess = activeProcesses[0];
                    defaultSelectedProcessId = firstProcess.ProcessID;

                    foreach (var p in activeProcesses)
                    {
                        var progs = _workflowCache.GetProgressesByProcess(p.ProcessID);
                        var activeProgs = progs != null ? progs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList() : new List<RM_DigitalSalesProgressModel>();

                        processList.Add(new
                        {
                            processId = p.ProcessID,
                            processCode = p.ProcessCode,
                            processName = p.ProcessName,
                            description = p.Description,
                            progressCount = activeProgs.Count
                        });

                        if (p.ProcessID == firstProcess.ProcessID)
                        {
                            int progSort = 1;
                            var today = DateTime.Today;
                            foreach (var pr in activeProgs)
                            {
                                var defaultDays = pr.DefaultDurationDays > 0 ? pr.DefaultDurationDays : 3;
                                var defaultDeadline = today.AddDays(defaultDays);
                                defaultProgressList.Add(new
                                {
                                    progressId = pr.ProgressID,
                                    processId = p.ProcessID,
                                    taskName = pr.ProgressName,
                                    sortOrder = pr.SortOrder > 0 ? pr.SortOrder : progSort++,
                                    startDate = today.ToString("yyyy-MM-dd"),
                                    durationDays = defaultDays,
                                    defaultDurationDays = defaultDays,
                                    deadline = defaultDeadline.ToString("yyyy-MM-dd"),
                                    assignedUserId = (int?)null
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    status = true,
                    processCount = activeProcesses.Count,
                    processes = processList,
                    selectedProcessId = defaultSelectedProcessId,
                    progresses = defaultProgressList,
                    members = membersList
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProgressesByProcess(int processId, int digitalSalesId)
        {
            try
            {
                if (processId <= 0)
                {
                    return Json(new { status = false, message = "ProcessID không hợp lệ" }, JsonRequestBehavior.AllowGet);
                }

                var progs = _workflowCache.GetProgressesByProcess(processId);
                var activeProgs = progs != null ? progs.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList() : new List<RM_DigitalSalesProgressModel>();

                var progressList = new List<object>();
                int progSort = 1;
                var today = DateTime.Today;
                foreach (var pr in activeProgs)
                {
                    var defaultDays = pr.DefaultDurationDays > 0 ? pr.DefaultDurationDays : 3;
                    var defaultDeadline = today.AddDays(defaultDays);
                    progressList.Add(new
                    {
                        progressId = pr.ProgressID,
                        processId = processId,
                        taskName = pr.ProgressName,
                        sortOrder = pr.SortOrder > 0 ? pr.SortOrder : progSort++,
                        startDate = today.ToString("yyyy-MM-dd"),
                        durationDays = defaultDays,
                        defaultDurationDays = defaultDays,
                        deadline = defaultDeadline.ToString("yyyy-MM-dd"),
                        assignedUserId = (int?)null
                    });
                }

                return Json(new
                {
                    status = true,
                    processId = processId,
                    progresses = progressList
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        private List<SelectListItem> GetProjectMemberSelectList(int digitalSalesId, int? currentAssignedUserId = null)
        {
            var result = new List<SelectListItem>();
            if (digitalSalesId <= 0) return result;

            var existingUserIds = new HashSet<int>();
            var sales = _salesCache.GetByID(digitalSalesId, User.UserName);

            // 1. AM chủ trì từ hồ sơ kinh doanh số
            if (sales != null && sales.AssignedEmployeeID.HasValue && sales.AssignedEmployeeID.Value > 0)
            {
                var amUserId = sales.AssignedEmployeeID.Value;
                existingUserIds.Add(amUserId);
                var amName = !string.IsNullOrWhiteSpace(sales.AssignedEmployeeName) ? sales.AssignedEmployeeName : ("ID " + amUserId);
                result.Add(new SelectListItem
                {
                    Value = amUserId.ToString(),
                    Text = $"{amName} (AM chủ trì)"
                });
            }

            // 2. Các thành viên đã được add trong hồ sơ KDGP
            var salesMembers = _salesCache.GetMembersBySalesID(digitalSalesId);
            if (salesMembers != null)
            {
                foreach (var m in salesMembers)
                {
                    if (m.UserID > 0 && !existingUserIds.Contains(m.UserID))
                    {
                        existingUserIds.Add(m.UserID);
                        var roleInfo = !string.IsNullOrWhiteSpace(m.RoleTitle) ? $" ({m.RoleTitle})" : (!string.IsNullOrWhiteSpace(m.UserName) ? $" ({m.UserName})" : "");
                        result.Add(new SelectListItem
                        {
                            Value = m.UserID.ToString(),
                            Text = $"{m.FullName}{roleInfo}"
                        });
                    }
                }
            }

            // 3. Dự phòng: Nếu đang sửa tiến trình/công việc con đã có người phụ trách từ trước mà người đó chưa có trong list
            if (currentAssignedUserId.HasValue && currentAssignedUserId.Value > 0 && !existingUserIds.Contains(currentAssignedUserId.Value))
            {
                var fallbackUser = _userCache.GetById(currentAssignedUserId.Value);
                if (fallbackUser != null)
                {
                    result.Add(new SelectListItem
                    {
                        Value = fallbackUser.UserId.ToString(),
                        Text = $"{fallbackUser.FullName} ({fallbackUser.UserName})"
                    });
                }
            }

            return result;
        }

        private void PrepareTrackingForm(int digitalSalesId = 0, int? currentAssignedUserId = null)
        {
            ViewBag.UserList = digitalSalesId > 0
                ? GetProjectMemberSelectList(digitalSalesId, currentAssignedUserId)
                : (_userCache.GetAll()?.Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = $"{u.FullName} ({u.UserName})"
                }).ToList() ?? new List<SelectListItem>());
        }

        private void PrepareMemberFormData(int digitalSalesId)
        {
            var departments = GetAccessibleDepartments() ?? new List<MN_BoPhanModel>();
            var departmentIds = new HashSet<int>(departments.Select(d => d.BoPhan_ID));
            var existingUserIds = new HashSet<int>((_salesCache.GetMembersBySalesID(digitalSalesId) ?? new List<RM_DigitalSalesMemberModel>()).Select(m => m.UserID));
            ViewBag.Employees = (_employeeCache.GetAll() ?? new List<MN_EmployeeModel>())
                .Where(e => departmentIds.Contains(e.BoPhan_ID) && !existingUserIds.Contains(e.Employee_ID))
                .OrderBy(e => e.FullName).ToList();
            ViewBag.Departments = departments;
            ViewBag.Roles = _rolesCache.GetAll() ?? new List<RM_RolesModel>();
        }

        #region 5. Products & Revenue
        private List<SelectListItem> GetProductSelectList()
        {
            try
            {
                // Ưu tiên 1: Lấy danh sách sản phẩm qua GetAllChild() (chứa ProductServiceID, CodeProduct, ShortNameProduct, NameProduct)
                var rawList = _productServiceCache.GetAllChild();
                if (rawList != null && rawList.Count > 0)
                {
                    var productItems = rawList
                        .Where(p => p.NodeType == "P" && p.ProductServiceID > 0 && p.IsActived)
                        .Select(p =>
                        {
                            var shortName = (p.ShortNameProduct ?? "").Trim();
                            var fullName = (p.NameProduct ?? "").Trim();
                            var code = (p.CodeProduct ?? "").Trim();

                            string displayText;
                            if (!string.IsNullOrEmpty(shortName) && !string.IsNullOrEmpty(fullName))
                            {
                                displayText = string.Equals(shortName, fullName, StringComparison.OrdinalIgnoreCase)
                                    ? fullName
                                    : $"{shortName} - {fullName}";
                            }
                            else if (!string.IsNullOrEmpty(fullName))
                            {
                                displayText = fullName;
                            }
                            else
                            {
                                displayText = shortName;
                            }

                            if (!string.IsNullOrEmpty(code) && !displayText.Contains($"({code})"))
                            {
                                displayText = $"{displayText} ({code})";
                            }

                            return new SelectListItem
                            {
                                Value = p.ProductServiceID.ToString(),
                                Text = displayText
                            };
                        })
                        .Where(item => item.Value != "0" && !string.IsNullOrWhiteSpace(item.Text))
                        .OrderBy(item => item.Text)
                        .ToList();

                    if (productItems.Count > 0)
                    {
                        return productItems;
                    }
                }

                // Fallback 2: Nếu GetAllChild chưa có, lấy qua GetAll() và ánh xạ an toàn cả pID và ProductServiceID
                var fallbackList = _productServiceCache.GetAll();
                if (fallbackList != null && fallbackList.Count > 0)
                {
                    return fallbackList
                        .Where(p => p.NodeType == "P" || p.pID > 0 || p.ProductServiceID > 0)
                        .Select(p =>
                        {
                            var id = p.ProductServiceID > 0 ? p.ProductServiceID : p.pID;
                            var shortName = (p.ShortNameProduct ?? "").Trim();
                            var fullName = (!string.IsNullOrWhiteSpace(p.NameProduct)
                                ? p.NameProduct
                                : (p.DisplayName ?? "").Replace("&nbsp;", "")).Trim();
                            var code = (p.CodeProduct ?? "").Trim();

                            string displayText;
                            if (!string.IsNullOrEmpty(shortName) && !string.IsNullOrEmpty(fullName))
                            {
                                displayText = string.Equals(shortName, fullName, StringComparison.OrdinalIgnoreCase)
                                    ? fullName
                                    : $"{shortName} - {fullName}";
                            }
                            else if (!string.IsNullOrEmpty(fullName))
                            {
                                displayText = fullName;
                            }
                            else
                            {
                                displayText = shortName;
                            }

                            if (!string.IsNullOrEmpty(code) && !displayText.Contains($"({code})"))
                            {
                                displayText = $"{displayText} ({code})";
                            }

                            return new SelectListItem
                            {
                                Value = id.ToString(),
                                Text = displayText
                            };
                        })
                        .Where(item => item.Value != "0" && !string.IsNullOrWhiteSpace(item.Text))
                        .OrderBy(item => item.Text)
                        .ToList();
                }

                return new List<SelectListItem>();
            }
            catch
            {
                return new List<SelectListItem>();
            }
        }

        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddProductModal(int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                }, JsonRequestBehavior.AllowGet);
            }

            var model = new RM_DigitalSalesProductModel
            {
                DigitalSalesID = digitalSalesId,
                Quantity = 1,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1)
            };

            ViewBag.ProductList = GetProductSelectList();

            return PartialView("_ProductModal", model);
        }

        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditProductModal(int id, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                }, JsonRequestBehavior.AllowGet);
            }

            var products = _salesCache.GetProductsBySalesID(digitalSalesId);
            var model = products.FirstOrDefault(p => p.SalesProductID == id);
            if (model == null)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage(AppProcessor.Messagor.GetMessage("DigitalSales_Product"), EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            }

            ViewBag.ProductList = GetProductSelectList();

            return PartialView("_ProductModal", model);
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveProduct(RM_DigitalSalesProductModel model, int? ProductServiceID, int? DigitalSalesID)
        {
            if (model == null) model = new RM_DigitalSalesProductModel();

            // Fallback ProductServiceID từ tham số hoặc Request.Form
            if (model.ProductServiceID <= 0)
            {
                if (ProductServiceID.HasValue && ProductServiceID.Value > 0)
                {
                    model.ProductServiceID = ProductServiceID.Value;
                }
                else if (int.TryParse(Request["ProductServiceID"], out int psId) && psId > 0)
                {
                    model.ProductServiceID = psId;
                }
            }

            // Fallback DigitalSalesID từ tham số hoặc Request.Form
            if (model.DigitalSalesID <= 0)
            {
                if (DigitalSalesID.HasValue && DigitalSalesID.Value > 0)
                {
                    model.DigitalSalesID = DigitalSalesID.Value;
                }
                else if (int.TryParse(Request["DigitalSalesID"], out int dsId) && dsId > 0)
                {
                    model.DigitalSalesID = dsId;
                }
            }

            if (model.DigitalSalesID <= 0)
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_InvalidSalesRecord")
                });
            }

            if (model.ProductServiceID <= 0)
            {
                ModelState.AddModelError("ProductServiceID", GetAppMessage("DigitalSales_Msg_ProductRequired"));
            }

            if (!HasDetailPermission(model.DigitalSalesID, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ProductList = GetProductSelectList();
                return PartialView("_ProductForm", model);
            }

            var id = _salesCache.SaveProduct(model, User.UserName);
            if (id > 0)
            {
                return Json(new
                {
                    status = true,
                    id = id,
                    message = GetAppMessage("DigitalSales_Msg_SaveProductSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Msg_SaveProductFail")
            });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteProduct(int id, int? salesId = null)
        {
            if (salesId.HasValue && salesId.Value > 0 && !HasDetailPermission(salesId.Value, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            var result = _salesCache.DeleteProduct(id, User.UserName);
            if (result > 0)
            {
                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Msg_DeleteProductSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Msg_DeleteProductFail")
            });
        }
        #endregion

        #region 6. Project Members
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddMemberModal(int digitalSalesId)
        {
            try
            {
                if (!HasDetailPermission(digitalSalesId, User.UserName))
                {
                    return Json(new
                    {
                        status = false,
                        message = GetAppMessage("DigitalSales_Msg_NoPermission")
                    }, JsonRequestBehavior.AllowGet);
                }

                var model = new RM_DigitalSalesMemberFormModel
                {
                    DigitalSalesID = digitalSalesId
                };

                var accessibleDepts = GetAccessibleDepartments() ?? new List<MN_BoPhanModel>();
                var accessibleDeptIds = new HashSet<int>(accessibleDepts.Select(d => d.BoPhan_ID));

                // Bao gồm cả các đơn vị con thuộc các đơn vị đang quản lý
                var allDepts = _departmentCache.GetAll() ?? new List<MN_BoPhanModel>();
                foreach (var d in allDepts)
                {
                    if (d.BoPhanCha_ID.HasValue && accessibleDeptIds.Contains(d.BoPhanCha_ID.Value))
                    {
                        accessibleDeptIds.Add(d.BoPhan_ID);
                    }
                }

                // Lọc danh sách nhân sự CHỈ thuộc các đơn vị người dùng đang quản lý
                var allEmployees = _employeeCache.GetAll() ?? new List<MN_EmployeeModel>();
                var employees = allEmployees.Where(e => accessibleDeptIds.Contains(e.BoPhan_ID)).ToList();

                // Fallback: nếu danh sách nhân sự rỗng, load theo _userCache dựa trên các đơn vị quản lý hoặc lấy danh sách nhân sự
                if (employees.Count == 0)
                {
                    var userList = new List<SysUserModel>();
                    foreach (var dId in accessibleDeptIds)
                    {
                        try
                        {
                            var uList = _userCache.GetByBoPhanAndChucVu(dId, null);
                            if (uList != null) userList.AddRange(uList);
                        }
                        catch { }
                    }

                    if (userList.Count > 0)
                    {
                        employees = userList.GroupBy(u => u.UserId).Select(g =>
                        {
                            var u = g.First();
                            return new MN_EmployeeModel
                            {
                                Employee_ID = u.UserId ?? 0,
                                FullName = u.FullName,
                                BoPhan_ID = accessibleDeptIds.FirstOrDefault(),
                                TenBoPhan = u.OfficeName
                            };
                        }).Where(e => e.Employee_ID > 0).ToList();
                    }
                    else
                    {
                        employees = allEmployees.Take(100).ToList();
                    }
                }

                List<RM_DigitalSalesMemberModel> existingMembers;
                try
                {
                    existingMembers = _salesCache.GetMembersBySalesID(digitalSalesId) ?? new List<RM_DigitalSalesMemberModel>();
                }
                catch
                {
                    existingMembers = new List<RM_DigitalSalesMemberModel>();
                }

                var existingUserIds = new HashSet<int>(existingMembers.Select(m => m.UserID));
                employees.ForEach(employee => employee.IsSaleMember = existingUserIds.Contains(employee.Employee_ID));

                var existingRolesByUserId = new Dictionary<int, string>();
                if (existingMembers.Count > 0)
                {
                    foreach (var grp in existingMembers.GroupBy(m => m.UserID))
                    {
                        if (grp.Key > 0)
                        {
                            var roleTitles = grp.Select(m => m.RoleTitle).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct();
                            existingRolesByUserId[grp.Key] = string.Join(", ", roleTitles);
                        }
                    }
                }
                ViewBag.ExistingRoles = existingRolesByUserId;
                ViewBag.Employees = employees;
                ViewBag.Departments = accessibleDepts;

                List<RM_RolesModel> roles = null;
                try
                {
                    roles = _rolesCache.GetAll();
                }
                catch { }

                if (roles == null || roles.Count == 0)
                {
                    roles = new List<RM_RolesModel>
                    {
                        new RM_RolesModel { RoleID = 1, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_AM") },
                        new RM_RolesModel { RoleID = 2, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_TechSolution") },
                        new RM_RolesModel { RoleID = 3, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_DeploymentExpert") },
                        new RM_RolesModel { RoleID = 4, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_PocSupport") },
                        new RM_RolesModel { RoleID = 5, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_ProjectAdmin") },
                        new RM_RolesModel { RoleID = 6, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_CustomerCare") }
                    };
                }
                ViewBag.Roles = roles;

                return PartialView("_MemberModal", model);
            }
            catch (Exception ex)
            {
                return Content(string.Format("<div class='alert alert-danger p-3'>{0}: {1}</div>", GetAppMessage("DigitalSales_Msg_SaveMemberFail"), HttpUtility.HtmlEncode(ex.Message)));
            }
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveMembers(RM_DigitalSalesMemberFormModel model)
        {
            if (model.DigitalSalesID <= 0)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidSalesRecord") });
            }

            if (!HasDetailPermission(model.DigitalSalesID, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            if (!ModelState.IsValid || model.EmployeeIDs == null || model.EmployeeIDs.Length == 0)
            {
                if (model.EmployeeIDs == null || model.EmployeeIDs.Length == 0)
                {
                    ModelState.AddModelError("EmployeeIDs", GetAppMessage("DigitalSales_Msg_MemberRequired"));
                }
                PrepareMemberFormData(model.DigitalSalesID);
                return PartialView("_MemberForm", model);
            }

            var allRoles = _rolesCache.GetAll() ?? new List<RM_RolesModel>();
            var selectedRoleIds = new HashSet<int>(model.RoleIDs ?? new int[0]);
            var roleNames = allRoles.Where(r => selectedRoleIds.Contains(r.RoleID)).Select(r => r.RoleName).ToList();
            if (!string.IsNullOrWhiteSpace(model.CustomRole)) roleNames.Add(model.CustomRole.Trim());
            var roleTitle = roleNames.Count > 0 ? string.Join(", ", roleNames.Distinct()) : GetAppMessage("DigitalSales_Role_Member");
            var savedCount = 0;
            foreach (var employeeId in model.EmployeeIDs.Distinct().Where(id => id > 0))
            {
                var member = new RM_DigitalSalesMemberModel { DigitalSalesID = model.DigitalSalesID, UserID = employeeId, RoleTitle = roleTitle, IsAM = model.IsAM, Note = model.Note, IsActive = true };
                if (_salesCache.SaveMember(member, User.UserName) > 0) savedCount++;
            }

            return Json(new
            {
                status = savedCount > 0,
                message = savedCount > 0
                    ? string.Format(GetAppMessage("DigitalSales_Msg_SaveMembersMultiSuccess"), savedCount)
                    : GetAppMessage("DigitalSales_Msg_SaveMemberFail")
            });
        }

        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditMemberModal(int id, int digitalSalesId)
        {
            try
            {
                if (!HasDetailPermission(digitalSalesId, User.UserName))
                {
                    return Json(new
                    {
                        status = false,
                        message = GetAppMessage("DigitalSales_Msg_NoPermission")
                    }, JsonRequestBehavior.AllowGet);
                }

                var members = _salesCache.GetMembersBySalesID(digitalSalesId) ?? new List<RM_DigitalSalesMemberModel>();
                var member = members.FirstOrDefault(m => m.MemberID == id);
                if (member == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = GetAppMessage("DigitalSales_Msg_NotFound")
                    }, JsonRequestBehavior.AllowGet);
                }

                List<RM_RolesModel> roles = null;
                try
                {
                    roles = _rolesCache.GetAll();
                }
                catch { }

                if (roles == null || roles.Count == 0)
                {
                    roles = new List<RM_RolesModel>
                    {
                        new RM_RolesModel { RoleID = 1, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_AM") },
                        new RM_RolesModel { RoleID = 2, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_TechSolution") },
                        new RM_RolesModel { RoleID = 3, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_DeploymentExpert") },
                        new RM_RolesModel { RoleID = 4, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_PocSupport") },
                        new RM_RolesModel { RoleID = 5, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_ProjectAdmin") },
                        new RM_RolesModel { RoleID = 6, RoleName = AppProcessor.Messagor.GetMessage("DigitalSales_Role_CustomerCare") }
                    };
                }
                ViewBag.Roles = roles;

                return PartialView("_EditMemberModal", member);
            }
            catch (Exception ex)
            {
                return Content(string.Format("<div class='alert alert-danger p-3'>{0}: {1}</div>", GetAppMessage("DigitalSales_Msg_SaveMemberFail"), HttpUtility.HtmlEncode(ex.Message)));
            }
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveMember(RM_DigitalSalesMemberModel model, string EmployeeIDs, string RoleIDs, string CustomRole)
        {
            try
            {
                if (model.DigitalSalesID <= 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = GetAppMessage("DigitalSales_Msg_InvalidSalesRecord")
                    });
                }

                if (!HasDetailPermission(model.DigitalSalesID, User.UserName))
                {
                    return Json(new
                    {
                        status = false,
                        message = GetAppMessage("DigitalSales_Msg_NoPermission")
                    });
                }

                var roleNamesList = new List<string>();
                if (!string.IsNullOrWhiteSpace(RoleIDs))
                {
                    List<RM_RolesModel> allRoles = null;
                    try
                    {
                        allRoles = _rolesCache.GetAll();
                    }
                    catch { }

                    allRoles = allRoles ?? new List<RM_RolesModel>();
                    var roleIdSet = new HashSet<string>(RoleIDs.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));

                    foreach (var r in allRoles)
                    {
                        if (roleIdSet.Contains(r.RoleID.ToString()))
                        {
                            roleNamesList.Add(r.RoleName);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(CustomRole))
                {
                    roleNamesList.Add(CustomRole.Trim());
                }
                else if (!string.IsNullOrWhiteSpace(model.RoleTitle))
                {
                    roleNamesList.Add(model.RoleTitle.Trim());
                }

                var finalRoleTitle = roleNamesList.Count > 0 ? string.Join(", ", roleNamesList.Distinct()) : AppProcessor.Messagor.GetMessage("DigitalSales_Role_Member");

                if (model.MemberID > 0)
                {
                    model.RoleTitle = finalRoleTitle;
                    model.IsActive = true;
                    var saveResult = _salesCache.SaveMember(model, User.UserName);
                    if (saveResult > 0)
                    {
                        var existingMembers = _salesCache.GetMembersBySalesID(model.DigitalSalesID) ?? new List<RM_DigitalSalesMemberModel>();
                        var otherDups = existingMembers.Where(m => m.MemberID != model.MemberID && m.UserID == model.UserID).ToList();
                        foreach (var dup in otherDups)
                        {
                            _salesCache.DeleteMember(dup.MemberID, User.UserName);
                        }

                        return Json(new
                        {
                            status = true,
                            message = GetAppMessage("DigitalSales_Msg_UpdateMemberSuccess")
                        });
                    }
                    return Json(new
                    {
                        status = false,
                        message = GetAppMessage("DigitalSales_Msg_SaveMemberFail")
                    });
                }

                var empIdList = new List<int>();
                if (!string.IsNullOrWhiteSpace(EmployeeIDs))
                {
                    foreach (var part in EmployeeIDs.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(part.Trim(), out int eid) && eid > 0 && !empIdList.Contains(eid))
                        {
                            empIdList.Add(eid);
                        }
                    }
                }
                else if (model.UserID > 0)
                {
                    empIdList.Add(model.UserID);
                }

                if (empIdList.Count == 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = GetAppMessage("DigitalSales_Msg_MemberRequired")
                    });
                }

                var existingMembersList = _salesCache.GetMembersBySalesID(model.DigitalSalesID) ?? new List<RM_DigitalSalesMemberModel>();
                int savedCount = 0;
                foreach (var empId in empIdList)
                {
                    var existing = existingMembersList.FirstOrDefault(e => e.UserID == empId);
                    if (existing != null)
                    {
                        // Đã có trong danh sách -> gom vai trò vào cùng 1 card của người này
                        var existingRoles = (existing.RoleTitle ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                        var newRoles = roleNamesList.Select(s => s.Trim()).ToList();
                        var mergedRoles = existingRoles.Concat(newRoles).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        existing.RoleTitle = mergedRoles.Count > 0 ? string.Join(", ", mergedRoles) : existing.RoleTitle;
                        if (model.IsAM)
                        {
                            existing.IsAM = true;
                        }
                        if (!string.IsNullOrWhiteSpace(model.Note))
                        {
                            existing.Note = model.Note;
                        }
                        existing.IsActive = true;
                        var saveId = _salesCache.SaveMember(existing, User.UserName);
                        if (saveId > 0) savedCount++;
                        continue;
                    }

                    var m = new RM_DigitalSalesMemberModel
                    {
                        MemberID = 0,
                        DigitalSalesID = model.DigitalSalesID,
                        UserID = empId,
                        RoleTitle = finalRoleTitle,
                        IsAM = model.IsAM,
                        Note = model.Note,
                        IsActive = true
                    };
                    var id = _salesCache.SaveMember(m, User.UserName);
                    if (id > 0) savedCount++;
                }

                if (savedCount > 0)
                {
                    return Json(new
                    {
                        status = true,
                        message = savedCount == 1 ? AppProcessor.Messagor.GetMessage("DigitalSales_Msg_SaveMemberSuccess") : string.Format(AppProcessor.Messagor.GetMessage("DigitalSales_Msg_SaveMembersMultiSuccess"), savedCount)
                    });
                }

                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_SaveMemberFail")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_SaveMemberFail") + " (" + ex.Message + ")"
                });
            }
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteMember(int id, int? salesId = null)
        {
            if (salesId.HasValue && salesId.Value > 0 && !HasDetailPermission(salesId.Value, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            if (salesId.HasValue && salesId.Value > 0)
            {
                var members = _salesCache.GetMembersBySalesID(salesId.Value) ?? new List<RM_DigitalSalesMemberModel>();
                var target = members.FirstOrDefault(m => m.MemberID == id);
                if (target != null && target.UserID > 0)
                {
                    var userDups = members.Where(m => m.UserID == target.UserID).ToList();
                    foreach (var m in userDups)
                    {
                        _salesCache.DeleteMember(m.MemberID, User.UserName);
                    }
                    return Json(new
                    {
                        status = true,
                        message = GetAppMessage("DigitalSales_Msg_DeleteMemberSuccess")
                    });
                }
            }

            var result = _salesCache.DeleteMember(id, User.UserName);
            if (result > 0)
            {
                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Msg_DeleteMemberSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Msg_DeleteMemberFail")
            });
        }
        #endregion

        #region 7. Tracking & Checklist
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddTrackingModal(int digitalSalesId, int? processId = null)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var model = new RM_DigitalSalesTrackingModel
            {
                DigitalSalesID = digitalSalesId,
                ProcessID = processId,
                StartDate = DateTime.Today,
                Deadline = DateTime.Today.AddDays(3),
                Status = 1,
                IsCustomTask = true
            };

            if (processId.HasValue && processId.Value > 0)
            {
                var proc = _workflowCache.GetProcessByID(processId.Value);
                ViewBag.ProcessName = proc?.ProcessName;
            }

            ViewBag.UserList = GetProjectMemberSelectList(digitalSalesId);

            return PartialView("_TrackingModal", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditTrackingModal(int id, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var tasks = _salesCache.GetTrackingTasks(digitalSalesId);
            var model = tasks.FirstOrDefault(t => t.TrackingID == id);
            if (model == null)
            {
                return Json(new
                {
                    status = false,
                    message = CreateMessage(AppProcessor.Messagor.GetMessage("DigitalSales_Task"), EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            }

            ViewBag.UserList = GetProjectMemberSelectList(digitalSalesId, model?.AssignedUserID);
            if (model.ProcessID.HasValue && model.ProcessID.Value > 0)
            {
                var proc = _workflowCache.GetProcessByID(model.ProcessID.Value);
                ViewBag.ProcessName = proc?.ProcessName ?? model.ProcessName;
            }

            return PartialView("_TrackingModal", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTracking(RM_DigitalSalesTrackingModel model, HttpPostedFileBase attachmentFile)
        {
            if (model.DigitalSalesID <= 0)
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_InvalidSalesRecord")
                });
            }

            if (!HasDetailPermission(model.DigitalSalesID, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            if (!ModelState.IsValid)
            {
                PrepareTrackingForm(model.DigitalSalesID, model.AssignedUserID);
                return PartialView("_TrackingForm", model);
            }

            // 1. Hạn xử lý: Tự động tính dựa vào Ngày bắt đầu + Số ngày xử lý
            if (model.DurationDays.HasValue && model.DurationDays.Value > 0)
            {
                model.Deadline = model.StartDate.AddDays(model.DurationDays.Value);
            }

            // 2. Ngày hoàn thành: Được tính dựa vào ngày cập nhật Trạng thái: Hoàn thành
            if (model.Status == 3)
            {
                if (!model.CompletedDate.HasValue)
                {
                    model.CompletedDate = DateTime.Now;
                }
            }
            else
            {
                model.CompletedDate = null;
            }

            // 3. Tệp đính kèm: Cho phép upload nhiều file
            var uploadedFiles = new List<string>();
            if (!string.IsNullOrWhiteSpace(model.AttachmentFile))
            {
                uploadedFiles.AddRange(model.AttachmentFile.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()));
            }

            if (attachmentFile != null && attachmentFile.ContentLength > 0)
            {
                var p = SaveUploadedFile(attachmentFile);
                if (!string.IsNullOrEmpty(p)) uploadedFiles.Add(p);
            }

            if (Request.Files != null && Request.Files.Count > 0)
            {
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0 && file != attachmentFile)
                    {
                        var p = SaveUploadedFile(file, i);
                        if (!string.IsNullOrEmpty(p)) uploadedFiles.Add(p);
                    }
                }
            }

            if (uploadedFiles.Count > 0)
            {
                model.AttachmentFile = string.Join(";", uploadedFiles.Distinct());
            }

            var isNew = model.TrackingID <= 0;
            var id = _salesCache.SaveTracking(model, User.UserName);
            if (id > 0)
            {
                // Bấm Lưu tiến trình: Lưu thông tin hiện tại và ghi 1 dòng Log thao tác để hiển thị bên Log thao tác
                var statusText = model.Status == 3 ? "Hoàn thành" : (model.Status == 2 ? "Đang thực hiện" : (model.Status == 4 ? "Quá hạn" : "Chưa thực hiện"));
                var logContent = isNew
                    ? $"Tạo mới tiến trình: <b>{HttpUtility.HtmlEncode(model.TaskName)}</b> (Trạng thái: {statusText})"
                    : $"Cập nhật tiến trình: <b>{HttpUtility.HtmlEncode(model.TaskName)}</b> (Trạng thái: {statusText})";

                if (!string.IsNullOrWhiteSpace(model.ResultNote))
                {
                    logContent += $"<div class='mt-1 text-secondary'><b>Nội dung thực hiện:</b> {model.ResultNote}</div>";
                }

                var activity = new RM_DigitalSalesActivityModel
                {
                    DigitalSalesID = model.DigitalSalesID,
                    ActivityType = (byte)(model.Status == 3 ? 4 : 5),
                    Content = logContent,
                    ReferenceID = id,
                    Attachments = model.AttachmentFile
                };
                _salesCache.AddActivity(activity, User.UserName);

                return Json(new
                {
                    status = true,
                    id = id,
                    message = GetAppMessage("DigitalSales_Msg_SaveTaskSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Msg_SaveTaskFail")
            });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ChangeProcessModal(int digitalSalesId, int statusId, int currentProcessId)
        {
            if (digitalSalesId <= 0 || statusId <= 0)
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-exclamation-circle'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_InvalidData")}</div>");
            }

            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var status = _salesCache.GetStatusList(null)?.FirstOrDefault(s => s.StatusID == statusId);
            var processes = _workflowCache.GetProcesses(out _, search: null, businessType: null, statusId: statusId);
            var activeProcesses = processes != null
                ? processes.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList()
                : new List<RM_DigitalSalesProcessModel>();

            var model = new RM_DigitalSalesChangeProcessViewModel
            {
                DigitalSalesID = digitalSalesId,
                StatusID = statusId,
                StatusName = status?.StatusName ?? "Trạng thái",
                CurrentProcessID = currentProcessId,
                SelectedProcessID = currentProcessId,
                AvailableProcesses = activeProcesses
            };

            return PartialView("_ChangeProcessModal", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult SaveChangeProcess(int digitalSalesId, int statusId, int newProcessId)
        {
            if (digitalSalesId <= 0 || statusId <= 0 || newProcessId <= 0)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidData") });
            }

            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            var result = _salesCache.ChangeProcessOfStatus(digitalSalesId, statusId, newProcessId, User.UserName);
            if (result > 0)
            {
                return Json(new { status = true, message = GetAppMessage("DigitalSalesTracking_ChangeProcessSuccess") });
            }

            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_UpdateTaskFail") });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddTodoModal(int parentTrackingId, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var tasks = _salesCache.GetTrackingTasks(digitalSalesId);
            var parent = tasks.FirstOrDefault(t => t.TrackingID == parentTrackingId);

            var model = new RM_DigitalSalesTrackingModel
            {
                TrackingID = 0,
                DigitalSalesID = digitalSalesId,
                ParentID = parentTrackingId,
                ProcessID = parent?.ProcessID,
                ProgressID = parent?.ProgressID,
                StartDate = parent?.StartDate ?? DateTime.Today,
                Deadline = parent?.MaxDeadline ?? DateTime.Today.AddDays(3),
                Status = 1,
                IsCustomTask = true,
                DurationDays = parent?.EffectiveDurationDays
            };

            ViewBag.ParentTask = parent;
            ViewBag.UserList = GetProjectMemberSelectList(digitalSalesId);

            return PartialView("_TodoModal", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditTodoModal(int id, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var tasks = _salesCache.GetTrackingTasks(digitalSalesId);
            var model = tasks.SelectMany(t => t.TodoList.Concat(new[] { t })).FirstOrDefault(t => t.TrackingID == id);
            if (model == null)
            {
                return Json(new { status = false, message = CreateMessage(AppProcessor.Messagor.GetMessage("DigitalSales_Task"), EnumProcessType.DataNotExist, EnumMsgIcon.Error) }, JsonRequestBehavior.AllowGet);
            }

            var parent = model.ParentID.HasValue ? tasks.FirstOrDefault(t => t.TrackingID == model.ParentID.Value) : null;
            ViewBag.ParentTask = parent;
            ViewBag.UserList = GetProjectMemberSelectList(digitalSalesId, model?.AssignedUserID);

            return PartialView("_TodoModal", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTodo(RM_DigitalSalesTrackingModel model)
        {
            if (model.DigitalSalesID <= 0)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidSalesRecord") });
            }

            if (!HasDetailPermission(model.DigitalSalesID, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            if (string.IsNullOrWhiteSpace(model.TaskName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_TaskNameRequired") });
            }

            // RÀNG BUỘC NGHIỆP VỤ: Deadline của Todo <= StartDate của Tiến trình + Tổng ngày của Tiến trình
            if (model.ParentID.HasValue && model.ParentID.Value > 0)
            {
                var tasks = _salesCache.GetTrackingTasks(model.DigitalSalesID);
                var parent = tasks.FirstOrDefault(t => t.TrackingID == model.ParentID.Value);
                if (parent != null)
                {
                    var maxDeadline = parent.MaxDeadline;
                    if (model.Deadline.HasValue && model.Deadline.Value.Date > maxDeadline.Date)
                    {
                        return Json(new { status = false, message = GetAppMessage("DigitalSalesTracking_DeadlineExceeded_Error") });
                    }
                }
            }

            // Xử lý file đính kèm (multiple upload)
            try
            {
                var uploadedPaths = new List<string>();
                if (Request.Files.Count > 0)
                {
                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        var file = Request.Files[i];
                        if (file != null && file.ContentLength > 0)
                        {
                            var path = SaveUploadedFile(file, i);
                            if (!string.IsNullOrEmpty(path))
                            {
                                uploadedPaths.Add(path);
                            }
                        }
                    }
                }
                if (uploadedPaths.Count > 0)
                {
                    // Nối với file cũ nếu có
                    var existingFiles = string.IsNullOrEmpty(model.AttachmentFile)
                        ? new List<string>()
                        : model.AttachmentFile.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    existingFiles.AddRange(uploadedPaths);
                    model.AttachmentFile = string.Join(";", existingFiles.Distinct());
                }
            }
            catch { /* Ignore file upload errors, proceed with save */ }

            var id = _salesCache.SaveTracking(model, User.UserName);
            if (id > 0)
            {
                return Json(new { status = true, id = id, message = GetAppMessage("DigitalSalesTracking_SaveSuccess") });
            }

            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_SaveTaskFail") });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ConfirmTracking(int trackingId, int digitalSalesId)
        {
            if (digitalSalesId > 0 && !HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            var result = _salesCache.UpdateTrackingStatus(trackingId, 3, null, null, null, null, User.UserName);
            if (result > 0)
            {
                return Json(new { status = true, message = GetAppMessage("DigitalSalesTracking_ConfirmSuccess") });
            }

            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_UpdateTaskFail") });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult UnlockTracking(int trackingId, int digitalSalesId)
        {
            if (digitalSalesId > 0 && !HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            var result = _salesCache.UpdateTrackingStatus(trackingId, 2, null, null, null, null, User.UserName);
            if (result > 0)
            {
                return Json(new { status = true, message = GetAppMessage("DigitalSalesTracking_UnlockSuccess") });
            }

            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_UpdateTaskFail") });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult UnlockProgressModal(int trackingId, int digitalSalesId)
        {
            if (digitalSalesId > 0 && !HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var tasks = _salesCache.GetTrackingTasks(digitalSalesId);
            var task = tasks.FirstOrDefault(t => t.TrackingID == trackingId);
            if (task == null)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidData") }, JsonRequestBehavior.AllowGet);
            }

            return PartialView("_UnlockProgressModal", task);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult SubmitUnlockProgress(int trackingId, int digitalSalesId, string reason)
        {
            if (digitalSalesId > 0 && !HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { status = false, message = "Vui lòng nhập lý do mở khóa tiến trình!" });
            }

            var tasks = _salesCache.GetTrackingTasks(digitalSalesId);
            var task = tasks.FirstOrDefault(t => t.TrackingID == trackingId);
            if (task == null)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidData") });
            }

            // Ghi log hoạt động mở khóa tiến trình để cập nhật nội dung (giữ nguyên trạng thái Hoàn thành)
            var act = new RM_DigitalSalesActivityModel
            {
                DigitalSalesID = digitalSalesId,
                ActivityType = 5,
                Content = $"Mở khóa tiến trình: <b>{HttpUtility.HtmlEncode(task.TaskName)}</b><div class='mt-1 text-secondary'><b>Lý do mở khóa:</b> {HttpUtility.HtmlEncode(reason.Trim())}</div>",
                ReferenceID = trackingId
            };
            _salesCache.AddActivity(act, User.UserName);

            return Json(new
            {
                status = true,
                trackingId = trackingId,
                digitalSalesId = digitalSalesId,
                message = GetAppMessage("DigitalSalesTracking_UnlockSuccess")
            });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetTrackingLogsModal(int trackingId, int digitalSalesId)
        {
            if (digitalSalesId > 0 && !HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var tasks = _salesCache.GetTrackingTasks(digitalSalesId);
            var task = tasks.FirstOrDefault(t => t.TrackingID == trackingId);
            if (task == null)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidData") }, JsonRequestBehavior.AllowGet);
            }

            var logs = _salesCache.GetActivitiesByTrackingID(digitalSalesId, trackingId);
            ViewBag.Task = task;
            return PartialView("_TrackingLogsModal", logs);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ReportTrackingModal(int trackingId, int digitalSalesId)
        {
            if (digitalSalesId > 0 && !HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var tasks = _salesCache.GetTrackingTasks(digitalSalesId);
            var task = tasks.SelectMany(t => t.TodoList.Concat(new[] { t })).FirstOrDefault(t => t.TrackingID == trackingId);
            if (task == null)
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_InvalidData") }, JsonRequestBehavior.AllowGet);
            }

            var model = new RM_DigitalSalesTrackingReportViewModel
            {
                TrackingID = trackingId,
                DigitalSalesID = digitalSalesId,
                TaskName = task.TaskName,
                Status = task.Status,
                ResultNote = task.ResultNote,
                AttachmentFile = task.AttachmentFile
            };

            return PartialView("_TrackingReportModal", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTrackingReport(RM_DigitalSalesTrackingReportViewModel model, HttpPostedFileBase reportFile)
        {
            if (model.DigitalSalesID > 0 && !HasDetailPermission(model.DigitalSalesID, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            string attachmentPath = model.AttachmentFile;
            if (reportFile != null && reportFile.ContentLength > 0)
            {
                attachmentPath = SaveUploadedFile(reportFile);
            }

            byte newStatus = model.Status > 0 ? model.Status : (byte)2;
            var result = _salesCache.UpdateTrackingStatus(model.TrackingID, newStatus, model.ResultNote, attachmentPath, null, null, User.UserName);
            if (result > 0)
            {
                return Json(new { status = true, message = GetAppMessage("DigitalSalesTracking_ReportSuccess") });
            }

            return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_UpdateTaskFail") });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult DetailTrackingTab(int id)
        {
            var model = _salesCache.GetByID(id, User.UserName);
            if (model == null)
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-exclamation-circle'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_InvalidSalesRecord")}</div>");
            }

            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailTracking", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult UpdateTrackingStatus(int trackingId, byte status, string resultNote, HttpPostedFileBase attachmentFile, int? assignedUserId, DateTime? deadline, int? salesId = null)
        {
            if (trackingId <= 0)
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_InvalidTaskCode")
                });
            }

            if (salesId.HasValue && salesId.Value > 0 && !HasDetailPermission(salesId.Value, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            string attachmentPath = null;
            if (attachmentFile != null && attachmentFile.ContentLength > 0)
            {
                attachmentPath = SaveUploadedFile(attachmentFile);
            }

            var result = _salesCache.UpdateTrackingStatus(trackingId, status, resultNote, attachmentPath, assignedUserId, deadline, User.UserName);
            if (result > 0)
            {
                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Msg_UpdateTaskSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Msg_UpdateTaskFail")
            });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeleteTracking(int id, int? salesId = null)
        {
            if (salesId.HasValue && salesId.Value > 0 && !HasDetailPermission(salesId.Value, User.UserName))
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_NoPermission")
                });
            }

            var result = _salesCache.DeleteTracking(id, User.UserName);
            if (result > 0)
            {
                return Json(new
                {
                    status = true,
                    message = GetAppMessage("DigitalSales_Msg_DeleteTaskSuccess")
                });
            }

            return Json(new
            {
                status = false,
                message = GetAppMessage("DigitalSales_Msg_DeleteTaskFail")
            });
        }
        #endregion

        #region 7.1 Import Progress & Todo
        [HttpGet]
        public ActionResult DownloadProgressImportTemplate(int processId, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            using (var workbook = new XLWorkbook())
            {
                var proc = processId > 0 ? _workflowCache.GetProcessByID(processId) : null;
                string procName = proc != null ? proc.ProcessName : "QuyTrinh";
                var ws = workbook.Worksheets.Add("DS Tien trinh");

                string[] headers = new[]
                {
                    "STT",
                    "Tên tiến trình (*)",
                    "Người thực hiện",
                    "Ngày bắt đầu (*)",
                    "Số ngày thực hiện (*)",
                    "Ghi chú"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(41, 128, 185); // #2980b9
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                var sampleRows = new[]
                {
                    new { STT = 1, Name = "Tổ chức họp tư vấn & trình diễn giải pháp", User = User.UserName, Start = DateTime.Today.ToString("dd/MM/yyyy"), Days = 3, Note = "Họp trực tiếp với đối tác" },
                    new { STT = 2, Name = "Khảo sát hiện trạng hạ tầng CNTT", User = "", Start = DateTime.Today.AddDays(3).ToString("dd/MM/yyyy"), Days = 2, Note = "Khảo sát phòng máy chủ" }
                };

                for (int r = 0; r < sampleRows.Length; r++)
                {
                    int rowIdx = r + 2;
                    ws.Cell(rowIdx, 1).Value = sampleRows[r].STT;
                    ws.Cell(rowIdx, 2).Value = sampleRows[r].Name;
                    ws.Cell(rowIdx, 3).Value = sampleRows[r].User;
                    ws.Cell(rowIdx, 4).Value = sampleRows[r].Start;
                    ws.Cell(rowIdx, 5).Value = sampleRows[r].Days;
                    ws.Cell(rowIdx, 6).Value = sampleRows[r].Note;

                    ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(rowIdx, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(rowIdx, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                var range = ws.Range(1, 1, sampleRows.Length + 1, headers.Length);
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    string safeProc = UtilString.ConvertToUnSign(procName).Replace(" ", "_");
                    string fileName = $"Mau_Import_TienTrinh_{safeProc}_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        [HttpGet]
        public ActionResult DownloadTodoImportTemplate(int processId, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            using (var workbook = new XLWorkbook())
            {
                var proc = processId > 0 ? _workflowCache.GetProcessByID(processId) : null;
                string procName = proc != null ? proc.ProcessName : "QuyTrinh";

                // SHEET 1: Dữ liệu Công việc
                var ws1 = workbook.Worksheets.Add("Du lieu Cong viec");
                string[] headers1 = new[]
                {
                    "STT",
                    "Mã tiến trình (*)",
                    "Tên công việc con (*)",
                    "Người thực hiện",
                    "Ngày bắt đầu (*)",
                    "Hạn xử lý (Deadline) (*)",
                    "Ghi chú"
                };

                for (int i = 0; i < headers1.Length; i++)
                {
                    var cell = ws1.Cell(1, i + 1);
                    cell.Value = headers1[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(39, 174, 96); // #27ae60
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                var allTasks = _salesCache.GetTrackingTasks(digitalSalesId);
                var procTasks = allTasks?.Where(t => t.ProcessID == processId && (!t.ParentID.HasValue || t.ParentID.Value <= 0)).ToList() ?? new List<RM_DigitalSalesTrackingModel>();

                string sampleCode1 = procTasks.Count > 0 ? (!string.IsNullOrEmpty(procTasks[0].TrackingCode) ? procTasks[0].TrackingCode : procTasks[0].TrackingID.ToString()) : "PR2609000328";
                string sampleCode2 = procTasks.Count > 1 ? (!string.IsNullOrEmpty(procTasks[1].TrackingCode) ? procTasks[1].TrackingCode : procTasks[1].TrackingID.ToString()) : sampleCode1;

                var sampleTodos = new[]
                {
                    new { STT = 1, Code = sampleCode1, Name = "Chuẩn bị slide tài liệu trình diễn giải pháp", User = User.UserName, Start = DateTime.Today.ToString("dd/MM/yyyy"), Deadline = DateTime.Today.AddDays(2).ToString("dd/MM/yyyy"), Note = "Gửi AM duyệt trước" },
                    new { STT = 2, Code = sampleCode2, Name = "Kiểm tra hạ tầng mạng demo", User = "", Start = DateTime.Today.ToString("dd/MM/yyyy"), Deadline = DateTime.Today.AddDays(1).ToString("dd/MM/yyyy"), Note = "Đảm bảo kết nối thông suốt" }
                };

                for (int r = 0; r < sampleTodos.Length; r++)
                {
                    int rowIdx = r + 2;
                    ws1.Cell(rowIdx, 1).Value = sampleTodos[r].STT;
                    ws1.Cell(rowIdx, 2).Value = sampleTodos[r].Code;
                    ws1.Cell(rowIdx, 3).Value = sampleTodos[r].Name;
                    ws1.Cell(rowIdx, 4).Value = sampleTodos[r].User;
                    ws1.Cell(rowIdx, 5).Value = sampleTodos[r].Start;
                    ws1.Cell(rowIdx, 6).Value = sampleTodos[r].Deadline;
                    ws1.Cell(rowIdx, 7).Value = sampleTodos[r].Note;

                    ws1.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws1.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws1.Cell(rowIdx, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws1.Cell(rowIdx, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                var range1 = ws1.Range(1, 1, sampleTodos.Length + 1, headers1.Length);
                range1.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range1.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws1.Columns().AdjustToContents();

                // SHEET 2: Danh sách Tiến trình của Quy trình (Tra cứu)
                var ws2 = workbook.Worksheets.Add("DS Tien trinh tra cuu");
                string[] headers2 = new[]
                {
                    "Mã tiến trình",
                    "Tên tiến trình",
                    "Người thực hiện",
                    "Ngày bắt đầu",
                    "Hạn chót",
                    "Tổng ngày"
                };

                for (int i = 0; i < headers2.Length; i++)
                {
                    var cell = ws2.Cell(1, i + 1);
                    cell.Value = headers2[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(41, 128, 185); // #2980b9
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                int curRow = 2;
                if (procTasks.Count > 0)
                {
                    foreach (var t in procTasks)
                    {
                        string code = !string.IsNullOrEmpty(t.TrackingCode) ? t.TrackingCode : t.TrackingID.ToString();
                        ws2.Cell(curRow, 1).Value = code;
                        ws2.Cell(curRow, 2).Value = t.TaskName;
                        ws2.Cell(curRow, 3).Value = t.AssignedUserName ?? "";
                        ws2.Cell(curRow, 4).Value = t.StartDate.ToString("dd/MM/yyyy");
                        ws2.Cell(curRow, 5).Value = t.MaxDeadline.ToString("dd/MM/yyyy");
                        ws2.Cell(curRow, 6).Value = t.EffectiveDurationDays;

                        ws2.Cell(curRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws2.Cell(curRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws2.Cell(curRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws2.Cell(curRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        curRow++;
                    }
                }
                else
                {
                    ws2.Cell(2, 1).Value = "(Chưa có tiến trình nào trong quy trình này)";
                    ws2.Range(2, 1, 2, headers2.Length).Merge();
                    ws2.Cell(2, 1).Style.Font.Italic = true;
                    ws2.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    curRow = 3;
                }

                var range2 = ws2.Range(1, 1, curRow - 1, headers2.Length);
                range2.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range2.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws2.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    string safeProc = UtilString.ConvertToUnSign(procName).Replace(" ", "_");
                    string fileName = $"Mau_Import_CongViec_{safeProc}_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ImportProgressModal(int processId, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var proc = processId > 0 ? _workflowCache.GetProcessByID(processId) : null;
            var sales = _salesCache.GetByID(digitalSalesId);

            ViewBag.ProcessID = processId;
            ViewBag.DigitalSalesID = digitalSalesId;
            ViewBag.ProcessName = proc?.ProcessName ?? "Quy trình";
            ViewBag.SalesTitle = sales?.Title ?? "";

            return PartialView("_ImportProgressModal");
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ImportTodoModal(int processId, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var proc = processId > 0 ? _workflowCache.GetProcessByID(processId) : null;
            var sales = _salesCache.GetByID(digitalSalesId);

            ViewBag.ProcessID = processId;
            ViewBag.DigitalSalesID = digitalSalesId;
            ViewBag.ProcessName = proc?.ProcessName ?? "Quy trình";
            ViewBag.SalesTitle = sales?.Title ?? "";

            return PartialView("_ImportTodoModal");
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult PreviewImportProgress(HttpPostedFileBase importFile, int processId, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            if (importFile == null || importFile.ContentLength <= 0)
            {
                return Json(new { status = false, message = "Vui lòng chọn tệp Excel để import!" });
            }

            var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                return Json(new { status = false, message = "Định dạng tệp không hợp lệ! Vui lòng chọn tệp Excel (.xlsx hoặc .xls)." });
            }

            try
            {
                var allRows = new List<RM_DigitalSalesProgressImportRowDTO>();
                var existingTasks = _salesCache.GetTrackingTasks(digitalSalesId)?.Where(t => t.ProcessID == processId && (!t.ParentID.HasValue || t.ParentID.Value <= 0)).ToList() ?? new List<RM_DigitalSalesTrackingModel>();
                var existingNames = new HashSet<string>(existingTasks.Select(t => t.TaskName.Trim()), StringComparer.OrdinalIgnoreCase);
                var batchNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var workbook = new XLWorkbook(importFile.InputStream))
                {
                    var ws = workbook.Worksheets.FirstOrDefault();
                    if (ws == null || ws.RowsUsed().Count() <= 1)
                    {
                        return Json(new { status = false, message = "Tệp Excel rỗng hoặc không có dữ liệu để import!" });
                    }

                    var rowsUsed = ws.RowsUsed().Skip(1);
                    int rowIdx = 1;

                    foreach (var row in rowsUsed)
                    {
                        string taskName = GetCellString(row.Cell(2));
                        string assignedUserText = GetCellString(row.Cell(3));
                        string startDateText = GetCellString(row.Cell(4));
                        string durationText = GetCellString(row.Cell(5));
                        string note = GetCellString(row.Cell(6));

                        if (string.IsNullOrWhiteSpace(taskName) && string.IsNullOrWhiteSpace(startDateText) && string.IsNullOrWhiteSpace(durationText))
                        {
                            continue;
                        }

                        var dto = new RM_DigitalSalesProgressImportRowDTO
                        {
                            RowIndex = rowIdx++,
                            TaskName = taskName,
                            AssignedUserName = assignedUserText,
                            StartDateStr = startDateText,
                            Note = note
                        };

                        var errors = new List<string>();

                        // 1. Kiểm tra Tên tiến trình
                        if (string.IsNullOrWhiteSpace(taskName))
                        {
                            errors.Add("Tên tiến trình không được để trống");
                        }
                        else
                        {
                            if (existingNames.Contains(taskName.Trim()))
                            {
                                errors.Add("Tên tiến trình đã tồn tại trong quy trình này");
                            }
                            else if (batchNames.Contains(taskName.Trim()))
                            {
                                errors.Add("Tên tiến trình bị trùng lặp trong tệp import");
                            }
                            else
                            {
                                batchNames.Add(taskName.Trim());
                            }
                        }

                        // 2. Kiểm tra Người thực hiện
                        if (!string.IsNullOrWhiteSpace(assignedUserText))
                        {
                            string resolvedName;
                            var userId = ResolveUserId(assignedUserText, digitalSalesId, out resolvedName);
                            if (userId.HasValue && userId.Value > 0)
                            {
                                dto.AssignedUserID = userId.Value;
                                dto.AssignedUserName = resolvedName;
                            }
                            else
                            {
                                errors.Add($"Không tìm thấy nhân sự '{assignedUserText}' trong hồ sơ/hệ thống");
                            }
                        }

                        // 3. Kiểm tra Ngày bắt đầu
                        var parsedStart = ParseDateCell(row.Cell(4));
                        if (parsedStart.HasValue)
                        {
                            dto.StartDate = parsedStart.Value;
                            dto.StartDateStr = parsedStart.Value.ToString("dd/MM/yyyy");
                        }
                        else if (!string.IsNullOrWhiteSpace(startDateText))
                        {
                            errors.Add("Ngày bắt đầu không đúng định dạng dd/MM/yyyy");
                        }
                        else
                        {
                            dto.StartDate = DateTime.Today;
                            dto.StartDateStr = DateTime.Today.ToString("dd/MM/yyyy");
                        }

                        // 4. Kiểm tra Số ngày thực hiện
                        int duration = 3;
                        if (!string.IsNullOrWhiteSpace(durationText))
                        {
                            if (int.TryParse(durationText, out int parsedDuration) && parsedDuration > 0)
                            {
                                duration = parsedDuration;
                                dto.DurationDays = duration;
                            }
                            else
                            {
                                errors.Add("Số ngày thực hiện phải là số nguyên lớn hơn 0");
                            }
                        }
                        else
                        {
                            dto.DurationDays = duration;
                        }

                        if (dto.StartDate.HasValue && dto.DurationDays.HasValue)
                        {
                            dto.Deadline = dto.StartDate.Value.AddDays(dto.DurationDays.Value);
                        }

                        if (errors.Count > 0)
                        {
                            dto.ErrorMessage = string.Join("; ", errors);
                        }

                        allRows.Add(dto);
                    }
                }

                if (allRows.Count == 0)
                {
                    return Json(new { status = false, message = "Không tìm thấy dữ liệu hợp lệ nào trong tệp Excel!" });
                }

                int validCount = allRows.Count(r => r.IsValid);
                int errorCount = allRows.Count(r => !r.IsValid);

                return Json(new
                {
                    status = true,
                    total = allRows.Count,
                    validCount = validCount,
                    errorCount = errorCount,
                    rows = allRows
                });
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return Json(new { status = false, message = "Lỗi khi đọc tệp Excel: " + ex.Message });
            }
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ConfirmImportProgress(int processId, int digitalSalesId, string validDataJson)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            if (string.IsNullOrWhiteSpace(validDataJson))
            {
                return Json(new { status = false, message = "Không có dữ liệu hợp lệ để xác nhận import!" });
            }

            try
            {
                var rows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<RM_DigitalSalesProgressImportRowDTO>>(validDataJson);
                if (rows == null || rows.Count == 0)
                {
                    return Json(new { status = false, message = "Danh sách dữ liệu import rỗng!" });
                }

                int savedCount = 0;
                int sortIdx = 100;

                foreach (var r in rows)
                {
                    if (string.IsNullOrWhiteSpace(r.TaskName)) continue;

                    var model = new RM_DigitalSalesTrackingModel
                    {
                        TrackingID = 0,
                        DigitalSalesID = digitalSalesId,
                        ProcessID = processId,
                        TaskName = r.TaskName.Trim(),
                        AssignedUserID = r.AssignedUserID,
                        StartDate = r.StartDate ?? DateTime.Today,
                        Deadline = r.Deadline ?? (r.StartDate ?? DateTime.Today).AddDays(r.DurationDays ?? 3),
                        DurationDays = r.DurationDays ?? 3,
                        Status = 1,
                        IsCustomTask = true,
                        SortOrder = sortIdx++,
                        ResultNote = r.Note
                    };

                    var id = _salesCache.SaveTracking(model, User.UserName);
                    if (id > 0) savedCount++;
                }

                return Json(new
                {
                    status = savedCount > 0,
                    count = savedCount,
                    message = savedCount > 0 ? $"Đã import thành công {savedCount} tiến trình vào quy trình!" : "Không lưu được tiến trình nào!"
                });
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return Json(new { status = false, message = "Lỗi khi lưu dữ liệu import: " + ex.Message });
            }
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult PreviewImportTodo(HttpPostedFileBase importFile, int processId, int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            if (importFile == null || importFile.ContentLength <= 0)
            {
                return Json(new { status = false, message = "Vui lòng chọn tệp Excel để import!" });
            }

            var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                return Json(new { status = false, message = "Định dạng tệp không hợp lệ! Vui lòng chọn tệp Excel (.xlsx hoặc .xls)." });
            }

            try
            {
                var allRows = new List<RM_DigitalSalesTodoImportRowDTO>();
                var allTasks = _salesCache.GetTrackingTasks(digitalSalesId);
                var procTasks = allTasks?.Where(t => t.ProcessID == processId && (!t.ParentID.HasValue || t.ParentID.Value <= 0)).ToList() ?? new List<RM_DigitalSalesTrackingModel>();

                var taskDict = new Dictionary<string, RM_DigitalSalesTrackingModel>(StringComparer.OrdinalIgnoreCase);
                foreach (var pt in procTasks)
                {
                    if (!string.IsNullOrEmpty(pt.TrackingCode))
                    {
                        taskDict[pt.TrackingCode.Trim()] = pt;
                    }
                    taskDict[pt.TrackingID.ToString()] = pt;
                    if (!string.IsNullOrEmpty(pt.TaskName))
                    {
                        taskDict[pt.TaskName.Trim()] = pt;
                    }
                }

                using (var workbook = new XLWorkbook(importFile.InputStream))
                {
                    var ws = workbook.Worksheets.FirstOrDefault();
                    if (ws == null || ws.RowsUsed().Count() <= 1)
                    {
                        return Json(new { status = false, message = "Tệp Excel rỗng hoặc Sheet 1 không có dữ liệu!" });
                    }

                    var rowsUsed = ws.RowsUsed().Skip(1);
                    int rowIdx = 1;

                    foreach (var row in rowsUsed)
                    {
                        string trackingCode = GetCellString(row.Cell(2));
                        string taskName = GetCellString(row.Cell(3));
                        string assignedUserText = GetCellString(row.Cell(4));
                        string startDateText = GetCellString(row.Cell(5));
                        string deadlineText = GetCellString(row.Cell(6));
                        string note = GetCellString(row.Cell(7));

                        if (string.IsNullOrWhiteSpace(trackingCode) && string.IsNullOrWhiteSpace(taskName) && string.IsNullOrWhiteSpace(deadlineText))
                        {
                            continue;
                        }

                        var dto = new RM_DigitalSalesTodoImportRowDTO
                        {
                            RowIndex = rowIdx++,
                            TrackingCode = trackingCode,
                            TaskName = taskName,
                            AssignedUserName = assignedUserText,
                            StartDateStr = startDateText,
                            DeadlineStr = deadlineText,
                            Note = note
                        };

                        var errors = new List<string>();

                        // 1. Kiểm tra Mã tiến trình cha
                        RM_DigitalSalesTrackingModel parentTask = null;
                        if (string.IsNullOrWhiteSpace(trackingCode))
                        {
                            errors.Add("Mã tiến trình không được để trống");
                        }
                        else
                        {
                            if (taskDict.TryGetValue(trackingCode.Trim(), out parentTask))
                            {
                                dto.ParentTrackingID = parentTask.TrackingID;
                                dto.ParentTaskName = parentTask.TaskName;
                                dto.ParentMaxDeadline = parentTask.MaxDeadline;
                            }
                            else
                            {
                                errors.Add($"Mã tiến trình '{trackingCode}' không tồn tại trong quy trình này (xem Sheet 2)");
                            }
                        }

                        // 2. Kiểm tra Tên công việc con
                        if (string.IsNullOrWhiteSpace(taskName))
                        {
                            errors.Add("Tên công việc con không được để trống");
                        }

                        // 3. Kiểm tra Người thực hiện
                        if (!string.IsNullOrWhiteSpace(assignedUserText))
                        {
                            string resolvedName;
                            var userId = ResolveUserId(assignedUserText, digitalSalesId, out resolvedName);
                            if (userId.HasValue && userId.Value > 0)
                            {
                                dto.AssignedUserID = userId.Value;
                                dto.AssignedUserName = resolvedName;
                            }
                            else
                            {
                                errors.Add($"Không tìm thấy nhân sự '{assignedUserText}' trong hồ sơ/hệ thống");
                            }
                        }

                        // 4. Kiểm tra Ngày bắt đầu
                        var parsedStart = ParseDateCell(row.Cell(5));
                        if (parsedStart.HasValue)
                        {
                            dto.StartDate = parsedStart.Value;
                            dto.StartDateStr = parsedStart.Value.ToString("dd/MM/yyyy");
                        }
                        else if (!string.IsNullOrWhiteSpace(startDateText))
                        {
                            errors.Add("Ngày bắt đầu không đúng định dạng dd/MM/yyyy");
                        }
                        else if (parentTask != null)
                        {
                            dto.StartDate = parentTask.StartDate;
                            dto.StartDateStr = parentTask.StartDate.ToString("dd/MM/yyyy");
                        }
                        else
                        {
                            dto.StartDate = DateTime.Today;
                            dto.StartDateStr = DateTime.Today.ToString("dd/MM/yyyy");
                        }

                        // 5. Kiểm tra Hạn xử lý (Deadline)
                        var parsedDeadline = ParseDateCell(row.Cell(6));
                        if (parsedDeadline.HasValue)
                        {
                            dto.Deadline = parsedDeadline.Value;
                            dto.DeadlineStr = parsedDeadline.Value.ToString("dd/MM/yyyy");

                            // RÀNG BUỘC NGHIỆP VỤ: Hạn xử lý <= Hạn tối đa của tiến trình cha
                            if (parentTask != null && dto.Deadline.Value.Date > parentTask.MaxDeadline.Date)
                            {
                                errors.Add($"Hạn xử lý ({dto.Deadline.Value:dd/MM/yyyy}) vượt quá hạn tối đa của tiến trình cha ({parentTask.MaxDeadline:dd/MM/yyyy})");
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(deadlineText))
                        {
                            errors.Add("Hạn xử lý không đúng định dạng dd/MM/yyyy");
                        }
                        else if (parentTask != null)
                        {
                            dto.Deadline = parentTask.MaxDeadline;
                            dto.DeadlineStr = parentTask.MaxDeadline.ToString("dd/MM/yyyy");
                        }
                        else
                        {
                            errors.Add("Hạn xử lý không được để trống");
                        }

                        if (errors.Count > 0)
                        {
                            dto.ErrorMessage = string.Join("; ", errors);
                        }

                        allRows.Add(dto);
                    }
                }

                if (allRows.Count == 0)
                {
                    return Json(new { status = false, message = "Không tìm thấy dữ liệu hợp lệ nào trong tệp Excel!" });
                }

                int validCount = allRows.Count(r => r.IsValid);
                int errorCount = allRows.Count(r => !r.IsValid);

                return Json(new
                {
                    status = true,
                    total = allRows.Count,
                    validCount = validCount,
                    errorCount = errorCount,
                    rows = allRows
                });
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return Json(new { status = false, message = "Lỗi khi đọc tệp Excel: " + ex.Message });
            }
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ConfirmImportTodo(int processId, int digitalSalesId, string validDataJson)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Json(new { status = false, message = GetAppMessage("DigitalSales_Msg_NoPermission") });
            }

            if (string.IsNullOrWhiteSpace(validDataJson))
            {
                return Json(new { status = false, message = "Không có dữ liệu hợp lệ để xác nhận import!" });
            }

            try
            {
                var rows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<RM_DigitalSalesTodoImportRowDTO>>(validDataJson);
                if (rows == null || rows.Count == 0)
                {
                    return Json(new { status = false, message = "Danh sách dữ liệu import rỗng!" });
                }

                int savedCount = 0;
                int sortIdx = 100;

                foreach (var r in rows)
                {
                    if (string.IsNullOrWhiteSpace(r.TaskName) || !r.ParentTrackingID.HasValue || r.ParentTrackingID.Value <= 0) continue;

                    var model = new RM_DigitalSalesTrackingModel
                    {
                        TrackingID = 0,
                        DigitalSalesID = digitalSalesId,
                        ProcessID = processId,
                        ParentID = r.ParentTrackingID.Value,
                        TaskName = r.TaskName.Trim(),
                        AssignedUserID = r.AssignedUserID,
                        StartDate = r.StartDate ?? DateTime.Today,
                        Deadline = r.Deadline ?? (r.StartDate ?? DateTime.Today).AddDays(3),
                        DurationDays = r.DurationDays ?? 3,
                        Status = 1,
                        IsCustomTask = true,
                        SortOrder = sortIdx++,
                        ResultNote = r.Note
                    };

                    var id = _salesCache.SaveTracking(model, User.UserName);
                    if (id > 0) savedCount++;
                }

                return Json(new
                {
                    status = savedCount > 0,
                    count = savedCount,
                    message = savedCount > 0 ? $"Đã import thành công {savedCount} công việc con vào tiến trình!" : "Không lưu được công việc con nào!"
                });
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return Json(new { status = false, message = "Lỗi khi lưu dữ liệu import: " + ex.Message });
            }
        }

        private string GetCellString(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty()) return string.Empty;
            if (cell.DataType == XLDataType.DateTime)
            {
                return cell.GetDateTime().ToString("dd/MM/yyyy");
            }
            return cell.GetString()?.Trim() ?? string.Empty;
        }

        private DateTime? ParseDateCell(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty()) return null;
            try
            {
                if (cell.DataType == XLDataType.DateTime)
                {
                    return cell.GetDateTime();
                }
                string val = cell.GetString()?.Trim();
                if (!string.IsNullOrEmpty(val) && DateTime.TryParseExact(val, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                {
                    return dt;
                }
            }
            catch { }
            return null;
        }

        private int? ResolveUserId(string input, int digitalSalesId, out string resolvedName)
        {
            resolvedName = null;
            if (string.IsNullOrWhiteSpace(input)) return null;

            string clean = input.Trim().ToLowerInvariant();

            // 1. Tìm trong thành viên hồ sơ KDGP
            var members = _salesCache.GetMembersBySalesID(digitalSalesId);
            if (members != null)
            {
                var m = members.FirstOrDefault(x =>
                    (!string.IsNullOrEmpty(x.UserName) && x.UserName.ToLowerInvariant() == clean) ||
                    (!string.IsNullOrEmpty(x.FullName) && x.FullName.ToLowerInvariant() == clean) ||
                    ($"{x.FullName} ({x.UserName})".ToLowerInvariant() == clean) ||
                    (!string.IsNullOrEmpty(x.FullName) && x.FullName.ToLowerInvariant().Contains(clean))
                );
                if (m != null && m.UserID > 0)
                {
                    resolvedName = !string.IsNullOrEmpty(m.FullName) ? $"{m.FullName} ({m.UserName})" : m.UserName;
                    return m.UserID;
                }
            }

            // 2. Tìm AM chủ trì
            var sales = _salesCache.GetByID(digitalSalesId);
            if (sales != null && sales.AssignedEmployeeID.HasValue && sales.AssignedEmployeeID.Value > 0)
            {
                var amUser = _userCache.GetById(sales.AssignedEmployeeID.Value);
                if (amUser != null)
                {
                    if (amUser.UserName.ToLowerInvariant() == clean || amUser.FullName.ToLowerInvariant() == clean || $"{amUser.FullName} ({amUser.UserName})".ToLowerInvariant() == clean)
                    {
                        resolvedName = $"{amUser.FullName} ({amUser.UserName})";
                        return amUser.UserId;
                    }
                }
            }

            // 3. Tìm trong toàn bộ UserCache
            var allUsers = _userCache.GetAll();
            if (allUsers != null)
            {
                var u = allUsers.FirstOrDefault(x =>
                    (!string.IsNullOrEmpty(x.UserName) && x.UserName.ToLowerInvariant() == clean) ||
                    (!string.IsNullOrEmpty(x.FullName) && x.FullName.ToLowerInvariant() == clean) ||
                    ($"{x.FullName} ({x.UserName})".ToLowerInvariant() == clean)
                );
                if (u != null)
                {
                    resolvedName = $"{u.FullName} ({u.UserName})";
                    return u.UserId;
                }
            }

            return null;
        }
        #endregion

        #region 8. Ajax Helpers
        [AjaxOnly]
        [HttpGet]
        public ActionResult SearchCustomers(string q, int page = 1, int pageSize = 20, int? customerId = null)
        {
            try
            {
                if (customerId.HasValue && customerId.Value > 0)
                {
                    var cus = _customerCache.GetById(customerId.Value);
                    if (cus != null)
                    {
                        var cusItem = new
                        {
                            id = cus.CustomerID,
                            text = cus.CustomerName,
                            shortName = cus.ShortName,
                            taxCode = cus.TaxCode,
                            phone = cus.Phone,
                            address = cus.AddressCus
                        };
                        return Json(new
                        {
                            total = 1,
                            page = 1,
                            pageSize = 1,
                            totalPages = 1,
                            data = new List<object> { cusItem },
                            results = new List<object> { cusItem },
                            pagination = new { more = false }
                        }, JsonRequestBehavior.AllowGet);
                    }
                }

                var searchModel = new RM_CustomerSearchModel
                {
                    Keyword = q
                };
                var baseSearch = new BaseSearchModel
                {
                    StartIndex = (page - 1) * pageSize,
                    PageSize = pageSize,
                    Order = "0",
                    OrderDir = "ASC"
                };

                int total = 0;
                var list = _customerCache.Get(out total, searchModel, baseSearch);

                var items = list?.Select(c => (object)new
                {
                    id = c.CustomerID,
                    text = c.CustomerName,
                    shortName = c.ShortName,
                    taxCode = c.TaxCode,
                    phone = c.Phone,
                    address = c.AddressCus
                }).ToList() ?? new List<object>();

                int totalPages = (int)Math.Ceiling((double)total / (pageSize > 0 ? pageSize : 10));
                bool more = (page * pageSize) < total;

                return Json(new
                {
                    total = total,
                    page = page,
                    pageSize = pageSize,
                    totalPages = totalPages,
                    data = items,
                    results = items,
                    pagination = new { more = more }
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { total = 0, page = 1, totalPages = 0, data = new List<object>(), results = new List<object>(), pagination = new { more = false } }, JsonRequestBehavior.AllowGet);
            }
        }

        [AjaxOnly]
        [HttpGet]
        public ActionResult GetCustomerDetail(int id)
        {
            if (id <= 0) return Json(null, JsonRequestBehavior.AllowGet);
            try
            {
                var c = _customerCache.GetById(id);
                if (c == null) return Json(null, JsonRequestBehavior.AllowGet);
                return Json(new
                {
                    id = c.CustomerID,
                    customerName = c.CustomerName,
                    shortName = c.ShortName,
                    taxCode = c.TaxCode,
                    phone = c.Phone,
                    email = c.Email,
                    address = c.AddressCus
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        [AjaxOnly]
        [HttpGet]
        public ActionResult GetContactPersons(int customerId)
        {
            var list = _contactPersonCache.GetByCustomerID(customerId)?.Select(c => new
            {
                id = c.ContactPerson_ID,
                name = $"{c.FullName} - {c.Position} ({c.Phone ?? c.Email ?? ""})"
            }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        public ActionResult GetDepartmentByEmployee(int employeeId)
        {
            var deptId = GetDepartmentIdByUserId(employeeId);
            return Json(new { departmentId = deptId ?? 0 }, JsonRequestBehavior.AllowGet);
        }

        private int? GetDepartmentIdByUserId(int? userId)
        {
            if (!userId.HasValue || userId.Value <= 0) return null;
            try
            {
                var deptId = _salesCache.GetDepartmentByUserID(userId.Value);
                if (deptId.HasValue && deptId.Value > 0) return deptId;

                var user = _userCache.GetById(userId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var userBoPhans = _userBoPhanCache.GetByEmail(user.Email);
                    if (userBoPhans != null && userBoPhans.Count > 0)
                    {
                        return userBoPhans.FirstOrDefault()?.BoPhan_ID;
                    }
                }
            }
            catch { }
            return null;
        }

        [HttpGet]
        public ActionResult GetStatusesByBusinessType(byte? businessType)
        {
            byte? bType = (businessType.HasValue && businessType.Value > 0) ? businessType : (byte?)null;
            var list = _salesCache.GetStatusList(bType)?.Select(s => new
            {
                id = s.StatusID,
                name = bType.HasValue ? s.StatusName : $"[{(s.BusinessType == 1 ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity") : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project"))}] {s.StatusName}"
            }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        #endregion
        
        #region 9. Helpers & Dropdown Population
        private List<RM_DigitalSalesUserModel> GetAccessibleEmployees()
        {
            try
            {
                var list = _salesCache.GetAccessibleEmployees(User.UserName);
                if (list != null && list.Count > 0) return list;
            }
            catch { }
            return new List<RM_DigitalSalesUserModel>();
        }

        [HttpGet]
        public JsonResult GetEmployeesByDepartment(int departmentId)
        {
            if (departmentId > 0)
            {
                var users = _userCache.GetByBoPhanAndChucVu(departmentId, null) ?? new List<SysUserModel>();
                var result = users.Select(x => new
                {
                    Value = x.UserId,
                    Text = $"{x.FullName} ({x.UserName})"
                }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var employees = GetAccessibleEmployees();
                var result = employees.Select(x => new
                {
                    Value = (int?)x.UserId,
                    Text = $"{x.FullName} ({x.UserName})"
                }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        private List<MN_BoPhanModel> GetAccessibleDepartments()
        {
            var currentUser = _userCache.GetByUserName(User.UserName);
            List<MN_BoPhanModel> list = null;
            if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Email))
            {
                list = (_userBoPhanCache.GetByEmail(currentUser.Email) ?? new List<MN_BoPhanModel>())
                    .GroupBy(x => x.BoPhan_ID)
                    .Select(x => x.First())
                    .OrderBy(x => x.TenBoPhanView)
                    .ToList();
            }

            if (list == null || list.Count == 0)
            {
                list = (_departmentCache.GetAll() ?? new List<MN_BoPhanModel>())
                    .Where(x => (x.MaBoPhan != null && x.MaBoPhan.StartsWith("239.603")) || x.BoPhan_ID == 5749 || x.BoPhanCha_ID == 5749)
                    .OrderBy(x => x.TenBoPhan)
                    .ToList();
            }

            return list;
        }

        #region Authorization Helpers
        private bool IsUserQTHT(string userName, int? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName)) return false;
                if (userName.Equals("admin", StringComparison.OrdinalIgnoreCase) || userName.Equals("quantri", StringComparison.OrdinalIgnoreCase)) return true;

                if (!userId.HasValue || userId.Value <= 0)
                {
                    var u = _userCache.GetByUserName(userName);
                    userId = u?.UserId;
                }

                if (userId.HasValue && userId.Value > 0)
                {
                    var roles = _userCache.GetRoles(userId.Value);
                    if (roles != null && roles.Any(r => r.RoleId == 1 || (r.Name != null && (r.Name.Equals("QTHT", StringComparison.OrdinalIgnoreCase) || UtilString.ConvertToUnSign(r.Name).IndexOf("quan tri", StringComparison.OrdinalIgnoreCase) >= 0))))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback safe
            }

            return false;
        }

        private bool HasDetailPermission(RM_DigitalSalesModel sales, string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName)) return false;

                // 1. Admin (Được phân nhóm QTHT hoặc tài khoản quản trị mặc định)
                if (IsUserQTHT(userName)) return true;

                // 2. Chỉ những người được check Cập nhật nội dung (IsAM = true) trong thành viên hồ sơ
                if (sales != null)
                {
                    var currentUser = _userCache.GetByUserName(userName);
                    var currentUserId = currentUser?.UserId;

                    var members = sales.Members;
                    if ((members == null || members.Count == 0) && sales.DigitalSalesID > 0)
                    {
                        members = _salesCache.GetMembersBySalesID(sales.DigitalSalesID);
                    }

                    if (members != null && members.Count > 0)
                    {
                        var hasUpdatePermission = members.Any(m =>
                            m.IsAM && (
                                (!string.IsNullOrEmpty(m.UserName) && m.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)) ||
                                (currentUserId.HasValue && currentUserId.Value > 0 && m.UserID == currentUserId.Value)
                            )
                        );
                        if (hasUpdatePermission) return true;
                    }
                }
            }
            catch
            {
                // Fallback safe
            }

            return false;
        }

        private bool HasDetailPermission(int digitalSalesId, string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName)) return false;
                if (IsUserQTHT(userName)) return true;

                if (digitalSalesId > 0)
                {
                    var sales = _salesCache.GetByID(digitalSalesId);
                    return HasDetailPermission(sales, userName);
                }
            }
            catch
            {
                // Fallback safe
            }

            return false;
        }
        #endregion

        private void PrepareSearchDropdowns(RM_DigitalSalesSearchModel model)
        {
            var accessibleDepts = GetAccessibleDepartments();
            model.Departments = accessibleDepts.Select(d => new SelectListItem
            {
                Value = d.BoPhan_ID.ToString(),
                Text = !string.IsNullOrEmpty(d.TenBoPhanView) ? d.TenBoPhanView : d.TenBoPhan
            }).ToList();

            List<SysUserModel> users;
            if (model.DepartmentID > 0)
            {
                users = _userCache.GetByBoPhanAndChucVu(model.DepartmentID, null) ?? new List<SysUserModel>();
            }
            else
            {
                if (IsUserQTHT(User.UserName))
                {
                    var allActive = _userCache.GetAll();
                    users = allActive != null ? allActive.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList() : new List<SysUserModel>();
                }
                else
                {
                    var deptUsers = new List<SysUserModel>();
                    if (accessibleDepts != null && accessibleDepts.Count > 0)
                    {
                        foreach (var d in accessibleDepts)
                        {
                            var uInDept = _userCache.GetByBoPhanAndChucVu(d.BoPhan_ID, null);
                            if (uInDept != null) deptUsers.AddRange(uInDept);
                        }
                    }
                    var currentUser = _userCache.GetByUserName(User.UserName);
                    if (currentUser != null && !deptUsers.Any(u => u.UserId == currentUser.UserId))
                    {
                        deptUsers.Add(currentUser);
                    }
                    users = deptUsers.Where(u => u != null && u.IsActive).GroupBy(u => u.UserId).Select(g => g.First()).OrderBy(u => u.FullName).ToList();
                }
            }

            model.ListEmployee = users.Select(e => new SelectListItem
            {
                Value = e.UserId.ToString(),
                Text = $"{e.FullName} ({e.UserName})"
            }).ToList();

            byte? bType = model.BusinessType > 0 ? (byte?)model.BusinessType : (byte?)null;
            var statusList = _salesCache.GetStatusList(null) ?? new List<RM_DigitalSalesStatusModel>();
            model.StatusItems = statusList;

            var exclusionConfig = _sysConfigCache.GetViaKey("DIGITAL_SALES_EXCLUSION_STATUS_IDS_KEY") ?? _sysConfigCache.GetViaKey("EXCLUSION_STATUS_IDS_KEY");
            model.ExcludedStatusIDs = (exclusionConfig?.ConfigValue ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => { int.TryParse(x, out int id); return id; })
                .Where(x => x > 0)
                .ToList();

            model.ListStatus = statusList.Select(s => new SelectListItem
            {
                Value = s.StatusID.ToString(),
                Text = bType.HasValue ? s.StatusName : $"[{(s.BusinessType == 1 ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity") : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project"))}] {s.StatusName}",
                Selected = s.StatusID == model.StatusID
            }).ToList();

            model.FilterSpecialList = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = GetAppMessage("DigitalSalesSearch_FilterSpecial_All") },
                new SelectListItem { Value = "1", Text = GetAppMessage("DigitalSalesSearch_FilterSpecial_KeyProject") },
                new SelectListItem { Value = "2", Text = GetAppMessage("DigitalSalesSearch_FilterSpecial_Followed") }
            };
        }

        private void PrepareSalesDropdowns(RM_DigitalSalesModel model)
        {
            if (model.CustomerID > 0)
            {
                var cus = _customerCache.GetById(model.CustomerID);
                if (cus != null)
                {
                    model.ListCustomer = new List<SelectListItem>
                    {
                        new SelectListItem { Value = cus.CustomerID.ToString(), Text = cus.CustomerName, Selected = true }
                    };
                    model.CustomerName = cus.CustomerName;
                }
                else
                {
                    model.ListCustomer = new List<SelectListItem>();
                }
            }
            else
            {
                model.ListCustomer = new List<SelectListItem>();
            }

            if (model.CustomerID > 0)
            {
                model.ListContactPerson = _contactPersonCache.GetByCustomerID(model.CustomerID)?.Select(c => new SelectListItem
                {
                    Value = c.ContactPerson_ID.ToString(),
                    Text = $"{c.FullName} - {c.Position}"
                }).ToList() ?? new List<SelectListItem>();
            }
            else
            {
                model.ListContactPerson = new List<SelectListItem>();
            }

            model.ListStatus = _salesCache.GetStatusList(model.BusinessType)?.Select(s => new SelectListItem
            {
                Value = s.StatusID.ToString(),
                Text = s.StatusName,
                Selected = (s.StatusID == model.StatusID)
            }).ToList() ?? new List<SelectListItem>();

            var accessibleUsers = GetAccessibleEmployees();

            var currentLoginUser = _userCache.GetByUserName(User.UserName);
            if (currentLoginUser != null && currentLoginUser.UserId.HasValue && !accessibleUsers.Any(u => u.UserId == currentLoginUser.UserId.Value))
            {
                accessibleUsers.Add(new RM_DigitalSalesUserModel
                {
                    UserId = currentLoginUser.UserId.Value,
                    UserName = currentLoginUser.UserName,
                    FullName = currentLoginUser.FullName
                });
            }

            if (model.AssignedEmployeeID.HasValue && model.AssignedEmployeeID.Value > 0 && !accessibleUsers.Any(u => u.UserId == model.AssignedEmployeeID.Value))
            {
                var assignedUser = _userCache.GetById(model.AssignedEmployeeID.Value);
                if (assignedUser != null && assignedUser.UserId.HasValue)
                {
                    accessibleUsers.Add(new RM_DigitalSalesUserModel
                    {
                        UserId = assignedUser.UserId.Value,
                        UserName = assignedUser.UserName,
                        FullName = assignedUser.FullName
                    });
                }
            }

            model.ListEmployee = accessibleUsers
                .Where(u => u.UserId > 0)
                .GroupBy(u => u.UserId)
                .Select(g => g.First())
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = $"{u.FullName} ({u.UserName})",
                    Selected = (model.AssignedEmployeeID.HasValue && u.UserId == model.AssignedEmployeeID.Value)
                }).ToList();

            if ((!model.DepartmentID.HasValue || model.DepartmentID.Value <= 0) && model.AssignedEmployeeID.HasValue)
            {
                model.DepartmentID = GetDepartmentIdByUserId(model.AssignedEmployeeID.Value);
            }

            model.ListDepartment = _departmentCache.GetAll()?.Select(d => new SelectListItem
            {
                Value = d.BoPhan_ID.ToString(),
                Text = d.TenBoPhan,
                Selected = (model.DepartmentID.HasValue && d.BoPhan_ID == model.DepartmentID.Value)
            }).ToList() ?? new List<SelectListItem>();

            model.ListContract = _contractCache.GetAll()?.Select(ct => new SelectListItem
            {
                Value = ct.ContractID.ToString(),
                Text = $"{ct.ContractCode} - {ct.ContractName}",
                Selected = (model.ContractID.HasValue && ct.ContractID == model.ContractID.Value)
            }).ToList() ?? new List<SelectListItem>();
        }

        private string SaveUploadedFile(HttpPostedFileBase file, int index = 0)
        {
            try
            {
                if (file == null || file.ContentLength <= 0) return null;

                // Giới hạn 50MB theo FILE_STORAGE_RULES
                if (file.ContentLength > 52428800)
                {
                    return null;
                }

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                var forbiddenExts = new[] { 
                    ".exe", ".dll", ".bat", ".cmd", ".vbs", ".ps1", 
                    ".sh", ".com", ".msi", ".vbe", ".jse", ".wsf", 
                    ".wsh", ".scr", ".pif", ".jar", ".app", ".gadget" 
                };
                if (!string.IsNullOrEmpty(ext) && forbiddenExts.Contains(ext))
                {
                    return null;
                }

                var subFolder = DateTime.Now.ToString("yyyyMM");
                var folderPath = $"{_folderUpload}/{subFolder}";
                var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                var suffix = index > 0 ? $"_{index}" : "";
                var safeName = UtilString.ConvertToUnSign(originalName) + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + suffix + ext;
                var relativePath = folderPath + "/" + safeName;
                var physicalPath = HostingEnvironment.MapPath(relativePath);

                var dir = Path.GetDirectoryName(physicalPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                file.SaveAs(physicalPath);
                return relativePath;
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
