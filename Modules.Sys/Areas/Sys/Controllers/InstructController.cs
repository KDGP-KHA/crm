using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class InstructController : AppController
    {
        private readonly string _instructTitle = AppProcessor.Messagor.GetMessage("Instruct_Title");
        private readonly SysInstructCache _instructCache = new SysInstructCache();

        private List<SelectListItem> BuildTreeDropdown(List<SysInstructModel> list)
        {
            var result = new List<SelectListItem>();

            var lookup = list.GroupBy(x => x.InstructParentID ?? 0)
                 .ToDictionary(g => g.Key, g => g.ToList());

            void Build(int parentId, int level)
            {
                if (!lookup.ContainsKey(parentId)) return;

                foreach (var item in lookup[parentId].OrderBy(x => x.PositionShow))
                {
                    result.Add(new SelectListItem
                    {
                        Value = item.InstructID.ToString(),
                        Text = new string('-', level * 2) + " " + item.InstructName
                    });

                    Build(item.InstructID, level + 1);
                }
            }

            Build(0, 0);

            return result;
        }

        private List<SelectListItem> BuildInstructList(int? id)
        {
            var list = _instructCache.GetAll()
            .Where(x => !x.IsDeleted)
            .ToList();

            // nếu create mới
            if (id == null)
            {
                return BuildTreeDropdown(list);
            }

            // lấy danh sách cần loại
            var excludeIds = GetAllChildIds(list, id.Value);
            excludeIds.Add(id.Value);

            var filtered = list
                .Where(x => !excludeIds.Contains(x.InstructID))
                .ToList();

            return BuildTreeDropdown(filtered);
        }

        private List<int> GetAllChildIds(List<SysInstructModel> list, int parentId)
        {
            var result = new List<int>();

            var children = list
                .Where(x => x.InstructParentID == parentId)
                .ToList();

            foreach (var child in children)
            {
                result.Add(child.InstructID);
                result.AddRange(GetAllChildIds(list, child.InstructID));
            }

            return result;
        }


        // GET: Instruct
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
            var search = Request.Form["search"];
            var draw = Request.Form.GetValues("draw")?[0];

            var dataSearch = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
            };
            var data = _instructCache.Get(out int total, dataSearch);

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
            var model = new SysInstructModel
            {
                PositionShow = 1,
                IsActive = true
            };
            ViewBag.ParentList = BuildInstructList(null);

            return PartialView("_Add", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateInput(false)]
        public ActionResult Add(SysInstructModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ParentList = BuildInstructList(null);
                return PartialView("_Instruct", model);
            }
            string response;
            var result = _instructCache.Save(model, User.UserName);
            if (result == 0)
                response = CreateMessage($"{_instructTitle} [{model.InstructName}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_instructTitle} [{model.InstructName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_instructTitle} [{model.InstructName}]", EnumProcessType.Add, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);

        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Edit)]
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var model = _instructCache.GetById(id);
            ViewBag.ParentList = BuildInstructList(id);
            return PartialView("_Edit", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult Edit(SysInstructModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ParentList = BuildInstructList(model.InstructID);
                return PartialView("_Instruct", model);
            }
            string response;
            var result = _instructCache.Save(model, User.UserName);
            if (result == 0)
                response = CreateMessage($"{_instructTitle} [{model.InstructName}]", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9)
                response = CreateMessage($"{_instructTitle} [{model.InstructName}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else
                response = CreateMessage($"{_instructTitle} [{model.InstructName}]", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id)
        {
            var model = _instructCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_instructTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"{_instructTitle} [{model.InstructName}]");

            return PartialView("_Delete", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(SysInstructModel model)
        {
            var deleted = _instructCache.Delete(model, User.UserName);
            var response = CreateMessage($"{_instructTitle} [{model.InstructName}]",
                EnumProcessType.Delete, deleted > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        private List<InstructTreeModel> BuildTree(List<SysInstructModel> list)
        {
            var lookup = list.ToDictionary(x => x.InstructID, x => new InstructTreeModel
            {
                InstructID = x.InstructID,
                InstructName = x.InstructName,
                Content = x.Content,
                InstructParentID = x.InstructParentID
            });

            var root = new List<InstructTreeModel>();

            foreach (var item in lookup.Values)
            {
                if (item.InstructParentID.HasValue && lookup.ContainsKey(item.InstructParentID.Value))
                {
                    lookup[item.InstructParentID.Value].Children.Add(item);
                }
                else
                {
                    root.Add(item);
                }
            }

            return root;
        }

        [HttpGet]
        [AllowAnyPermission]
        public ActionResult ViewInstruct()
        {
            var all = _instructCache.GetAll()
                        .Where(x => x.IsActive)
                        .ToList();

            // lọc node hợp lệ (cha phải tồn tại)
            var valid = all
                .Where(x => x.InstructParentID == null
                         || all.Any(p => p.InstructID == x.InstructParentID))
                .ToList();

            // build tree
            var tree = BuildTree(valid);
            return PartialView("_InstructModal", valid);
        }

        [HttpGet]
        [AllowAnyPermission]
        public ActionResult GetInstructContent(int id)
        {
            var model = _instructCache.GetById(id);

            if (model == null)
                return Json(new
                {
                    success = false,
                    message = AppProcessor.Messagor.GetMessage("Instruct_Title")
                }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                data = model,
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
