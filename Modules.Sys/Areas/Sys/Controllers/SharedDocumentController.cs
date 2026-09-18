using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using Core.Sys.BaseApp;
using Core.Sys.Cache;
using Core.Sys.Caches.Sys;
using Core.Sys.Models;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Utils;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class SharedDocumentController : AppController
    {
        private readonly SharedDocumentCache _documentCache;
        private readonly SysUserCache _userCache;

        private readonly string _moduleTitle;
        private readonly string _folderUpload = "/Contents/Uploads/SharedDocuments";

        public SharedDocumentController()
        {
            _documentCache = new SharedDocumentCache();
            _userCache = new SysUserCache();
            _moduleTitle = AppProcessor.Messagor.GetMessage("SharedDoc_Title");
        }

        #region Authorization Helpers

        private bool IsUserQTHT(string userName, int? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName)) return false;
                if (userName.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                    userName.Equals("quantri", StringComparison.OrdinalIgnoreCase)) return true;

                if (!userId.HasValue || userId.Value <= 0)
                {
                    var u = _userCache.GetByUserName(userName);
                    userId = u?.UserId;
                }

                if (userId.HasValue && userId.Value > 0)
                {
                    var roles = _userCache.GetRoles(userId.Value);
                    if (roles != null && roles.Any(r => r.RoleId == 1 ||
                        (r.Name != null && (r.Name.Equals("QTHT", StringComparison.OrdinalIgnoreCase) ||
                         UtilString.ConvertToUnSign(r.Name).IndexOf("quan tri", StringComparison.OrdinalIgnoreCase) >= 0))))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback an toan
            }
            return false;
        }

        private bool CanModifyDocument(SharedDocumentModel doc, string userName)
        {
            if (doc == null || string.IsNullOrWhiteSpace(userName)) return false;
            if (IsUserQTHT(userName)) return true;
            return string.Equals(doc.CreatedBy, userName, StringComparison.OrdinalIgnoreCase);
        }

        private List<SelectListItem> GetCategorySelectList(int? selectedId = null)
        {
            var categories = _documentCache.GetCategories(true);
            var list = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = AppProcessor.Messagor.GetMessage("SharedDoc_Placeholder_Category")
                }
            };
            if (categories != null)
            {
                foreach (var c in categories)
                {
                    list.Add(new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName,
                        Selected = selectedId.HasValue && selectedId.Value == c.CategoryId
                    });
                }
            }
            return list;
        }

        private List<SelectListItem> GetSearchCategorySelectList(int? selectedId = null)
        {
            var categories = _documentCache.GetCategories(true);
            var list = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "0",
                    Text = AppProcessor.Messagor.GetMessage("SharedDoc_Search_Category")
                }
            };
            if (categories != null)
            {
                foreach (var c in categories)
                {
                    list.Add(new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName,
                        Selected = selectedId.HasValue && selectedId.Value == c.CategoryId
                    });
                }
            }
            return list;
        }

        #endregion

        #region Index & Grid List

        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Index(int? categoryId)
        {
            ViewBag.Title = _moduleTitle;
            ViewBag.IsQTHT = IsUserQTHT(User.UserName);

            var model = new SharedDocumentSearchModel
            {
                CategoryId = categoryId ?? 0,
                Categories = GetSearchCategorySelectList(categoryId)
            };
            return View(model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(SharedDocumentSearchModel model)
        {
            var search = Request.Form.GetValues("search[value]")?[0];
            var draw = Request.Form.GetValues("draw")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);

            if (pageSize <= 0) pageSize = 20;
            int pageNumber = (startRec / pageSize) + 1;

            string keyword = !string.IsNullOrWhiteSpace(model?.Keyword) ? model.Keyword : search;
            int? categoryId = (model != null && model.CategoryId.HasValue && model.CategoryId.Value > 0) ? model.CategoryId : null;
            DateTime? fromDate = model?.FromDate;
            DateTime? toDate = model?.ToDate;

            var data = _documentCache.GetList(out int total, keyword, categoryId, fromDate, toDate, pageNumber, pageSize);

            bool isQTHT = IsUserQTHT(User.UserName);
            string currentUserName = User.UserName;
            if (data != null)
            {
                foreach (var item in data)
                {
                    bool canModify = isQTHT || string.Equals(item.CreatedBy, currentUserName, StringComparison.OrdinalIgnoreCase);
                    item.CanEdit = canModify;
                    item.CanDelete = canModify;
                }
            }

            return Json(new
            {
                draw = Convert.ToInt32(draw),
                recordsTotal = total,
                recordsFiltered = total,
                data = data
            }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Create Action (Add)

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add(int? categoryId)
        {
            var model = new SharedDocumentModel
            {
                CategoryId = categoryId ?? 0,
                Categories = GetCategorySelectList(categoryId)
            };
            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add(SharedDocumentModel model, HttpPostedFileBase fileUpload)
        {
            if (fileUpload == null || fileUpload.ContentLength <= 0)
            {
                ModelState.AddModelError("FileUpload", AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileRequired"));
            }

            if (!ModelState.IsValid)
            {
                model.Categories = GetCategorySelectList(model.CategoryId);
                return PartialView("_AddDoc_View", model);
            }

            var uploadResult = SaveUploadedFile(fileUpload);
            if (!uploadResult.Success)
            {
                ModelState.AddModelError("FileUpload", uploadResult.ErrorMessage);
                model.Categories = GetCategorySelectList(model.CategoryId);
                return PartialView("_AddDoc_View", model);
            }

            model.FileName = uploadResult.SafeFileName;
            model.OriginalFileName = uploadResult.OriginalFileName;
            model.FilePath = uploadResult.RelativePath;
            model.FileSize = uploadResult.FileSize;
            model.FileExtension = uploadResult.Extension;

            var result = _documentCache.Save(model, User.UserName);
            if (result > 0)
            {
                var msg = CreateMessage(string.Format("{0} [{1}]", _moduleTitle, model.DocumentName), EnumProcessType.Add, EnumMsgIcon.Success);
                return Json(new { status = true, message = msg }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var msg = CreateMessage(string.Format("{0} [{1}]", _moduleTitle, model.DocumentName), EnumProcessType.Add, EnumMsgIcon.Error);
                return Json(new { status = false, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Edit Action

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id)
        {
            var doc = _documentCache.GetById(id);
            if (doc == null)
            {
                var notFoundMsg = CreateMessage(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileNotFound"), EnumProcessType.DataNotExist, EnumMsgIcon.Error);
                return Json(new { status = false, message = notFoundMsg }, JsonRequestBehavior.AllowGet);
            }

            if (!CanModifyDocument(doc, User.UserName))
            {
                var deniedMsg = string.Format("toastr.error('{0}');", HttpUtility.JavaScriptStringEncode(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_PermissionDenied")));
                return Json(new { status = false, message = deniedMsg }, JsonRequestBehavior.AllowGet);
            }

            doc.Categories = GetCategorySelectList(doc.CategoryId);
            return PartialView("_Edit", doc);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(SharedDocumentModel model, HttpPostedFileBase fileUpload)
        {
            var currentDoc = _documentCache.GetById(model.DocumentId);
            if (currentDoc == null)
            {
                var notFoundMsg = CreateMessage(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileNotFound"), EnumProcessType.DataNotExist, EnumMsgIcon.Error);
                return Json(new { status = false, message = notFoundMsg }, JsonRequestBehavior.AllowGet);
            }

            if (!CanModifyDocument(currentDoc, User.UserName))
            {
                var deniedMsg = string.Format("toastr.error('{0}');", HttpUtility.JavaScriptStringEncode(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_PermissionDenied")));
                return Json(new { status = false, message = deniedMsg }, JsonRequestBehavior.AllowGet);
            }

            if (!ModelState.IsValid)
            {
                model.Categories = GetCategorySelectList(model.CategoryId);
                model.FileName = currentDoc.FileName;
                model.OriginalFileName = currentDoc.OriginalFileName;
                model.FilePath = currentDoc.FilePath;
                model.FileSize = currentDoc.FileSize;
                model.FileExtension = currentDoc.FileExtension;
                return PartialView("_EditDoc_View", model);
            }

            if (fileUpload != null && fileUpload.ContentLength > 0)
            {
                var uploadResult = SaveUploadedFile(fileUpload);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("FileUpload", uploadResult.ErrorMessage);
                    model.Categories = GetCategorySelectList(model.CategoryId);
                    model.FileName = currentDoc.FileName;
                    model.OriginalFileName = currentDoc.OriginalFileName;
                    model.FilePath = currentDoc.FilePath;
                    model.FileSize = currentDoc.FileSize;
                    model.FileExtension = currentDoc.FileExtension;
                    return PartialView("_EditDoc_View", model);
                }

                model.FileName = uploadResult.SafeFileName;
                model.OriginalFileName = uploadResult.OriginalFileName;
                model.FilePath = uploadResult.RelativePath;
                model.FileSize = uploadResult.FileSize;
                model.FileExtension = uploadResult.Extension;
            }
            else
            {
                model.FileName = currentDoc.FileName;
                model.OriginalFileName = currentDoc.OriginalFileName;
                model.FilePath = currentDoc.FilePath;
                model.FileSize = currentDoc.FileSize;
                model.FileExtension = currentDoc.FileExtension;
            }

            var result = _documentCache.Save(model, User.UserName);
            if (result > 0)
            {
                var msg = CreateMessage(string.Format("{0} [{1}]", _moduleTitle, model.DocumentName), EnumProcessType.Edit, EnumMsgIcon.Success);
                return Json(new { status = true, message = msg }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var msg = CreateMessage(string.Format("{0} [{1}]", _moduleTitle, model.DocumentName), EnumProcessType.Edit, EnumMsgIcon.Error);
                return Json(new { status = false, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Delete Action

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var doc = _documentCache.GetById(id);
            if (doc == null)
            {
                var notFoundMsg = CreateMessage(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileNotFound"), EnumProcessType.DataNotExist, EnumMsgIcon.Error);
                return Json(new { status = false, message = notFoundMsg }, JsonRequestBehavior.AllowGet);
            }

            if (!CanModifyDocument(doc, User.UserName))
            {
                var deniedMsg = string.Format("toastr.error('{0}');", HttpUtility.JavaScriptStringEncode(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_PermissionDenied")));
                return Json(new { status = false, message = deniedMsg }, JsonRequestBehavior.AllowGet);
            }

            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_ConfirmDelete"), doc.DocumentName);
            return PartialView("_Delete", doc);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(SharedDocumentModel model)
        {
            var doc = _documentCache.GetById(model.DocumentId);
            if (doc == null)
            {
                var notFoundMsg = CreateMessage(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileNotFound"), EnumProcessType.DataNotExist, EnumMsgIcon.Error);
                return Json(new { status = false, message = notFoundMsg }, JsonRequestBehavior.AllowGet);
            }

            if (!CanModifyDocument(doc, User.UserName))
            {
                var deniedMsg = string.Format("toastr.error('{0}');", HttpUtility.JavaScriptStringEncode(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_PermissionDenied")));
                return Json(new { status = false, message = deniedMsg }, JsonRequestBehavior.AllowGet);
            }

            var result = _documentCache.Delete(model.DocumentId, User.UserName);
            if (result > 0)
            {
                var msg = CreateMessage(string.Format("{0} [{1}]", _moduleTitle, doc.DocumentName), EnumProcessType.Delete, EnumMsgIcon.Success);
                return Json(new { status = true, message = msg }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var msg = CreateMessage(string.Format("{0} [{1}]", _moduleTitle, doc.DocumentName), EnumProcessType.Delete, EnumMsgIcon.Error);
                return Json(new { status = false, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Detail Action

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Detail(int id)
        {
            var doc = _documentCache.GetById(id);
            if (doc == null)
            {
                var notFoundMsg = CreateMessage(AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileNotFound"), EnumProcessType.DataNotExist, EnumMsgIcon.Error);
                return Json(new { status = false, message = notFoundMsg }, JsonRequestBehavior.AllowGet);
            }

            return PartialView("_Detail", doc);
        }

        #endregion

        #region Secure File Download & Storage

        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Download(int id)
        {
            var doc = _documentCache.GetById(id);
            if (doc == null || string.IsNullOrWhiteSpace(doc.FilePath))
            {
                return HttpNotFound();
            }

            var cleanPath = doc.FilePath.Trim().Replace("~", "");
            if (cleanPath.Contains("..") || cleanPath.Contains("\\..") ||
                (!cleanPath.StartsWith("/Contents/Uploads/SharedDocuments/", StringComparison.OrdinalIgnoreCase) &&
                 !cleanPath.StartsWith("/Contents/Uploads/Documents/", StringComparison.OrdinalIgnoreCase)))
            {
                return Json(new { status = false, message = AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileDangerousBlocked") }, JsonRequestBehavior.AllowGet);
            }

            var physicalPath = HostingEnvironment.MapPath(cleanPath) ?? Server.MapPath(cleanPath);
            if (string.IsNullOrEmpty(physicalPath) || !System.IO.File.Exists(physicalPath))
            {
                return Json(new { status = false, message = AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileNotFound") }, JsonRequestBehavior.AllowGet);
            }

            // Tang luot tai nguyen tu
            _documentCache.TrackDownload(id, User.UserName);

            var downloadFileName = !string.IsNullOrWhiteSpace(doc.OriginalFileName)
                ? doc.OriginalFileName
                : (!string.IsNullOrWhiteSpace(doc.FileName) ? doc.FileName : Path.GetFileName(physicalPath));
            var mimeType = MimeMapping.GetMimeMapping(physicalPath);

            return File(physicalPath, mimeType, downloadFileName);
        }

        private UploadFileResult SaveUploadedFile(HttpPostedFileBase file)
        {
            var result = new UploadFileResult();
            try
            {
                if (file == null || file.ContentLength <= 0)
                {
                    result.ErrorMessage = AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileRequired");
                    return result;
                }

                // Gioi han 50MB (52,428,800 bytes)
                if (file.ContentLength > 52428800)
                {
                    result.ErrorMessage = AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileSizeExceeded");
                    return result;
                }

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();

                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".ppt", ".pptx"
                };

                var dangerousExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".exe", ".dll", ".bat", ".cmd", ".vbs", ".ps1", ".sh", ".com", ".msi",
                    ".vbe", ".jse", ".wsf", ".wsh", ".scr", ".pif", ".jar", ".app", ".gadget",
                    ".htm", ".html", ".js", ".asp", ".aspx", ".php", ".jsp", ".config"
                };

                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    result.ErrorMessage = AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileExtensionInvalid");
                    return result;
                }

                // Kiem tra duoi mo rong kep (double extension)
                var rawFileName = Path.GetFileName(file.FileName);
                var fileNameParts = rawFileName.Split('.');
                if (fileNameParts.Length > 2)
                {
                    for (int i = 1; i < fileNameParts.Length - 1; i++)
                    {
                        var middleExt = "." + fileNameParts[i].ToLowerInvariant();
                        if (dangerousExtensions.Contains(middleExt))
                        {
                            result.ErrorMessage = AppProcessor.Messagor.GetMessage("SharedDoc_Msg_FileDangerousBlocked");
                            return result;
                        }
                    }
                }

                var subFolder = DateTime.Now.ToString("yyyyMM");
                var folderPath = $"{_folderUpload}/{subFolder}";
                var originalFileName = Path.GetFileName(file.FileName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

                var safeBaseName = UtilString.ConvertToUnSign(nameWithoutExt).Replace(" ", "_");
                safeBaseName = Regex.Replace(safeBaseName, @"[^a-zA-Z0-9_\-]", "");
                if (string.IsNullOrWhiteSpace(safeBaseName)) safeBaseName = "tailieu";

                var safeFileName = $"{safeBaseName}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
                var relativePath = $"{folderPath}/{safeFileName}";

                var physicalPath = HostingEnvironment.MapPath(relativePath) ?? Server.MapPath(relativePath);
                var dir = Path.GetDirectoryName(physicalPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                file.SaveAs(physicalPath);

                result.Success = true;
                result.RelativePath = relativePath;
                result.SafeFileName = safeFileName;
                result.OriginalFileName = originalFileName;
                result.FileSize = file.ContentLength;
                result.Extension = ext;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }
            return result;
        }

        private class UploadFileResult
        {
            public bool Success { get; set; } = false;
            public string RelativePath { get; set; }
            public string SafeFileName { get; set; }
            public string OriginalFileName { get; set; }
            public long FileSize { get; set; }
            public string Extension { get; set; }
            public string ErrorMessage { get; set; }
        }

        #endregion
    }
}
