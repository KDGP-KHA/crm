using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web.Hosting;
using System.Web;
using System.Web.Mvc;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Helpers;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Utils;
using System.Linq;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Modules.Cate.Areas.Cate.Controllers
{
    public class ProductServiceController : AppController
    {
        private readonly Cate_ProductServiceCache _productServiceCache;
        private readonly RM_ProductServiceFilePathCache _productServiceFilePathCache;
        private readonly string _productServiceTitle = AppProcessor.Messagor.GetMessage("ProductService_Title");
        private readonly string _folderFile = ConfigurationManager.AppSettings["AppImageRoot_Path"] ?? "/Contents/File";

        public ProductServiceController()
        {
            _productServiceCache = new Cate_ProductServiceCache();
            _productServiceFilePathCache = new RM_ProductServiceFilePathCache();
        }

        /// <summary>
        /// Hiển thị màn hình danh sách sản phẩm dịch vụ.
        /// </summary>
        [HttpGet]
        public ActionResult Index()
        {
            var model = new Cate_ProductServiceSearchModel();
            return View(model);
        }

        /// <summary>
        /// Tải danh sách sản phẩm dịch vụ để hiển thị lên lưới.
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(Cate_ProductServiceSearchModel model)
        {
            var data = _productServiceCache.GetAllChild(model.Search, model.SearchFile);

            return Json(
                new
                {
                    data = data ?? new List<Cate_ProductServiceModel>()
                },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup thêm mới sản phẩm dịch vụ.
        /// </summary>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add()
        {
            var model = new Cate_ProductServiceModel
            {
                selectListItems = _productServiceCache.GetAllChild()
            };

            return PartialView("_Add", model);
        }

        /// <summary>
        /// Xử lý thêm mới sản phẩm dịch vụ.
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(Cate_ProductServiceModel model)
        {
            if (!ModelState.IsValid)
            {
                model.selectListItems = _productServiceCache.GetAllChild();
                return PartialView("_ProductService", model);
            }

            // Lưu danh sách file đính kèm
            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null && FileSecurityHelper.IsAllowedUpload(file.FileName))
                    {
                        var FileName = UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName);
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

            var result = _productServiceCache.Save(model, User.UserName);
            var response = BuildSaveMessage(model, result, EnumProcessType.Add);

            return Json(new { status = result > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup cập nhật sản phẩm dịch vụ.
        /// </summary>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id)
        {
            var model = _productServiceCache.GetById(id);
            if (model == null)
            {
                return Json(
                    new
                    {
                        status = false,
                        message = CreateMessage(_productServiceTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                    });
            }

            //var files = _productServiceFilePathCache.GetByProductServiceID(id);
            //model.ExistingFiles = files.Select(f => new RM_ProductServiceFilePathModel
            //{
            //    FilePathID = f.FilePathID,
            //    FilePath = f.FilePath
            //}).ToList();

            model.selectListItems = _productServiceCache.GetAllChild();
            return PartialView("_Edit", model);
        }

        /// <summary>
        /// Xử lý cập nhật sản phẩm dịch vụ.
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(Cate_ProductServiceModel model)
        {
            if (!ModelState.IsValid)
            {
                // var files = _productServiceFilePathCache.GetByProductServiceID(model.ProductServiceID);
                // model.ExistingFiles = files.Select(f => new RM_ProductServiceFilePathModel
                // {
                //     FilePathID = f.FilePathID,
                //     FilePath = f.FilePath
                // }).ToList();
                model.selectListItems = _productServiceCache.GetAllChild();
                return PartialView("_ProductService", model);
            }

            if (model.DinhKemFile != null && model.DinhKemFile.Count > 0)
            {
                List<string> FileAttachs = new List<string>();
                foreach (var file in model.DinhKemFile)
                {
                    if (file != null && FileSecurityHelper.IsAllowedUpload(file.FileName))
                    {
                        var FileName = UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)) + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Path.GetExtension(file.FileName);
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
                    var file = _productServiceFilePathCache.GetById(fileId);

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
                        _productServiceFilePathCache.Delete(fileId, User.UserName);
                    }
                }
            }

            var result = _productServiceCache.Save(model, User.UserName);
            var response = BuildSaveMessage(model, result, EnumProcessType.Edit);

            return Json(new { status = result > 0, message = response }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup xem chi tiết sản phẩm dịch vụ.
        /// </summary>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult View(int id)
        {
            var model = _productServiceCache.GetById(id);
            if (model == null)
            {
                return Json(
                    new
                    {
                        status = false,
                        message = CreateMessage(_productServiceTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                    });
            }

            // var files = _productServiceFilePathCache.GetByProductServiceID(id);
            // model.ExistingFiles = files.Select(f => new RM_ProductServiceFilePathModel
            // {
            //     FilePathID = f.FilePathID,
            //     FilePath = f.FilePath
            // }).ToList();

            model.selectListItems = _productServiceCache.GetAllChild();
            return PartialView("_Detail", model);
        }

        /// <summary>
        /// Hiển thị popup xác nhận xóa sản phẩm dịch vụ.
        /// </summary>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _productServiceCache.GetById(id);
            if (model == null)
            {
                return Json(
                    new
                    {
                        status = false,
                        message = CreateMessage(_productServiceTitle, EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                    });
            }

            ViewBag.ConfirmMessage = string.Format(
                AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                string.Format("{0} [{1}]", _productServiceTitle, model.NameProduct));

            return PartialView("_Delete", model);
        }

        /// <summary>
        /// Xử lý xóa sản phẩm dịch vụ.
        /// </summary>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(Cate_ProductServiceModel model)
        {
            var deleted = _productServiceCache.Delete(model, User.UserName);
            var response = CreateMessage(
                string.Format("{0} [{1}]", _productServiceTitle, model.NameProduct),
                EnumProcessType.Delete,
                deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);

            return Json(new { status = deleted > 0, message = response });
        }

        /// <summary>
        /// Tạo message phản hồi theo mã kết quả khi thêm hoặc cập nhật dữ liệu.
        /// </summary>
        private string BuildSaveMessage(Cate_ProductServiceModel model, int result, EnumProcessType processType)
        {
            if (result == 0)
            {
                return CreateMessage(
                    string.Format("{0} [{1}]", _productServiceTitle, model.NameProduct),
                    processType,
                    EnumMsgIcon.Error);
            }

            if (result == -9)
            {
                return CreateMessage(
                    string.Format("{0} [{1}]", _productServiceTitle, model.NameProduct),
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            }

            if (result == -8)
            {
                return CreateMessage(
                    string.Format("{0} [{1}]", _productServiceTitle, model.CodeProduct),
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            }

            if (result == -7)
            {
                return CreateMessage(
                    string.Format("{0} [{1}]", _productServiceTitle, model.ShortNameProduct),
                    EnumProcessType.DataExisted,
                    EnumMsgIcon.Error);
            }

            return CreateMessage(
                string.Format("{0} [{1}]", _productServiceTitle, model.NameProduct),
                processType,
                EnumMsgIcon.Success);
        }

        /// <summary>
        /// Hàm lưu file an toàn
        /// </summary>
        void LuuFile(HttpPostedFileBase filebase, string filePath)
        {
            if (filebase == null || string.IsNullOrWhiteSpace(filePath)) return;

            if (!FileSecurityHelper.IsAllowedUpload(filebase.FileName))
            {
                return;
            }

            var physicalPath = HostingEnvironment.MapPath(filePath);
            if (string.IsNullOrWhiteSpace(physicalPath)) return;

            var dir = Path.GetDirectoryName(physicalPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            filebase.SaveAs(physicalPath);
        }
    }
}
