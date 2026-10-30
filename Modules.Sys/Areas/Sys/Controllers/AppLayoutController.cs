using System;
using System.IO;
using System.IO.Compression;
using System.Web.Mvc;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Helpers;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class AppLayoutController : AppController
    {
        private readonly string _funcName = AppProcessor.Messagor.GetMessage("AppLayout_Title");
        private readonly SysLayoutCache _sysLayoutCache = new SysLayoutCache();

        // GET: Layout
        [ActionType(Type = EnumActionType.View)]
        //[HttpGet]
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
            var data = _sysLayoutCache.Get(out var total, dataSearch);

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
            var model = new SysLayoutModel();
            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add(SysLayoutModel model)
        {
            if (!ModelState.IsValid) return PartialView("_AppLayout", model);
            if (model.ZipFile != null)
            {
                var dirServer = Server.MapPath(@"~\Views\Skin\");

                using (var archive = new ZipArchive(model.ZipFile.InputStream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        FileSecurityHelper.SafeExtractZipEntry(entry, dirServer, allowExecutable: false);
                    }
                }
            }

            var idLayout = _sysLayoutCache.Save(new SysLayoutModel
            {
                LayoutId = 0,
                LayoutName = model.LayoutName,
                LayoutView = model.LayoutView,
                Note = model.Note,
                NumberContentPanel = model.NumberContentPanel,
                NumberCol = model.NumberCol,
                Creator = User.UserName,
                CreateDated = DateTime.Now,
                Deleted = false
            });

            var response = CreateMessage($"{_funcName} [{model.LayoutName}]", EnumProcessType.Add,
                idLayout > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id = 0)
        {
            var model = _sysLayoutCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });

            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(SysLayoutModel model)
        {
            if (!ModelState.IsValid) return PartialView("_AppLayout", model);
            if (model.ZipFile != null)
            {
                var dirServer = Server.MapPath(@"~\Views\Skin\");

                using (var archive = new ZipArchive(model.ZipFile.InputStream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        FileSecurityHelper.SafeExtractZipEntry(entry, dirServer, allowExecutable: false);
                    }
                }
            }

            var idLayout = _sysLayoutCache.Save(new SysLayoutModel
            {
                LayoutId = model.LayoutId,
                LayoutName = model.LayoutName,
                LayoutView = model.LayoutView,
                Note = model.Note,
                NumberContentPanel = model.NumberContentPanel,
                NumberCol = model.NumberCol,
                Updater = User.UserName,
                UpdateDated = DateTime.Now,
                Deleted = false
            });

            var response = CreateMessage($"{_funcName} [{model.LayoutName}]", EnumProcessType.Edit,
                idLayout > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id = 0)
        {
            var model = _sysLayoutCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"<b>{_funcName} [{model.LayoutName}]</b>");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(SysLayoutModel model)
        {
            var deleted = _sysLayoutCache.Delete(model);

            var response = CreateMessage($"{_funcName} [{model.LayoutName}]", EnumProcessType.Delete,
                deleted ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ChangeActive(int id = 0)
        {
            var model = _sysLayoutCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmActiveMessage = string.Format(AppProcessor.Messagor.GetMessage("Common_Confirm_Active"),
                $"<b>{_funcName} [{model.LayoutName}]</b>");

            return PartialView("_ChangeActive", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ChangeActive(SysLayoutModel model)
        {
            model.Updater = User.UserName;
            var isSuccess = _sysLayoutCache.Activated(model);
            //if (isSuccess) MappingModuleWidget = new Dictionary<string, List<SysPanelModuleModel>>();
            var response = CreateMessage($"{_funcName} [{model.LayoutName}]", EnumProcessType.Edit,
                isSuccess ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }
    }
}