using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using DocumentFormat.OpenXml.Office2010.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
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
    public class RM_BusinessPlanController : AppController
    {
        private readonly RM_BusinessPlanCache _businessPlanCache;
        private readonly RM_BusinessPlanFilePathCache _businessPlanFilePathCache;
        private readonly string _businessPlanTitle = AppProcessor.Messagor.GetMessage("BusinessPlan_Title");
        private readonly string _folderFile = ConfigurationManager.AppSettings["AppImageRoot_Path"] ?? "/Contents/File";
        public RM_BusinessPlanController()
        {
            _businessPlanCache = new RM_BusinessPlanCache();
            _businessPlanFilePathCache = new RM_BusinessPlanFilePathCache();
        }

        // GET: Cate/RM_BusinessPlan
        public ActionResult Index()
        {
            return View();
        }

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
            var dataSearch = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };
            var data = _businessPlanCache.Get(out var total, dataSearch);
            var result = Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
            return result;
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add()
        {
            var model = new RM_BusinessPlanModel();

            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(RM_BusinessPlanModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_BusinessPlan", model);
            }

            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderFile + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuFile(file, FileAttach);
                            FileAttachs.Add(FileAttach);
                        }
                    }
                }
                model.FileAttach = string.Join("||", FileAttachs);
            }
            string response;

            var result = _businessPlanCache.Save(model, User.UserName);

            if (result == 0)
                response = CreateMessage($"{_businessPlanTitle}",
                    EnumProcessType.Add,
                    EnumMsgIcon.Error);

            else if (result == -9)
                response = CreateMessage($"{_businessPlanTitle}",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_businessPlanTitle}",
                    EnumProcessType.Add,
                    EnumMsgIcon.Success);
            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id)
        {
            var model = _businessPlanCache.GetById(id);
            var files = _businessPlanFilePathCache.GetByBusinessPlanID(id);
            model.ExistingFiles = files.Select(f => new RM_BusinessPlanFilePathModel
            {
                FilePathID = f.FilePathID,
                FilePath = f.FilePath
            }).ToList();
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_businessPlanTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });

            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(RM_BusinessPlanModel model)
        {
            if (!ModelState.IsValid)
            {
                var files = _businessPlanFilePathCache.GetByBusinessPlanID(model.BusinessPlanID);
                model.ExistingFiles = files.Select(f => new RM_BusinessPlanFilePathModel
                {
                    FilePathID = f.FilePathID,
                    FilePath = f.FilePath
                }).ToList();
                return PartialView("_BusinessPlan", model);
            }
            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null)
                    {
                        var FileName = file != null ? UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName) : string.Empty;
                        var FileAttach = (!string.IsNullOrEmpty(FileName) ? _folderFile + "/" + FileName : "");
                        if (!string.IsNullOrEmpty(FileAttach))
                        {
                            LuuFile(file, FileAttach);
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
                    var file = _businessPlanFilePathCache.GetById(fileId);

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
                        _businessPlanFilePathCache.Delete(fileId, User.UserName);
                    }
                }
            }
            string response;
            var result = _businessPlanCache.Save(model, User.UserName);

            if (result == 0)
                response = CreateMessage($"{_businessPlanTitle}",
                    EnumProcessType.Edit,
                    EnumMsgIcon.Error);

            else if (result == -9)
                response = CreateMessage($"{_businessPlanTitle}",
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_businessPlanTitle}",
                    EnumProcessType.Edit,
                    EnumMsgIcon.Success);
            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _businessPlanCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_businessPlanTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_businessPlanTitle}");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(RM_BusinessPlanModel model)
        {
            var deleted = _businessPlanCache.Delete(model, User.UserName);

            var response = CreateMessage($"{_businessPlanTitle}",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        /// <summary>
        /// Hàm lưu file
        /// </summary>
        /// <param name="filebase"></param>
        /// <param name="filePath"></param>
        void LuuFile(HttpPostedFileBase filebase, string filePath)
        {
            if (filebase != null && !string.IsNullOrEmpty(filePath))
            {
                if (!FileSecurityHelper.IsAllowedUpload(filebase.FileName) || !FileSecurityHelper.IsAllowedUpload(filePath))
                    return;
                filebase.SaveAs(HostingEnvironment.MapPath(filePath));
            }
        }
    }
}