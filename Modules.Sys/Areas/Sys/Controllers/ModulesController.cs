using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using FastMember;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.BaseApps;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Helpers;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class ModulesController : AppController
    {
        private readonly string _funcName = AppProcessor.Messagor.GetMessage("Module_Title");
        private readonly SysModuleCache _sysModuleCache = new SysModuleCache();
        private readonly SysUserCache _sysUserCache = new SysUserCache();

        // GET: Modules
        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
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
            var data = _sysModuleCache.Get(out int total, dataSearch);

            var result = Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
            return result;
        }

        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Install()
        {
            var moduleModel = new SysModuleModel();
            return PartialView("_Install", moduleModel);
        }

        [ActionType(Type = EnumActionType.View)]
        [HttpPost]
        public ActionResult Install(SysModuleModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_Install", model);

            var unzipSuccess = UnzipModule(model);

            int? idModule = 0;
            if (unzipSuccess)
                idModule = _sysModuleCache.Save(new SysModuleModel
                {
                    ModuleId = 0,
                    ModuleName = model.ModuleName,
                    AssemblyName = model.AssemblyName,
                    MainController = model.MainController,
                    ModuleView = model.ModuleView,
                    Description = model.Description,
                    Creator = User.UserName,
                    CreateDated = DateTime.Now,
                    Deleted = false
                });

            var response = CreateMessage($"{_funcName} [{model.ModuleName}]", EnumProcessType.Add,
                idModule > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add()
        {
            var model = new SysModuleModel();
            return PartialView("_Add", model);
        }

        [ActionType(Type = EnumActionType.View)]
        [HttpPost]
        public ActionResult Add(SysModuleModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_Module", model);

            var unzipSuccess = UnzipModule(model);
            int? idModule = 0;
            if (unzipSuccess)
                idModule = _sysModuleCache.Save(new SysModuleModel
                {
                    ModuleId = 0,
                    ModuleName = model.ModuleName,
                    AssemblyName = model.AssemblyName,
                    MainController = model.MainController,
                    ModuleView = model.ModuleView,
                    DefaultAction = model.DefaultAction,
                    Description = model.Description,
                    Icon = model.Icon,
                    Creator = User.UserName,
                    CreateDated = DateTime.Now,
                    Deleted = false
                });

            //if (idModule > 0) MappingModuleWidget = new Dictionary<string, List<SysPanelModuleModel>>();
            var response = CreateMessage($"{_funcName} [{model.ModuleName}]", EnumProcessType.Add,
                idModule > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id = 0)
        {
            var model = _sysModuleCache.GetById(id);
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
        public ActionResult Edit(SysModuleModel model)
        {
            if (!ModelState.IsValid) return PartialView("_Module", model);

            var unzipSuccess = UnzipModule(model);
            int? idModule = 0;
            if (unzipSuccess)
                idModule = _sysModuleCache.Save(new SysModuleModel
                {
                    ModuleId = model.ModuleId,
                    ModuleName = model.ModuleName,
                    MainController = model.MainController,
                    AssemblyName = model.AssemblyName,
                    ModuleView = model.ModuleView,
                    DefaultAction = model.DefaultAction,
                    Description = model.Description,
                    Icon = model.Icon,
                    Updater = User.UserName,
                    UpdateDated = DateTime.Now,
                    Deleted = false
                });

            //if (idModule > 0) MappingModuleWidget = new Dictionary<string, List<SysPanelModuleModel>>();
            var response = CreateMessage($"{_funcName} [{model.ModuleName}]", EnumProcessType.Edit,
                idModule > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id = 0)
        {
            var model = _sysModuleCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"<b>{_funcName} [{model.ModuleName}]</b>");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(SysModuleModel model)
        {
            var deleted = _sysModuleCache.Delete(model);
            //if (deleted) MappingModuleWidget = new Dictionary<string, List<SysPanelModuleModel>>();
            var response = CreateMessage($"{_funcName} [{model.ModuleName}]", EnumProcessType.Delete,
                deleted ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [HttpGet]
        [AjaxOnly]
        [ActionType(Type = EnumActionType.Edit)]
        [AllowAnyPermission]
        public ActionResult UpdatePanelModule(string panelName)
        {
            var activeModules = _sysModuleCache.GetNotInContentPanel(panelName);
            var panelModuleModel = new SysPanelModuleModel
            {
                PanelName = panelName,
                ActiveModules = activeModules != null
                    ? activeModules
                        .Where(m => !string.IsNullOrEmpty(m.MainController) || !string.IsNullOrEmpty(m.ModuleView))
                        .Select(m => new ListItem(m.ModuleName, m.ModuleId.ToString())).ToList()
                    : new List<ListItem>()
            };

            return PartialView("_UpdatePanelModule", panelModuleModel);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        [AllowAnyPermission]
        public ActionResult UpdatePanelModule(SysPanelModuleModel model)
        {
            if (ModelState.IsValid)
            {
                var idContenModule = _sysModuleCache.SaveContentPanelModule(model.PanelName, new SysModuleModel
                {
                    ModuleId = model.ModuleId,
                    Updater = User.UserName
                });
                //if (idContenModule > 0)
                //{
                //    MappingModuleWidget = new Dictionary<string, List<SysPanelModuleModel>>();
                //    InitModulePanel();
                //}

                var response = CreateMessage($"{_funcName}", EnumProcessType.Edit,
                    idContenModule > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response, panelName = model.PanelName });
            }

            var activeModules = _sysModuleCache.GetNotInContentPanel(model.PanelName);
            model.ActiveModules = activeModules != null
                ? activeModules.Select(m => new ListItem(m.ModuleName, m.ModuleName)).ToList()
                : new List<ListItem>();

            return PartialView("_PanelModule", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DelPanelModule(int moduleId, string panelName)
        {
            var module = _sysModuleCache.GetById(moduleId);
            if (module == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });

            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Common_Confirm_Del_PanelModule"),
                $"<b>{module.ModuleName}</b>", panelName);

            var model = new SysPanelModuleModel
            {
                ModuleId = moduleId,
                PanelName = panelName,
                ModuleName = module.ModuleName,
                ModuleView = module.ModuleView,
                AssemblyName = module.AssemblyName
            };
            return PartialView("_DelModulePanel", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DelPanelModule(SysPanelModuleModel model)
        {
            var deleted = _sysModuleCache.DeleteContentPanelModule(model.ModuleId, model.PanelName, User.UserName);
            //if (deleted)
            //{
            //    MappingModuleWidget = new Dictionary<string, List<SysPanelModuleModel>>();
            //    InitModulePanel();
            //}

            var response = CreateMessage($"{_funcName} [{model.ModuleName}]", EnumProcessType.Delete,
                deleted ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response, panelName = model.PanelName });
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult SaveModulesContent(List<SysPanelModuleModel> panelModules)
        {
            string response;
            if (panelModules == null)
            {
                response = CreateMessage($"{_funcName}",
                    EnumProcessType.DataNotExist, EnumMsgIcon.Error);
            }
            else
            {
                var dataPanelModules = new DataTable();
                using (var reader = ObjectReader.Create(panelModules, "ContentPanelId", "ModuleId", "OrderBy"))
                {
                    dataPanelModules.Load(reader);
                }

                var idContentPanelModule = _sysModuleCache.SaveListContentPanelModule(dataPanelModules, User.UserName);

                //if (idContentPanelModule > 0)
                //{
                //    MappingModuleWidget = new Dictionary<string, List<SysPanelModuleModel>>();
                //    InitModulePanel();
                //}

                response = CreateMessage(AppProcessor.Messagor.GetMessage("PanelModule_Title"), EnumProcessType.Edit,
                    idContentPanelModule > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            }

            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Permission(int id)
        {
            var module = _sysModuleCache.GetById(id);
            if (module == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });

            var permissionUsers = _sysModuleCache.GetPermissionUsers(id);

            var permissionModule = new SysPermissionModuleModel
            {
                Description = module.Description,
                ModuleName = module.ModuleName,
                ModuleId = module.ModuleId,
                Users = _sysUserCache.GetAll().Where(u => u.IsActive)
                    .Select(u => new ListItem(u.FullName, u.UserId.ToString())).ToList(),
                PermissionUserIDs = permissionUsers != null
                    ? string.Join(",", permissionUsers.Select(u => u.UserId).ToList())
                    : string.Empty
            };

            return PartialView("_Permission", permissionModule);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Permission(SysPermissionModuleModel model)
        {
            if (ModelState.IsValid)
            {
                var retId = _sysModuleCache.SavePermissionModule(model);
                var response =
                    CreateMessage(
                        $"{AppProcessor.Messagor.GetMessage("Module_Message_Permision")}{_funcName} [{model.ModuleName}]",
                        EnumProcessType.NonFormat, retId > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }

            model.Users = _sysUserCache.GetAll().Where(u => u.IsActive)
                .Select(u => new ListItem(u.FullName, u.UserId.ToString())).ToList();

            return PartialView("_PermissionModule", model);
        }

        #region Private function

        private void ActiveModule(string assemblyPath)
        {
            var pluginAssembly = Assembly.Load(System.IO.File.ReadAllBytes(assemblyPath));

            var availableTypes = new List<Type>();
            availableTypes.AddRange(pluginAssembly.GetTypes());

            // get a list of objects that implement the IPlugableRule interface AND have the RulePluginAttribute
            var plugableRuleList = availableTypes.FindAll(t => t.BaseType == typeof(BaseController));

            // conver the list of Objects to an instantiated list of ICalculators
            plugableRuleList.ConvertAll(t => Activator.CreateInstance(t) as BaseController);
        }

        private bool UnzipModule(SysModuleModel model)
        {
            try
            {
                /*
                 * Setup file
                 * Contains:
                 *  - App_Data: File[StoredProcedures.xml] - Copy file store procedure to folder App_Data\Modules
                 *  - bin: File[dll] - Copy dll module and library necessary
                 *  - Configs: File[AppSettings] - Copy contents appsetting
                 *  - Contents: Folder[Modules/{ModuleName}] - Copy all file and folder
                 *  - Views: All folders - Copy all folder and file
                 */

                if (model.ModuleFileZip == null)
                    return true;

                var dirServer = Server.MapPath("~/");

                using (var archive = new ZipArchive(model.ModuleFileZip.InputStream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var fullFilePath = FileSecurityHelper.SafeExtractZipEntry(entry, dirServer, allowExecutable: true);
                        if (string.IsNullOrEmpty(Path.GetExtension(entry.FullName)))
                        {
                            continue;
                        }

                        var fileInfo = new FileInfo(fullFilePath);

                        if (System.IO.File.Exists(fullFilePath) && fileInfo.Extension == ".dll")
                            ActiveModule(fullFilePath);

                        if (!System.IO.File.Exists(fullFilePath) || fileInfo.Directory.Name != "Modules" ||
                            fileInfo.Extension != ".config") continue;

                        var dataModuleSettings = ConfigHelper.GetSettingsByPath(fullFilePath);
                        if (dataModuleSettings == null || dataModuleSettings.Count <= 0) continue;
                        if (dataModuleSettings.ContainsKey("MainController"))
                            model.MainController = dataModuleSettings["MainController"].ToString();

                        if (dataModuleSettings.ContainsKey("ModuleView"))
                            model.ModuleView = dataModuleSettings["ModuleView"].ToString();

                        if (dataModuleSettings.ContainsKey("AssemblyName"))
                            model.AssemblyName = dataModuleSettings["AssemblyName"].ToString();
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}