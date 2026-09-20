using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class MenuController : AppController
    {
        private readonly SysFunctionActionCache _apiFunctionAction = new SysFunctionActionCache();
        private readonly SysMenuCache _apiMenu = new SysMenuCache();
        private readonly string _funcName = AppProcessor.Messagor.GetMessage("Menu_Title");

        #region Index

        // GET: Menu
        [ActionType(Type = EnumActionType.View)]
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
            var data = _apiMenu.Get(out int total, dataSearch);

            var result = Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
            return result;
        }

        #endregion

        #region Main Actions

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add()
        {
            var model = new SysMenuModel { FunctionActions = CreateListFunctionActions() };
            model.ParentMenus = CreateListParentMenus(model.MenuId);
            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add(SysMenuModel model)
        {
            if (ModelState.IsValid)
            {
                var idMenu = _apiMenu.Save(new SysMenuModel
                {
                    MenuId = model.MenuId,
                    Name = model.Name,
                    FunctionActionId = model.FunctionActionId,
                    Depth = model.Depth,
                    Icon = model.Icon,
                    Position = model.Position,
                    Link = model.Link,
                    LevelMenu = model.LevelMenu,
                    IsShow = model.IsShow,
                    UseModal = model.UseModal,
                    ModalId = model.ModalId,
                    ParentId = model.ParentId
                });

                var response = CreateMessage($"{_funcName} [{model.Name}]", EnumProcessType.Add,
                    idMenu > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }

            model.FunctionActions = CreateListFunctionActions();
            model.ParentMenus = CreateListParentMenus(model.MenuId);

            return PartialView("_MenuView", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id = 0)
        {
            var model = _apiMenu.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            model.FunctionActions = CreateListFunctionActions();
            model.ParentMenus = CreateListParentMenus(model.MenuId);

            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(SysMenuModel model)
        {
            if (ModelState.IsValid)
            {
                var idMenu = _apiMenu.Save(new SysMenuModel
                {
                    MenuId = model.MenuId,
                    Name = model.Name,
                    FunctionActionId = model.FunctionActionId,
                    Depth = model.Depth,
                    Icon = model.Icon,
                    Position = model.Position,
                    Link = model.Link,
                    LevelMenu = model.LevelMenu,
                    IsShow = model.IsShow,
                    UseModal = model.UseModal,
                    ModalId = model.ModalId,
                    ParentId = model.ParentId
                });

                var response = CreateMessage($"{_funcName} [{model.Name}]", EnumProcessType.Edit,
                    idMenu > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }

            model.FunctionActions = CreateListFunctionActions();
            model.ParentMenus = CreateListParentMenus(model.MenuId);

            return PartialView("_MenuView", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id = 0)
        {
            var model = _apiMenu.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_funcName}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"<b>{_funcName} [{model.Name}]</b>");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(SysMenuModel model)
        {
            var deleted = _apiMenu.Delete(model);

            var response = CreateMessage($"{_funcName} [{model.Name}]", EnumProcessType.Delete,
                deleted ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [ChildActionOnly]
        [HttpGet]
        public ActionResult Render()
        {
            var menusViaUser = _apiMenu.GetByUserName(User?.UserName);
            if (menusViaUser != null && menusViaUser.Count > 0)
            {
                menusViaUser = menusViaUser.GroupBy(m => m.MenuId).Select(g => g.First()).ToList();
            }
            var menuViews = GetViewMenus(menusViaUser);
            var htmlMenu = CreateViewMenu(menuViews);

            return PartialView("Manager/_Menu", htmlMenu);
        }


        #endregion

        [HttpPost]
        [AllowAnonymous]
        public ActionResult LoadTreeView()
        { 
            var menusViaUser = _apiMenu.GetByUserName(User?.UserName);
            if (menusViaUser != null && menusViaUser.Count > 0)
            {
                menusViaUser = menusViaUser.GroupBy(m => m.MenuId).Select(g => g.First()).ToList();
            }
            var menuViews = GetViewMenus(menusViaUser);

            return Json(new { status = true, message = "", data = menuViews });
        }

        #region Extend Functions

        [NonAction]
        public List<SysMenuViewModel> GetViewMenus(List<SysMenuModel> data, int menuParentId = 0, string sDepth = null)
        {
            var viewMenus = new List<SysMenuViewModel>();

            if (data == null) return viewMenus;
            var appMenus = menuParentId == 0
                ? data.Where(mn => !mn.ParentId.HasValue).ToList()
                : data.Where(mn => mn.ParentId == menuParentId).ToList();

            foreach (var item in appMenus)
            {
                var arrParentDepth = sDepth != null ? sDepth.Split(',').ToList() : new List<string>();
                var arrDepth = item.Depth != null ? item.Depth.Split(',').ToList() : new List<string>();
                string sChildDepth;
                if (sDepth != null)
                {
                    arrDepth.AddRange(arrParentDepth.ToArray());
                    arrDepth = arrDepth.Distinct().ToList();
                    sChildDepth = string.Join(",", arrDepth);
                }
                else
                {
                    sChildDepth = item.Depth;
                }

                viewMenus.Add(new SysMenuViewModel
                {
                    Depth = sChildDepth,
                    ModuleName = item.ModuleName,
                    FunctionActionId = item.FunctionActionId.GetValueOrDefault(),
                    Icon = item.Icon,
                    Id = item.MenuId,
                    LevelMenu = item.LevelMenu.GetValueOrDefault(),
                    Link = item.Link,
                    Name = item.Name,
                    UseModal = item.UseModal,
                    ModalId = item.ModalId,
                    Position = item.Position.GetValueOrDefault(),
                    TitleView = item.TitleView,
                    Childs = GetViewMenus(data, item.MenuId, sChildDepth)
                });
            }

            return viewMenus;
        }

        [NonAction]
        public string CreateViewMenu(List<SysMenuViewModel> data, int iLevel = 0)
        {
            var dataHtml = new StringBuilder();
            if (data == null) return dataHtml.ToString();
            foreach (var menu in data)
                dataHtml.Append(menu.Childs.Count > 0
                    ? $"<li class=\"nav-item\" id=\"{menu.Id}\" title=\"{menu.Name}\"><a href=\"#\" class=\"nav-link dropdown-toggle collapsed\"><i class=\"nav-icon {menu.Icon}\"></i><span class=\"nav-text {(iLevel == 0 ? "fadeable" : "")}\"><span>{menu.Name}</span></span><b class=\"caret fa fa-angle-left rt-n90\"></b></a><div class=\"hideable submenu collapse\"><ul class=\"submenu-inner\">{CreateViewMenu(menu.Childs, iLevel + 1)}</ul></div><b class=\"sub-arrow\"></b></li>"
                    : $"<li class=\"nav-item\" id=\"{menu.Id}\" title=\"{menu.Name}\"><a {(menu.UseModal ? "data-modal='true'" : "")} {(menu.UseModal ? $"data-modal-id='{menu.ModalId}'" : "")} href=\"{menu.Link}\" class=\"nav-link\" name=\"{menu.Depth}\"><i class=\"nav-icon {menu.Icon}\"></i><span class=\"hideable nav-text\"><span>{menu.Name}</span></span></a></li>");

            return dataHtml.ToString();
        }

        [NonAction]
        private List<ListItem> CreateListFunctionActions()
        {
            return _apiFunctionAction.GetAll()
                .Select(fa => new ListItem
                    { Value = fa.FunctionActionId.ToString(), Text = $"{fa.Area} - {fa.Function} - {fa.Action}" })
                .OrderBy(item => item.Text).Distinct().ToList();
        }

        [NonAction]
        private List<ListItem> CreateListParentMenus(int idMenu)
        {
            return _apiMenu.GetAll().Where(mn => mn.MenuId != idMenu)
                .Select(mn => new ListItem { Value = mn.MenuId.ToString(), Text = mn.Name }).ToList();
        }

        #endregion

    }
}