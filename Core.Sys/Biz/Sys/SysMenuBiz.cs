using System.Collections.Generic;
using System.Linq;
using Core.Sys.Models.Sys;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Core.Sys.Biz.Sys
{
    public class SysMenuBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Sys";
        private readonly string _sysMenuDelete = "Sys_Menu_Delete";
        private readonly string _sysMenuGet = "Sys_Menu_Get";
        private readonly string _sysMenuGetAll = "Sys_Menu_GetAll";
        private readonly string _sysMenuGetById = "Sys_Menu_GetById";
        private readonly string _sysMenuGetByUserId = "Sys_Menu_GetByUserId";
        private readonly string _sysMenuGetByUserName = "Sys_Menu_GetByUserName";
        private readonly string _sysMenuGetByUserAndArea = "Sys_Menu_GetByUserAndArea";
        private readonly string _sysMenuGetMenuChilds = "Sys_Menu_GetMenuChilds";
        private readonly string _sysMenuSave = "Sys_Menu_Save";

        private List<SysMenuModel> Get(out int total, BaseSearchModel search)
        {
            search = search ?? new BaseSearchModel
            {
                Search = null,
                Order = "1",
                OrderDir = "ASC",
                StartIndex = 0,
                PageSize = -1
            };

            var listMenus = AppProcessor.ProcedureProvider.ExecuteTypedList<SysMenuModel>(_sysMenuGet,
                DATA_PROVIDER_NAME,
                search.Search,
                search.Order,
                search.OrderDir,
                search.StartIndex,
                search.PageSize);

            total = 0;
            if (listMenus != null && listMenus.Count > 0)
                total = int.Parse(listMenus.First()?.TotalRow.ToString() ?? "0");
            return listMenus;
        }

        private SysMenuModel LoadDetail(int menuId)
        {
            var dataMenu =
                AppProcessor.ProcedureProvider.ExecuteScalarObject<SysMenuModel>(_sysMenuGetById, DATA_PROVIDER_NAME,
                    menuId);
            return dataMenu;
        }

        private List<SysMenuModel> Get(int userId)
        {
            var listMenus =
                AppProcessor.ProcedureProvider.ExecuteTypedList<SysMenuModel>(_sysMenuGetByUserId, DATA_PROVIDER_NAME,
                    userId);
            if (listMenus != null && listMenus.Count > 0)
            {
                listMenus = listMenus.GroupBy(m => m.MenuId).Select(g => g.First()).ToList();
            }
            return listMenus;
        }

        private List<SysMenuModel> Get(string userName)
        {
            var listMenus =
                AppProcessor.ProcedureProvider.ExecuteTypedList<SysMenuModel>(_sysMenuGetByUserName, DATA_PROVIDER_NAME,
                    userName);
            if (listMenus != null && listMenus.Count > 0)
            {
                listMenus = listMenus.GroupBy(m => m.MenuId).Select(g => g.First()).ToList();
            }
            return listMenus;
        }

        private List<SysMenuModel> LoadListChilds(int menuId)
        {
            var listMenus =
                AppProcessor.ProcedureProvider.ExecuteTypedList<SysMenuModel>(_sysMenuGetMenuChilds, DATA_PROVIDER_NAME,
                    menuId);
            return listMenus;
        }

        public bool Delete(SysMenuModel model)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_sysMenuDelete, DATA_PROVIDER_NAME, model.MenuId);
            return result == model.MenuId;
        }

        public SysMenuModel GetById(int menuId)
        {
            var menu = LoadDetail(menuId);
            return menu;
        }

        public List<SysMenuModel> GetByUserName(string userName)
        {
            var menus = Get(userName);
            return menus;
        }

        public List<SysMenuModel> GetByUserId(int userId)
        {
            var menus = Get(userId);
            return menus;
        }

        public List<SysMenuModel> GetMenuChilds(int menuId)
        {
            var menus = LoadListChilds(menuId);
            return menus;
        }

        public List<SysMenuModel> GetList(out int total, BaseSearchModel search = null)
        {
            var listMenus = Get(out total, search);
            return listMenus;
        }

        public int Save(SysMenuModel model)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_sysMenuSave, DATA_PROVIDER_NAME,
                model.MenuId,
                model.ParentId,
                model.FunctionActionId,
                model.Name,
                model.Position,
                model.LevelMenu,
                model.Depth,
                model.Link,
                model.Icon,
                model.IsShow, model.UseModal, model.ModalId);

            return result.GetValueOrDefault(0);
        }

        public List<SysMenuModel> GetAll()
        {
            var allMenus =
                AppProcessor.ProcedureProvider.ExecuteTypedList<SysMenuModel>(_sysMenuGetAll, DATA_PROVIDER_NAME);
            return allMenus;
        }

        public List<SysMenuModel> GetByUserAndArea(string userName, string areaName)
        {
            var listMenus =
                AppProcessor.ProcedureProvider.ExecuteTypedList<SysMenuModel>(_sysMenuGetByUserAndArea, DATA_PROVIDER_NAME,
                    userName, areaName);
            return listMenus;
        }
    }
}