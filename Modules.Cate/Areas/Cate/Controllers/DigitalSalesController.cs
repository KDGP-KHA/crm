using ClosedXML.Excel;
using Core.Cate.Biz;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using System;
using System.Collections.Generic;
using System.Configuration;
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
                                   bool? isKeyProject, bool? isFollowed)
        {
            var searchModel = new RM_DigitalSalesSearchModel
            {
                Keyword = keyword,
                BusinessType = businessType.GetValueOrDefault(0),
                StatusID = statusID.GetValueOrDefault(0),
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
        public ActionResult Detail(int id)
        {
            try
            {
                var model = _salesCache.GetByID(id, User.UserName);
                if (model == null)
                {
                    return RedirectToAction("Index");
                }

                model.Note = FormatHtmlContent(model.Note);

                if (string.IsNullOrEmpty(model.CreatedByName) && !string.IsNullOrEmpty(model.CreatedBy))
                {
                    model.CreatedByName = _userCache.GetByUserName(model.CreatedBy)?.FullName;
                }

                ViewBag.Title = $"{AppProcessor.Messagor.GetMessage("DigitalSales_RecordPrefix")}: {model.Code} - {model.Title}";
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

            if (activityType.HasValue)
            {
                model.Activities = _salesCache.GetActivitiesBySalesID(id, activityType.Value);
            }

            ViewBag.CurrentFilter = activityType;
            ViewBag.CanEdit = HasDetailPermission(model, User.UserName);
            return PartialView("_DetailDiscussions", model);
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

            var activity = new RM_DigitalSalesActivityModel
            {
                DigitalSalesID = digitalSalesId,
                ActivityType = 1,
                Content = content.Trim(),
                Attachments = uploadedFiles.Count > 0 ? Newtonsoft.Json.JsonConvert.SerializeObject(uploadedFiles) : null,
                MentionedUserIDs = string.IsNullOrWhiteSpace(mentionedUserIds) ? null : mentionedUserIds.Trim(),
                MentionedNames = string.IsNullOrWhiteSpace(mentionedNames) ? null : mentionedNames.Trim()
            };

            var saveRes = _salesCache.AddActivity(activity, User.UserName);
            if (saveRes > 0)
            {
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
                    .Where(s => s.StatusID != sales.StatusID)
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
            if (currentSales != null && currentSales.StatusID == model.NewStatusID)
            {
                return Json(new
                {
                    status = false,
                    message = GetAppMessage("DigitalSales_Msg_StatusInvalid")
                });
            }

            string attachmentPath = null;
            if (attachmentFile != null && attachmentFile.ContentLength > 0)
            {
                attachmentPath = SaveUploadedFile(attachmentFile);
            }

            var code = _salesCache.ChangeStatus(model.DigitalSalesID, model.NewStatusID, model.Note, attachmentPath, User.UserName);

            if (code == 1)
            {
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

            model.AvailableStatuses = (_salesCache.GetStatusList(null) ?? new List<RM_DigitalSalesStatusModel>())
                .Where(s => sales == null || s.StatusID != sales.StatusID)
                .Select(s => new SelectListItem
                {
                    Value = s.StatusID.ToString(),
                    Text = $"[{(s.BusinessType == 1 ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity") : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project"))}] {s.StatusName}"
                }).ToList();
        }

        private void PrepareTrackingForm()
        {
            ViewBag.UserList = _userCache.GetAll()?.Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = $"{u.FullName} ({u.UserName})"
            }).ToList() ?? new List<SelectListItem>();
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
        public ActionResult AddTrackingModal(int digitalSalesId)
        {
            if (!HasDetailPermission(digitalSalesId, User.UserName))
            {
                return Content($"<div class='alert alert-warning m-3'><i class='fa fa-lock'></i> {AppProcessor.Messagor.GetMessage("DigitalSales_Msg_NoPermission")}</div>");
            }

            var model = new RM_DigitalSalesTrackingModel
            {
                DigitalSalesID = digitalSalesId,
                StartDate = DateTime.Today,
                Deadline = DateTime.Today.AddDays(3),
                Status = 1,
                IsCustomTask = true
            };

            ViewBag.UserList = _userCache.GetAll()?.Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = $"{u.FullName} ({u.UserName})"
            }).ToList() ?? new List<SelectListItem>();

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

            ViewBag.UserList = _userCache.GetAll()?.Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = $"{u.FullName} ({u.UserName})"
            }).ToList() ?? new List<SelectListItem>();

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
                PrepareTrackingForm();
                return PartialView("_TrackingForm", model);
            }

            if (attachmentFile != null && attachmentFile.ContentLength > 0)
            {
                model.AttachmentFile = SaveUploadedFile(attachmentFile);
            }

            var id = _salesCache.SaveTracking(model, User.UserName);
            if (id > 0)
            {
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
                var connStr = ConfigurationManager.ConnectionStrings["TOC.Conn.Major"]?.ConnectionString;
                if (!string.IsNullOrEmpty(connStr))
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT TOP 1 bp.BoPhan_ID FROM dbo.Sys_Users u INNER JOIN dbo.MN_BoPhan bp ON u.MaBoPhan = bp.MaBoPhan WHERE u.UserId = @UserId";
                            cmd.Parameters.AddWithValue("@UserId", userId.Value);
                            var obj = cmd.ExecuteScalar();
                            if (obj != null && obj != DBNull.Value)
                            {
                                return Convert.ToInt32(obj);
                            }
                        }
                    }
                }

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
        private List<SysUserModel> GetAccessibleEmployees()
        {
            var list = new List<SysUserModel>();
            try
            {
                var connStr = ConfigurationManager.ConnectionStrings["TOC.Conn.Major"]?.ConnectionString 
                    ?? ConfigurationManager.ConnectionStrings["CenITConnection"]?.ConnectionString;
                var currentUser = _userCache.GetByUserName(User.UserName);
                if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Email) && !string.IsNullOrEmpty(connStr))
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                SELECT DISTINCT u.UserId, u.UserName, u.FullName
                                FROM Sys_Users u
                                INNER JOIN MN_BoPhan bp ON u.MaBoPhan = bp.MaBoPhan
                                INNER JOIN Sys_UserBoPhan ub ON bp.MaBoPhan = ub.MaBoPhan
                                WHERE ub.Email = @Email AND u.IsActive = 1 AND u.IsDeleted = 0
                                ORDER BY u.FullName";
                            cmd.Parameters.AddWithValue("@Email", currentUser.Email);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    list.Add(new SysUserModel
                                    {
                                        UserId = Convert.ToInt32(reader["UserId"]),
                                        UserName = reader["UserName"]?.ToString(),
                                        FullName = reader["FullName"]?.ToString()
                                    });
                                }
                            }
                        }
                    }
                }

                if (list.Count == 0 && !string.IsNullOrEmpty(connStr))
                {
                    var accessibleDepts = GetAccessibleDepartments();
                    if (accessibleDepts != null && accessibleDepts.Count > 0)
                    {
                        var deptIds = string.Join(",", accessibleDepts.Select(d => d.BoPhan_ID));
                        using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = $@"
                                    SELECT DISTINCT u.UserId, u.UserName, u.FullName
                                    FROM Sys_Users u
                                    INNER JOIN MN_BoPhan bp ON u.MaBoPhan = bp.MaBoPhan
                                    WHERE bp.BoPhan_ID IN ({deptIds}) AND u.IsActive = 1 AND u.IsDeleted = 0
                                    ORDER BY u.FullName";
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        list.Add(new SysUserModel
                                        {
                                            UserId = Convert.ToInt32(reader["UserId"]),
                                            UserName = reader["UserName"]?.ToString(),
                                            FullName = reader["FullName"]?.ToString()
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback safe
            }
            return list;
        }

        [HttpGet]
        public JsonResult GetEmployeesByDepartment(int departmentId)
        {
            List<SysUserModel> users;
            if (departmentId > 0)
            {
                users = _userCache.GetByBoPhanAndChucVu(departmentId, null) ?? new List<SysUserModel>();
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
                    var accessibleDepts = GetAccessibleDepartments();
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

            var result = users.Select(x => new
            {
                Value = x.UserId,
                Text = $"{x.FullName} ({x.UserName})"
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
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

                if (sales != null)
                {
                    // 1. Người tạo hồ sơ có toàn quyền ngay lập tức (0ms, không tốn query)
                    if (!string.IsNullOrEmpty(sales.CreatedBy) && sales.CreatedBy.Equals(userName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // 2. Tài khoản quản trị mặc định
                    if (userName.Equals("admin", StringComparison.OrdinalIgnoreCase) || userName.Equals("quantri", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // 3. Nhân sự phụ trách / AM chủ trì
                    var currentUser = _userCache.GetByUserName(userName);
                    var currentUserId = currentUser?.UserId;

                    if (currentUserId.HasValue && currentUserId.Value > 0 && sales.AssignedEmployeeID == currentUserId.Value)
                    {
                        return true;
                    }

                    // 4. Những người được check quyền cập nhật trạng thái (IsAM = true)
                    var members = sales.Members;
                    if ((members == null || members.Count == 0) && sales.DigitalSalesID > 0)
                    {
                        members = _salesCache.GetMembersBySalesID(sales.DigitalSalesID);
                    }

                    if (members != null && members.Count > 0)
                    {
                        var hasStatusPermission = members.Any(m =>
                            m.IsAM && (
                                (!string.IsNullOrEmpty(m.UserName) && m.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)) ||
                                (currentUserId.HasValue && currentUserId.Value > 0 && m.UserID == currentUserId.Value)
                            )
                        );
                        if (hasStatusPermission) return true;
                    }
                }

                // 5. Kiểm tra QTHT qua vai trò (roles) nếu các điều kiện trên chưa khớp
                if (IsUserQTHT(userName)) return true;
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
            model.ListStatus = _salesCache.GetStatusList(bType)?.Select(s => new SelectListItem
            {
                Value = s.StatusID.ToString(),
                Text = bType.HasValue ? s.StatusName : $"[{(s.BusinessType == 1 ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity") : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project"))}] {s.StatusName}",
                Selected = s.StatusID == model.StatusID
            }).ToList() ?? new List<SelectListItem>();

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
            if (currentLoginUser != null && !accessibleUsers.Any(u => u.UserId == currentLoginUser.UserId))
            {
                accessibleUsers.Add(currentLoginUser);
            }

            if (model.AssignedEmployeeID.HasValue && model.AssignedEmployeeID.Value > 0 && !accessibleUsers.Any(u => u.UserId == model.AssignedEmployeeID.Value))
            {
                var assignedUser = _userCache.GetById(model.AssignedEmployeeID.Value);
                if (assignedUser != null)
                {
                    accessibleUsers.Add(assignedUser);
                }
            }

            model.ListEmployee = accessibleUsers
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
