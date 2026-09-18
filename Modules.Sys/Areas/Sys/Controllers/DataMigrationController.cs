using System;
using System.Web.Mvc;
using Core.Cate.Biz;
using Core.Cate.Models;
using Core.Sys.BaseApp;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class DataMigrationController : AppController
    {
        private readonly RM_DataMigrationBiz _migrationBiz;

        public DataMigrationController()
        {
            _migrationBiz = new RM_DataMigrationBiz();
        }

        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                ViewBag.RecentMigrations = _migrationBiz.GetRecentMigrations(10);
                ViewBag.ActiveStatuses = _migrationBiz.GetActiveStatuses();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View();
        }

        [ActionType(Type = EnumActionType.View)]
        [HttpPost]
        public ActionResult Preview(int sourceType, int sourceId)
        {
            try
            {
                if (sourceId <= 0)
                {
                    return Json(new { status = false, message = "Vui lòng nhập ID hợp lệ (> 0)." });
                }

                var preview = _migrationBiz.Preview(sourceType, sourceId);
                return Json(new { status = true, data = preview });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [ActionType(Type = EnumActionType.Create)]
        [HttpPost]
        public ActionResult Execute(DataMigrationRequestModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { status = false, message = "Dữ liệu yêu cầu không hợp lệ." });
                }

                if (model.SourceID <= 0)
                {
                    return Json(new { status = false, message = "Vui lòng nhập ID nguồn hợp lệ (> 0)." });
                }

                string username = User != null && !string.IsNullOrWhiteSpace(User.UserName) ? User.UserName : "quantri";
                var result = _migrationBiz.ExecuteMigration(model, username);

                return Json(new { status = result.Success, message = result.Message, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Lỗi thực thi: " + ex.Message });
            }
        }

        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult GetRecent()
        {
            try
            {
                var list = _migrationBiz.GetRecentMigrations(15);
                return Json(new { status = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
