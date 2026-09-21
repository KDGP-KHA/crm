using Core.Cate.Biz;
using Core.Cate.Caches;
using Core.Cate.Models;
using Core.Cate.Services;
using Core.Sys.BaseApp;
using System;
using System.Collections.Generic;
using System.Web.Hosting;
using System.Web;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Utils;
using TSFramework.Libs.Processors;
using System.IO;
using System.Configuration;
using System.Linq;
using Core.Sys.Caches.Sys;
using Newtonsoft.Json;

namespace Modules.Cate.Areas.Cate.Controllers
{
    public class ReviewBatchItemController : AppController
    {
        private readonly RM_ReviewBatchItemCache _reviewBatchItemCache;
        private readonly RM_ReviewBatchCache _reviewBatchCache;
        private readonly RM_ReviewBatchItemBiz _reviewBatchItemBiz;
        private readonly RM_DigitalSalesCache _digitalSalesCache;
        private readonly SysUserCache _userCache;
        private readonly SysUserBoPhanCache _userBoPhanCache;
        private readonly RM_DigitalSalesWorkflowCache _workflowCache;
        private readonly DigitalSalesMailService _digitalSalesMailService;
        private readonly string _reviewBatchTitle = AppProcessor.Messagor.GetMessage("ReviewBatch_Title");
        private readonly string _reviewHistoryTitle = AppProcessor.Messagor.GetMessage("ReviewHistory_Title");
        private readonly string _folderImage = ConfigurationManager.AppSettings["AppImageRoot_Path"] ?? "/Contents/imgs";
        private const string ReviewDigitalSalesFilterSessionPrefix = "ReviewDigitalSalesFilter_";

        public ReviewBatchItemController()
        {
            _reviewBatchItemCache = new RM_ReviewBatchItemCache();
            _reviewBatchCache = new RM_ReviewBatchCache();
            _reviewBatchItemBiz = new RM_ReviewBatchItemBiz();
            _digitalSalesCache = new RM_DigitalSalesCache();
            _userCache = new SysUserCache();
            _userBoPhanCache = new SysUserBoPhanCache();
            _workflowCache = new RM_DigitalSalesWorkflowCache();
            _digitalSalesMailService = new DigitalSalesMailService();
        }

        public ActionResult Index(int? id)
        {
            var model = new RM_ReviewBatchItemSearchModel
            {
                DigitalSalesSearch = new RM_ReviewDigitalSalesSearchModel
                {
                    ReviewBatchID = id.GetValueOrDefault(0),
                    IsReviewed = false,
                    ReviewBatches = _reviewBatchCache.GetAll() ?? new List<RM_ReviewBatchModel>(),
                    Departments = GetAccessibleDepartments(),
                    StatusOptions = (_digitalSalesCache.GetStatusList(null) ?? new List<RM_DigitalSalesStatusModel>())
                        .Select(status => new SelectListItem
                        {
                            Value = status.StatusID.ToString(),
                            Text = string.Format("[{0}] {1}",
                                status.BusinessType == 1
                                    ? AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Opportunity")
                                    : AppProcessor.Messagor.GetMessage("DigitalSales_BusinessType_Project"),
                                status.StatusName)
                        }).ToList()
                }
            };

            return View(model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetDigitalSales(RM_ReviewDigitalSalesSearchModel model)
        {
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0] ?? "0");
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0] ?? "10");
            var search = Request.Form.GetValues("search[value]")?[0];

            model.UserName = User.UserName;
            SaveReviewDigitalSalesFilter(model);
            var dataSearch = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };

            var data = _reviewBatchItemCache.LoadDigitalSales(out var total, model, dataSearch);
            return Json(new
            {
                draw = Convert.ToInt32(draw),
                recordsTotal = total,
                recordsFiltered = total,
                data
            }, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProcessesByStatus(int statusId)
        {
            if (statusId <= 0)
            {
                return Json(new List<SelectListItem>(), JsonRequestBehavior.AllowGet);
            }

            var processes = _workflowCache.GetProcesses(out _, search: null, businessType: null, statusId: statusId);
            var result = (processes ?? new List<RM_DigitalSalesProcessModel>())
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new SelectListItem
                {
                    Value = p.ProcessID.ToString(),
                    Text = p.ProcessName
                }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProgressesByProcess(int processId)
        {
            if (processId <= 0)
            {
                return Json(new List<SelectListItem>(), JsonRequestBehavior.AllowGet);
            }

            var progresses = _workflowCache.GetProgressesByProcess(processId);
            var result = (progresses ?? new List<RM_DigitalSalesProgressModel>())
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new SelectListItem
                {
                    Value = p.ProgressID.ToString(),
                    Text = p.ProgressName
                }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Lấy danh sách dự án cần rà soát.
        /// </summary>
        /// <param name="model">search model.</param>
        /// <returns>Danh sách dự án.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetProject(RM_ReviewProjectSearchModel model)
        {
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);
            var search = Request.Form.GetValues("search[value]")?[0];

            model.UserName = User.UserName;

            var dataSearch = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };

            var data = _reviewBatchItemCache.LoadProject(out var total, model, dataSearch);

            return Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Lấy danh sách cơ hội cần rà soát.
        /// </summary>
        /// <param name="model">search model.</param>
        /// <returns>Danh sách cơ hội.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult GetBusinessOpportunity(RM_ReviewBusinessOpportunitySearchModel model)
        {
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);
            var search = Request.Form.GetValues("search[value]")?[0];

            model.UserName = User.UserName;

            var dataSearch = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };

            var data = _reviewBatchItemCache.LoadBusinessOpportunity(out var total, model, dataSearch);

            return Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị màn hình rà soát dự án.
        /// </summary>
        /// <returns>Popup rà soát dự án.</returns>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult ReviewBatch(int? reviewBatchID, int? digitalSalesID, string continueReviewFilter = null)
        {
            if (!reviewBatchID.HasValue || reviewBatchID.Value <= 0 || !digitalSalesID.HasValue || digitalSalesID.Value <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = GetAppMessage("ReviewDigitalSales_InvalidData_Message")
                }, JsonRequestBehavior.AllowGet);
            }

            var model = new RM_ReviewFormModel
            {
                ReviewBatchID = reviewBatchID.Value,
                DigitalSalesID = digitalSalesID.Value,
                IsConfirmed = true,
                ContinueReviewFilter = continueReviewFilter,
                ReviewConclusion = RM_ReviewConclusion.Accepted,
                ReviewConclusionOptions = BuildReviewConclusionOptions()
            };
            return PartialView("_ReviewBatch", model);
        }

        /// <summary>
        /// Lưu thông tin rà soát dự án từ màn hình.
        /// </summary>
        /// <param name="model">Dữ liệu dự án cần lưu.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ReviewBatch(RM_ReviewFormModel model, bool continueReview = false)
        {
            ValidateReviewConclusion(model);
            if (!ModelState.IsValid)
            {
                model.ReviewConclusionOptions = BuildReviewConclusionOptions();
                return PartialView("_ReviewForm", model);
            }
            var result = _reviewBatchItemCache.Save(model, User.UserName);
            if (result > 0)
            {
                SaveFiles(model.DinhKemFile, result);
                QueueReviewCompletedNotification(model);
            }
            string response;
            if (result == 0) response = CreateMessage($"{_reviewBatchTitle} [{model.ReviewBatchID}]", EnumProcessType.Add, EnumMsgIcon.Error);
            else if (result == -9) response = CreateMessage($"{_reviewBatchTitle} [{model.ReviewBatchID}]", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else response = CreateMessage($"{_reviewBatchTitle} [{model.ReviewBatchID}]", EnumProcessType.Add, EnumMsgIcon.Success);
            var nextReviewUrl = result > 0 && continueReview ? GetNextReviewUrl(model) : null;
            return Json(new
            {
                status = result > 0,
                message = response,
                reviewBatchID = model.ReviewBatchID,
                nextReviewUrl
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Lấy DigitalSales chưa rà soát kế tiếp trong cùng đợt.
        /// </summary>
        private string GetNextReviewUrl(RM_ReviewFormModel model)
        {
            var search = new BaseSearchModel
            {
                Search = null,
                Order = "1",
                OrderDir = "DESC",
                StartIndex = 0,
                PageSize = 1
            };

            // Use the submitted filter first. The server-side copy is the reliable
            // fallback when Detail/modal navigation loses the browser-side value.
            var serializedFilter = model.ContinueReviewFilter;
            if (string.IsNullOrWhiteSpace(serializedFilter))
                serializedFilter = GetReviewDigitalSalesFilter(model.ReviewBatchID);
            if (string.IsNullOrWhiteSpace(serializedFilter)) return null;

            RM_ReviewDigitalSalesSearchModel digitalSalesSearch;
            try
            {
                digitalSalesSearch = JsonConvert.DeserializeObject<RM_ReviewDigitalSalesSearchModel>(serializedFilter);
            }
            catch
            {
                serializedFilter = GetReviewDigitalSalesFilter(model.ReviewBatchID);
                try
                {
                    digitalSalesSearch = string.IsNullOrWhiteSpace(serializedFilter)
                        ? null
                        : JsonConvert.DeserializeObject<RM_ReviewDigitalSalesSearchModel>(serializedFilter);
                }
                catch
                {
                    return null;
                }
            }
            if (digitalSalesSearch == null) return null;

            digitalSalesSearch.ReviewBatchID = model.ReviewBatchID;
            digitalSalesSearch.IsReviewed = false;
            digitalSalesSearch.UserName = User.UserName;
            var digitalSales = _reviewBatchItemCache.LoadDigitalSales(out _, digitalSalesSearch, search);
            var nextDigitalSales = digitalSales?.FirstOrDefault();
            return nextDigitalSales == null
                ? null
                : Url.Action("Detail", "DigitalSales", new
                {
                    area = "Cate",
                    id = nextDigitalSales.DigitalSalesID,
                    reviewBatchID = model.ReviewBatchID,
                    reviewFilter = serializedFilter
                });
        }

        private string GetReviewDigitalSalesFilterSessionKey(int reviewBatchID)
        {
            return string.Concat(ReviewDigitalSalesFilterSessionPrefix, User.UserName, "_", reviewBatchID);
        }

        private void SaveReviewDigitalSalesFilter(RM_ReviewDigitalSalesSearchModel model)
        {
            if (model == null || model.ReviewBatchID <= 0 || Session == null) return;
            Session[GetReviewDigitalSalesFilterSessionKey(model.ReviewBatchID)] = JsonConvert.SerializeObject(model);
        }

        private string GetReviewDigitalSalesFilter(int reviewBatchID)
        {
            if (reviewBatchID <= 0 || Session == null) return null;
            return Session[GetReviewDigitalSalesFilterSessionKey(reviewBatchID)] as string;
        }

        private void QueueReviewCompletedNotification(RM_ReviewFormModel model)
        {
            try
            {
                var sales = _digitalSalesCache.GetByID(model.DigitalSalesID);
                if (sales == null) return;

                var amUserName = sales.AssignedEmployeeID.HasValue
                    ? _userCache.GetById(sales.AssignedEmployeeID.Value)?.UserName
                    : null;
                var memberUserNames = (_digitalSalesCache.GetMembersBySalesID(model.DigitalSalesID) ?? new List<RM_DigitalSalesMemberModel>())
                    .Select(member => member.UserName)
                    .Where(userName => !string.IsNullOrWhiteSpace(userName))
                    .ToList();
                var batchName = _reviewBatchCache.GetById(model.ReviewBatchID)?.BatchName;

                _digitalSalesMailService.QueueReviewCompleted(model.DigitalSalesID, memberUserNames, amUserName,
                    batchName, model.ReviewComment, model.IsConfirmed, model.ReviewConclusion, User.UserName);
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
            }
        }

        /// <summary>
        /// Hiển thị màn hình cập nhật lịch sử rà soát.
        /// </summary>
        /// <param name="id">Mã lịch sử rà soát.</param>
        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult EditHistory(int id)
        {
            var history = _reviewBatchItemCache.GetHistoryById(id);
            if (history == null)
                return Json(new { status = false, message = CreateMessage($"{_reviewHistoryTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error) }, JsonRequestBehavior.AllowGet);

            var model = new RM_ReviewFormModel
            {
                ReviewHistoryID = history.ReviewHistoryID,
                ReviewBatchItemID = history.ReviewBatchItemID,
                ReviewComment = history.ReviewComment,
                IsConfirmed = history.IsConfirmed,
                ReviewConclusion = history.ReviewConclusion,
                ReviewConclusionOptions = BuildReviewConclusionOptions()
            };
            model.ExistingFiles = _reviewBatchItemBiz.GetFilePaths(id);
            return PartialView("_EditHistory", model);
        }

        /// <summary>
        /// Cập nhật thông tin lịch sử rà soát.
        /// </summary>
        /// <param name="model">Dữ liệu lịch sử rà soát cần cập nhật.</param>
        /// <returns>Kết quả xử lý.</returns>
        [AjaxOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        [ValidateInput(false)]
        public ActionResult EditHistory(RM_ReviewFormModel model)
        {
            ValidateReviewConclusion(model);
            if (!ModelState.IsValid)
            {
                model.ExistingFiles = _reviewBatchItemBiz.GetFilePaths(model.ReviewHistoryID);
                model.ReviewConclusionOptions = BuildReviewConclusionOptions();
                return PartialView("_ReviewForm", model);
            }

            var result = _reviewBatchItemCache.SaveHistory(model, User.UserName);
            if (result > 0)
            {
                SaveFiles(model.DinhKemFile, model.ReviewHistoryID);
                if (model.DeletedFileIds != null && model.DeletedFileIds.Any())
                {
                    foreach (var fileId in model.DeletedFileIds)
                    {
                        var file = _reviewBatchItemBiz.GetFilePathById(fileId);

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
                            _reviewBatchItemBiz.DeleteFilePath(fileId, User.UserName);
                        }
                    }
                }
            }

            string response;
            if (result == 0) response = CreateMessage($"{_reviewHistoryTitle}", EnumProcessType.Edit, EnumMsgIcon.Error);
            else if (result == -9) response = CreateMessage($"{_reviewHistoryTitle}", EnumProcessType.DataExisted, EnumMsgIcon.Error);
            else response = CreateMessage($"{_reviewHistoryTitle}", EnumProcessType.Edit, EnumMsgIcon.Success);

            return Json(new { status = true, message = response }, JsonRequestBehavior.AllowGet);
        }

        private void LuuFile(HttpPostedFileBase file, string filePath)
        {
            if (file != null && !string.IsNullOrEmpty(filePath))
                file.SaveAs(HostingEnvironment.MapPath(filePath));
        }

        private void SaveFiles(List<HttpPostedFileBase> files, int reviewHistoryID)
        {
            if (files == null || files.Count == 0) return;
            foreach (var file in files)
            {
                if (file == null || file.ContentLength == 0) continue;
                var fileName = UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName))
                    + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss")
                    + Path.GetExtension(file.FileName);
                var filePath = _folderImage + "/" + fileName;
                LuuFile(file, filePath);
                _reviewBatchItemBiz.SaveFilePath(new RM_ReviewBatchFilePathModel
                {
                    FilePathID = 0,
                    ReviewHistoryID = reviewHistoryID,
                    FilePath = filePath
                }, User.UserName);
            }
        }

        [HttpGet]
        public ActionResult GetReviewHistory(int objectType, int id)
        {
            var data = _BuildReviewHistoryForm(objectType, id);
            return PartialView("_ReviewHistory", data);
        }

        private List<RM_ReviewHistoryModel> BuildDigitalSalesReviewHistory(int digitalSalesID)
        {
            return BuildReviewHistory(_reviewBatchItemCache.GetDigitalSalesHistory(digitalSalesID));
        }

        /// <summary>
        /// Xây dựng model lịch sử rà soát kèm file đính kèm cho dự án.
        /// </summary>
        /// <returns>Model lịch sử rà soát phục vụ tab overview.</returns>
        private List<RM_ReviewHistoryModel> _BuildReviewHistoryForm(int objectType, int objectID)
        {
            return BuildReviewHistory(_reviewBatchItemCache.GetHistory(objectType, objectID));
        }

        private List<RM_ReviewHistoryModel> BuildReviewHistory(List<RM_ReviewHistoryModel> histories)
        {

            var result = new List<RM_ReviewHistoryModel>();

            if (histories != null && histories.Count > 0)
            {
                int index = 1;

                foreach (var item in histories)
                {
                    // File đính kèm của từng lịch sử rà soát
                    var files = _reviewBatchItemBiz.GetFilePaths(item.ReviewHistoryID);

                    result.Add(new RM_ReviewHistoryModel
                    {
                        ReviewBatchID = item.ReviewBatchID,
                        ReviewBatchItemID = item.ReviewBatchItemID,
                        BatchCode = item.BatchCode,
                        BatchName = item.BatchName,
                        ReviewLevel = item.ReviewLevel,
                        ReviewComment = item.ReviewComment,
                        IsConfirmed = item.IsConfirmed,
                        ReviewConclusion = item.ReviewConclusion,
                        ExistingFiles = files ?? new List<RM_ReviewBatchFilePathModel>(),
                        ReviewHistoryID = item.ReviewHistoryID,
                        Reviewer = item.Reviewer,
                        CanEdit = item.CreatedBy == User.UserName,
                        CreatedDate = item.CreatedDate
                    });

                    index++;
                }
            }
            return result;
        }

        private void ValidateReviewConclusion(RM_ReviewFormModel model)
        {
            if (model.IsConfirmed && !RM_ReviewConclusion.IsValid(model.ReviewConclusion))
            {
                ModelState.AddModelError(nameof(model.ReviewConclusion), GetAppMessage("ReviewConclusion_Required"));
            }
            else if (model.ReviewConclusion.HasValue && !RM_ReviewConclusion.IsValid(model.ReviewConclusion))
            {
                ModelState.AddModelError(nameof(model.ReviewConclusion), GetAppMessage("ReviewConclusion_Invalid"));
            }
        }

        private List<SelectListItem> BuildReviewConclusionOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = RM_ReviewConclusion.Accepted.ToString(), Text = GetAppMessage("ReviewConclusion_Accepted") },
                new SelectListItem { Value = RM_ReviewConclusion.Interested.ToString(), Text = GetAppMessage("ReviewConclusion_Interested") },
                new SelectListItem { Value = RM_ReviewConclusion.Rejected.ToString(), Text = GetAppMessage("ReviewConclusion_Rejected") }
            };
        }

        private string GetAppMessage(string labelKey)
        {
            var message = AppProcessor.Messagor.GetMessage(labelKey);
            return string.IsNullOrWhiteSpace(message) ? labelKey : message;
        }

        /// <summary>
        /// Lấy danh sách phòng ban mà user hiện tại được gán quyền xem qua Sys_UserBoPhan_GetByEmail.
        /// </summary>
        private List<MN_BoPhanModel> GetAccessibleDepartments()
        {
            var currentUser = _userCache.GetByUserName(User.UserName);
            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.Email))
            {
                return new List<MN_BoPhanModel>();
            }

            return (_userBoPhanCache.GetByEmail(currentUser.Email) ?? new List<MN_BoPhanModel>())
                .GroupBy(x => x.BoPhan_ID)
                .Select(x => x.First())
                .OrderBy(x => x.TenBoPhanView)
                .ToList();
        }
    }
}
