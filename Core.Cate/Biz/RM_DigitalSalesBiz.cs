using Core.Cate.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class RM_DigitalSalesBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Major";

        private readonly string _spGetList = "RM_DigitalSales_GetList";
        private readonly string _spGetByID = "RM_DigitalSales_GetByID";
        private readonly string _spSave = "RM_DigitalSales_Save";
        private readonly string _spProductGetBySalesID = "RM_DigitalSalesProduct_GetBySalesID";
        private readonly string _spProductSave = "RM_DigitalSalesProduct_Save";
        private readonly string _spProductDelete = "RM_DigitalSalesProduct_Delete";
        private readonly string _spChangeStatus = "RM_DigitalSales_ChangeStatus";
        private readonly string _spTrackingGetBySalesID = "RM_DigitalSalesTracking_GetBySalesID";
        private readonly string _spTrackingSave = "RM_DigitalSalesTracking_Save";
        private readonly string _spTrackingDelete = "RM_DigitalSalesTracking_Delete";
        private readonly string _spTrackingUpdateStatus = "RM_DigitalSalesTracking_UpdateStatus";
        private readonly string _spMemberGetBySalesID = "RM_DigitalSalesMember_GetBySalesID";
        private readonly string _spMemberSave = "RM_DigitalSalesMember_Save";
        private readonly string _spMemberDelete = "RM_DigitalSalesMember_Delete";
        private readonly string _spGetTimeline = "RM_DigitalSales_GetTimeline";
        private readonly string _spDelete = "RM_DigitalSales_Delete";
        private readonly string _spStatusGetAll = "RM_DigitalSalesStatus_GetAll";
        private readonly string _spToggleKeyProject = "RM_DigitalSales_ToggleKeyProject";
        private readonly string _spToggleFollow = "RM_DigitalSales_ToggleFollow";
        private readonly string _spActivityAdd = "RM_DigitalSalesActivity_Add";
        private readonly string _spActivityGetList = "RM_DigitalSalesActivity_GetList";
        private readonly string _spActivityDelete = "RM_DigitalSalesActivity_Delete";
        private readonly string _spActivityUpdateLatestStatusChange = "RM_DigitalSalesActivity_UpdateLatestStatusChangeAttachments";

        public List<RM_DigitalSalesModel> LoadList(out int total, RM_DigitalSalesSearchModel model)
        {
            total = 0;
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrWhiteSpace(model.FromDate))
            {
                if (DateTime.TryParseExact(model.FromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fd))
                    fromDate = fd;
            }

            if (!string.IsNullOrWhiteSpace(model.ToDate))
            {
                if (DateTime.TryParseExact(model.ToDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime td))
                    toDate = td;
            }

            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesModel>(
                _spGetList,
                DATA_PROVIDER_NAME,
                string.IsNullOrWhiteSpace(model.Keyword) ? null : model.Keyword.Trim(),
                model.BusinessType,
                model.StatusID,
                model.CustomerID,
                model.ProductServiceID,
                model.DepartmentID,
                model.EmployeeID,
                fromDate,
                toDate,
                model.PageNumber <= 0 ? 1 : model.PageNumber,
                model.PageSize <= 0 ? 20 : model.PageSize,
                model.UserName,
                model.IsKeyProject.HasValue && model.IsKeyProject.Value ? 1 : 0,
                model.IsFollowed.HasValue && model.IsFollowed.Value ? 1 : 0
            );

            if (data != null && data.Count > 0)
            {
                total = data.First().TotalCount.GetValueOrDefault(0);
            }

            return data ?? new List<RM_DigitalSalesModel>();
        }

        public RM_DigitalSalesModel GetByID(int id)
        {
            return GetByID(id, null);
        }

        public RM_DigitalSalesModel GetByID(int id, string userName)
        {
            if (id <= 0) return null;
            var model = AppProcessor.ProcedureProvider.ExecuteScalarObject<RM_DigitalSalesModel>(_spGetByID, DATA_PROVIDER_NAME, id, userName);
            if (model != null)
            {
                try { model.Products = GetProductsBySalesID(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.Products = new List<RM_DigitalSalesProductModel>(); }
                try { model.Members = GetMembersBySalesID(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.Members = new List<RM_DigitalSalesMemberModel>(); }
                try { model.TrackingTasks = GetTrackingTasks(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.TrackingTasks = new List<RM_DigitalSalesTrackingModel>(); }
                try { model.Timelines = GetTimeline(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.Timelines = new List<RM_DigitalSalesTimelineModel>(); }
                try { model.Activities = GetActivitiesBySalesID(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.Activities = new List<RM_DigitalSalesActivityModel>(); }
            }
            return model;
        }

        public bool ToggleKeyProject(int id, bool isKeyProject, string userName)
        {
            if (id <= 0) return false;
            try
            {
                var res = AppProcessor.ProcedureProvider.Execute(
                    _spToggleKeyProject,
                    DATA_PROVIDER_NAME,
                    id,
                    isKeyProject,
                    userName
                );
                if (res.GetValueOrDefault(0) > 0) return true;

                var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                    _spToggleKeyProject,
                    DATA_PROVIDER_NAME,
                    id,
                    isKeyProject,
                    userName
                );
                if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
            }
            catch
            {
                try
                {
                    var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                        _spToggleKeyProject,
                        DATA_PROVIDER_NAME,
                        id,
                        isKeyProject,
                        userName
                    );
                    if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
                }
                catch { }
            }
            return false;
        }

        public bool ToggleFollow(int id, bool isFollowed, string userName)
        {
            if (id <= 0 || string.IsNullOrEmpty(userName)) return false;
            try
            {
                var res = AppProcessor.ProcedureProvider.Execute(
                    _spToggleFollow,
                    DATA_PROVIDER_NAME,
                    id,
                    userName,
                    isFollowed
                );
                if (res.GetValueOrDefault(0) > 0) return true;

                var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                    _spToggleFollow,
                    DATA_PROVIDER_NAME,
                    id,
                    userName,
                    isFollowed
                );
                if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
            }
            catch
            {
                try
                {
                    var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                        _spToggleFollow,
                        DATA_PROVIDER_NAME,
                        id,
                        userName,
                        isFollowed
                    );
                    if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
                }
                catch { }
            }
            return false;
        }

        public int Save(RM_DigitalSalesModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spSave,
                DATA_PROVIDER_NAME,
                model.DigitalSalesID,
                model.Code,
                model.Title,
                model.BusinessType,
                model.StatusID,
                model.CustomerID,
                model.ContactPerson_ID,
                model.ClosingProbability,
                model.ExpectedDate,
                model.StartDate,
                model.EndDate,
                model.ContractID,
                model.ContractNo,
                model.ContractValue,
                model.ContractSignDate,
                model.AssignedEmployeeID,
                model.DepartmentID,
                model.Note,
                model.FileAttach,
                username
            );

            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesProductModel> GetProductsBySalesID(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesProductModel>();
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesProductModel>(
                _spProductGetBySalesID,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            return data ?? new List<RM_DigitalSalesProductModel>();
        }

        public int SaveProduct(RM_DigitalSalesProductModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spProductSave,
                DATA_PROVIDER_NAME,
                model.SalesProductID,
                model.DigitalSalesID,
                model.ProductServiceID,
                model.ExpectedRevenue,
                model.ActualRevenue,
                model.PackageName,
                model.Quantity <= 0 ? 1 : model.Quantity,
                model.StartDate,
                model.EndDate,
                model.Note,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int DeleteProduct(int salesProductId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spProductDelete,
                DATA_PROVIDER_NAME,
                salesProductId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int ChangeStatus(int digitalSalesId, int newStatusId, string note, string attachmentPath, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spChangeStatus,
                DATA_PROVIDER_NAME,
                digitalSalesId,
                newStatusId,
                note,
                attachmentPath,
                username
            );

            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesTrackingModel> GetTrackingTasks(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesTrackingModel>();
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesTrackingModel>(
                _spTrackingGetBySalesID,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            if (list != null && list.Count > 0)
            {
                var parents = list.Where(t => !t.ParentID.HasValue || t.ParentID.Value <= 0).ToList();
                var children = list.Where(t => t.ParentID.HasValue && t.ParentID.Value > 0).ToList();
                foreach (var p in parents)
                {
                    p.TodoList = children.Where(c => c.ParentID == p.TrackingID).OrderBy(c => c.SortOrder).ThenBy(c => c.TrackingID).ToList();
                }
            }
            return list ?? new List<RM_DigitalSalesTrackingModel>();
        }

        public int UpdateTrackingStatus(int trackingId, byte status, string resultNote, string attachmentFile, int? assignedUserId, DateTime? deadline, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spTrackingUpdateStatus,
                DATA_PROVIDER_NAME,
                trackingId,
                status,
                resultNote,
                attachmentFile,
                assignedUserId.HasValue ? (object)assignedUserId.Value : DBNull.Value,
                deadline.HasValue ? (object)deadline.Value : DBNull.Value,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int SaveTracking(RM_DigitalSalesTrackingModel model, string username)
        {
            // Nếu thêm tiến trình thực tế mới vào quy trình đang có placeholder rỗng, xóa placeholder đi
            if (model.TrackingID <= 0 && model.ProcessID.HasValue && model.ProcessID.Value > 0 && !string.IsNullOrWhiteSpace(model.TaskName) && (!model.ParentID.HasValue || model.ParentID.Value <= 0))
            {
                try
                {
                    var existingTasks = GetTrackingTasks(model.DigitalSalesID);
                    var emptyPlaceholders = existingTasks.Where(t => t.ProcessID == model.ProcessID.Value && (!t.ParentID.HasValue || t.ParentID.Value <= 0) && string.IsNullOrWhiteSpace(t.TaskName)).ToList();
                    foreach (var ep in emptyPlaceholders)
                    {
                        DeleteTracking(ep.TrackingID, username);
                    }
                }
                catch (Exception ex)
                {
                    AppProcessor.Logger.Error(ex);
                }
            }

            var result = AppProcessor.ProcedureProvider.Execute(
                _spTrackingSave,
                DATA_PROVIDER_NAME,
                model.TrackingID,
                model.DigitalSalesID,
                model.ProcessID.HasValue ? (object)model.ProcessID.Value : DBNull.Value,
                model.ProgressID.HasValue ? (object)model.ProgressID.Value : DBNull.Value,
                model.TaskName,
                model.AssignedUserID.HasValue ? (object)model.AssignedUserID.Value : DBNull.Value,
                model.StartDate,
                model.Deadline.HasValue ? (object)model.Deadline.Value : DBNull.Value,
                model.Status,
                model.ResultNote,
                model.AttachmentFile,
                model.IsCustomTask,
                model.SortOrder,
                username,
                model.ParentID.HasValue ? (object)model.ParentID.Value : DBNull.Value,
                model.DurationDays.HasValue ? (object)model.DurationDays.Value : DBNull.Value
            );
            return result.GetValueOrDefault(0);
        }

        public int ChangeProcessOfStatus(int digitalSalesId, int statusId, int newProcessId, string username)
        {
            if (digitalSalesId <= 0 || statusId <= 0 || newProcessId <= 0) return 0;

            var currentTasks = GetTrackingTasks(digitalSalesId);
            var oldTasks = currentTasks.Where(t => t.StatusID == statusId && (!t.ParentID.HasValue || t.ParentID.Value <= 0)).ToList();
            foreach (var ot in oldTasks)
            {
                DeleteTracking(ot.TrackingID, username);
            }

            var progressList = new RM_DigitalSalesWorkflowBiz().GetProgressesByProcess(newProcessId);
            if (progressList != null && progressList.Count > 0)
            {
                int sort = 1;
                foreach (var pg in progressList.OrderBy(p => p.SortOrder))
                {
                    int duration = pg.DefaultDurationDays > 0 ? pg.DefaultDurationDays : 3;
                    var task = new RM_DigitalSalesTrackingModel
                    {
                        TrackingID = 0,
                        DigitalSalesID = digitalSalesId,
                        ProcessID = newProcessId,
                        ProgressID = pg.ProgressID,
                        TaskName = pg.ProgressName,
                        DurationDays = duration,
                        DefaultDurationDays = duration,
                        StartDate = DateTime.Now,
                        Deadline = DateTime.Now.AddDays(duration),
                        Status = 1,
                        IsCustomTask = false,
                        SortOrder = sort++
                    };
                    SaveTracking(task, username);
                }
            }
            else
            {
                // Nếu quy trình được chọn chưa có tiến trình mẫu nào, lưu 1 bản ghi tiến trình rỗng
                // để giữ quy trình hiển thị trên bảng Checklist và cho phép bấm "Thêm tiến trình"
                var emptyTask = new RM_DigitalSalesTrackingModel
                {
                    TrackingID = 0,
                    DigitalSalesID = digitalSalesId,
                    ProcessID = newProcessId,
                    ProgressID = null,
                    TaskName = string.Empty,
                    DurationDays = 3,
                    DefaultDurationDays = 3,
                    StartDate = DateTime.Now,
                    Deadline = DateTime.Now.AddDays(3),
                    Status = 1,
                    IsCustomTask = true,
                    SortOrder = 1
                };
                SaveTracking(emptyTask, username);
            }

            return 1;
        }

        public int DeleteTracking(int trackingId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spTrackingDelete,
                DATA_PROVIDER_NAME,
                trackingId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesMemberModel> GetMembersBySalesID(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesMemberModel>();
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesMemberModel>(
                _spMemberGetBySalesID,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            return list ?? new List<RM_DigitalSalesMemberModel>();
        }

        public int SaveMember(RM_DigitalSalesMemberModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spMemberSave,
                DATA_PROVIDER_NAME,
                model.MemberID,
                model.DigitalSalesID,
                model.UserID,
                model.RoleTitle,
                model.IsAM,
                model.Note,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int DeleteMember(int memberId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spMemberDelete,
                DATA_PROVIDER_NAME,
                memberId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesTimelineModel> GetTimeline(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesTimelineModel>();
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesTimelineModel>(
                _spGetTimeline,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    item.Note = FixVietnameseMojibake(item.Note);
                }
            }
            return list ?? new List<RM_DigitalSalesTimelineModel>();
        }

        public int Delete(int digitalSalesId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spDelete,
                DATA_PROVIDER_NAME,
                digitalSalesId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesStatusModel> GetStatusList(byte? businessType = null)
        {
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesStatusModel>(
                _spStatusGetAll,
                DATA_PROVIDER_NAME,
                businessType.HasValue ? (object)businessType.Value : DBNull.Value
            );
            return list ?? new List<RM_DigitalSalesStatusModel>();
        }

        public string GenerateNextCode()
        {
            try
            {
                var currentYear = DateTime.Now.Year.ToString();
                int total = 0;
                var latest = LoadList(out total, new RM_DigitalSalesSearchModel());
                int maxId = 0;
                if (latest != null && latest.Count > 0)
                {
                    maxId = latest.Max(x => x.DigitalSalesID);
                }
                int nextSeq = maxId + 1;
                return $"SPDV-{currentYear}-{nextSeq:D4}";
            }
            catch
            {
                return $"SPDV-{DateTime.Now.Year}-0001";
            }
        }

        public List<RM_DigitalSalesActivityModel> GetActivitiesBySalesID(int digitalSalesId, byte? activityType = null)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesActivityModel>();
            List<RM_DigitalSalesActivityModel> list = null;
            try
            {
                list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesActivityModel>(
                    _spActivityGetList,
                    DATA_PROVIDER_NAME,
                    digitalSalesId,
                    activityType.HasValue ? (object)activityType.Value : DBNull.Value
                );
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return new List<RM_DigitalSalesActivityModel>();
            }

            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    item.Content = FixVietnameseMojibake(item.Content);
                    item.ActionByName = FixVietnameseMojibake(item.ActionByName);

                    if (!string.IsNullOrWhiteSpace(item.Attachments))
                    {
                        try
                        {
                            if (item.Attachments.TrimStart().StartsWith("["))
                            {
                                item.AttachmentList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ActivityAttachmentItem>>(item.Attachments) ?? new List<ActivityAttachmentItem>();
                            }
                            else
                            {
                                var parts = item.Attachments.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var p in parts)
                                {
                                    var trimmed = p?.Trim();
                                    if (string.IsNullOrEmpty(trimmed)) continue;
                                    var ext = System.IO.Path.GetExtension(trimmed)?.ToLowerInvariant() ?? "";
                                    item.AttachmentList.Add(new ActivityAttachmentItem
                                    {
                                        FileName = System.IO.Path.GetFileName(trimmed),
                                        FilePath = trimmed,
                                        Extension = ext,
                                        IsImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" }.Contains(ext)
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // Ignore parse error
                        }
                    }
                }
            }

            return list ?? new List<RM_DigitalSalesActivityModel>();
        }

        public int AddActivity(RM_DigitalSalesActivityModel model, string username)
        {
            if (model == null || model.DigitalSalesID <= 0 || string.IsNullOrWhiteSpace(model.Content)) return 0;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spActivityAdd,
                DATA_PROVIDER_NAME,
                model.DigitalSalesID,
                model.ActivityType,
                model.Content,
                string.IsNullOrWhiteSpace(model.Attachments) ? (object)DBNull.Value : model.Attachments,
                string.IsNullOrWhiteSpace(model.MentionedUserIDs) ? (object)DBNull.Value : model.MentionedUserIDs,
                string.IsNullOrWhiteSpace(model.MentionedNames) ? (object)DBNull.Value : model.MentionedNames,
                model.ReferenceID.HasValue ? (object)model.ReferenceID.Value : DBNull.Value,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int DeleteActivity(int activityId, string username)
        {
            if (activityId <= 0) return 0;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spActivityDelete,
                DATA_PROVIDER_NAME,
                activityId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int UpdateLatestStatusChangeActivityAttachments(int digitalSalesId, string attachmentsJson, string username)
        {
            if (digitalSalesId <= 0 || string.IsNullOrWhiteSpace(attachmentsJson)) return 0;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spActivityUpdateLatestStatusChange,
                DATA_PROVIDER_NAME,
                digitalSalesId,
                attachmentsJson,
                username
            );
            return result.GetValueOrDefault(0);
        }

        private static readonly Dictionary<char, byte> _win1252Map = new Dictionary<char, byte>()
        {
            { '\u20AC', 0x80 }, { '\u201A', 0x82 }, { '\u0192', 0x83 }, { '\u201E', 0x84 },
            { '\u2026', 0x85 }, { '\u2020', 0x86 }, { '\u2021', 0x87 }, { '\u02C6', 0x88 },
            { '\u2030', 0x89 }, { '\u0160', 0x8A }, { '\u2039', 0x8B }, { '\u0152', 0x8C },
            { '\u017D', 0x8E }, { '\u2018', 0x91 }, { '\u2019', 0x92 }, { '\u201C', 0x93 },
            { '\u201D', 0x94 }, { '\u2022', 0x95 }, { '\u2013', 0x96 }, { '\u2014', 0x97 },
            { '\u02DC', 0x98 }, { '\u2122', 0x99 }, { '\u0161', 0x9A }, { '\u203A', 0x9B },
            { '\u0153', 0x9C }, { '\u017E', 0x9E }, { '\u0178', 0x9F }
        };

        public static string FixVietnameseMojibake(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            if (input.Contains("Chuyá»") || input.Contains("Ä‘á»") || input.Contains("CÆ") || input.Contains("Dá»±") ||
                input.Contains("Tráº¡ng") || input.Contains("Ä Ã¡nh") || input.Contains("ÄÃ¡nh") || input.Contains("Bá» "))
            {
                input = input
                    .Replace("Chuyá»ƒn Ä‘á»•i thÃ nh cÃ´ng tá»« CÆ  Há»˜I sang Dá»° Ã N. Tráº¡ng thÃ¡i má»›i:", "Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới:")
                    .Replace("Chuyá»ƒn Ä'á»•i thÃ nh cÃ´ng tá»« CÆ  Há»™i sang Dá»± Ã¡N. Tráº¡ng thÃ¡i má»›i:", "Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới:")
                    .Replace("Chuyá»ƒn Ä‘á»•i thÃ nh cÃ´ng tá»« CÆ  Há»˜I sang Dá»° Ã N. Trạng thái mới:", "Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới:")
                    .Replace("Chuyá»ƒn tráº¡ng thÃ¡i sang:", "Chuyển trạng thái sang:")
                    .Replace("Chuyá»ƒn tráº¡ng thÃ¡i", "Chuyển trạng thái")
                    .Replace("Ghi chÃº:", "Ghi chú:")
                    .Replace("Ä Ã¡nh dáº¥u lÃ  Dá»± Ã¡n trá» ng Ä‘iá»ƒm", "Đánh dấu là Dự án trọng điểm")
                    .Replace("ÄÃ¡nh dáº¥u lÃ  Dá»± Ã¡n trá»ng Ä'iá»ƒm", "Đánh dấu là Dự án trọng điểm")
                    .Replace("Ä Ã¡nh dáº¥u lÃ  Dá»± Ã¡n", "Đánh dấu là Dự án")
                    .Replace("Bá»  Ä‘Ã¡nh dáº¥u Dá»± Ã¡n trá» ng Ä‘iá»ƒm", "Bỏ đánh dấu Dự án trọng điểm")
                    .Replace("Bá»  Ä‘Ã¡nh dáº¥u", "Bỏ đánh dấu")
                    .Replace("Ä Ã£ hoÃ n thÃ nh cÃ´ng viá»‡c checklist:", "Đã hoàn thành công việc checklist:")
                    .Replace("Ä Ã£ hoÃ n thÃ nh 100% cÃ¡c cÃ´ng viá»‡c trong quy trÃ¬nh:", "Đã hoàn thành 100% các công việc trong quy trình:")
                    .Replace("Cáº­p nháº­t tiáº¿n Ä‘á»™ cÃ´ng viá»‡c", "Cập nhật tiến độ công việc")
                    .Replace("Káº¿t quáº£:", "Kết quả:");
            }

            if (!input.Contains("\u00C2") && !input.Contains("\u00C3") && !input.Contains("\u00C4") &&
                !input.Contains("\u00C5") && !input.Contains("\u00C6") && !input.Contains("\u00E1\u00BA") &&
                !input.Contains("\u00E1\u00BB") && !input.Contains("Ä") && !input.Contains("á»") && !input.Contains("Ã"))
            {
                return input;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                List<byte> byteBuffer = new List<byte>();

                Action flushBytes = () =>
                {
                    if (byteBuffer.Count > 0)
                    {
                        try
                        {
                            string decoded = Encoding.UTF8.GetString(byteBuffer.ToArray());
                            sb.Append(decoded);
                        }
                        catch
                        {
                            foreach (byte bVal in byteBuffer) sb.Append((char)bVal);
                        }
                        byteBuffer.Clear();
                    }
                };

                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    byte b;
                    if (c <= 0xFF)
                    {
                        byteBuffer.Add((byte)c);
                    }
                    else if (_win1252Map.TryGetValue(c, out b))
                    {
                        byteBuffer.Add(b);
                    }
                    else
                    {
                        flushBytes();
                        sb.Append(c);
                    }
                }
                flushBytes();

                var result = sb.ToString();
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch
            {
                return input;
            }
        }
    }
}
