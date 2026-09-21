using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using Core.Cate.Caches;
using Core.Cate.Models;
using Newtonsoft.Json;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Utils;

namespace Core.Cate.Services
{
    /// <summary>Gửi email và thông báo cho các sự kiện của hồ sơ KD sản phẩm/dịch vụ số.</summary>
    public class DigitalSalesMailService
    {
        private const string ProviderName = "CenIT.Provider.Sys";
        private const string UserProcedure = "Sys_User_GetByUserName";
        private readonly RM_DigitalSalesCache _salesCache = new RM_DigitalSalesCache();
        private readonly MailTemplateService _mailTemplateService = new MailTemplateService();
        private readonly NotificationService _notificationService = new NotificationService();

        public void QueueCreated(int salesId, string amUserName, IEnumerable<string> managerUserNames, string actionUserName)
        {
            Queue(delegate { Send(salesId, new[] { amUserName }, new[] { amUserName }, managerUserNames, actionUserName, "DIGITAL_SALES_CREATED", "", "Hồ sơ KD sản phẩm/dịch vụ số mới", "Hồ sơ KD sản phẩm/dịch vụ số mới được khởi tạo", "MailTemplate_DigitalSalesCreated", "fa-plus", "text-success"); });
        }

        public void QueueMemberAdded(int salesId, IEnumerable<string> notificationUserNames, string amUserName, IEnumerable<string> ccUserNames, string roleNames, string actionUserName)
        {
            Queue(delegate { Send(salesId, notificationUserNames, notificationUserNames, null, actionUserName, "DIGITAL_SALES_MEMBER_ADDED", roleNames, "Bạn được thêm vào Hồ sơ KD sản phẩm/dịch vụ số", "Bạn được thêm vào hồ sơ", "MailTemplate_DigitalSalesMemberAdded", "fa-user-plus", "text-success"); });
        }

        public void QueueMemberRemoved(int salesId, string notificationUserName, string amUserName, IEnumerable<string> ccUserNames, string roleNames, string actionUserName)
        {
            Queue(delegate { Send(salesId, new[] { notificationUserName }, new[] { notificationUserName }, null, actionUserName, "DIGITAL_SALES_MEMBER_REMOVED", roleNames, "Bạn bị xóa khỏi Hồ sơ KD sản phẩm/dịch vụ số", "Bạn bị xóa khỏi hồ sơ", "MailTemplate_DigitalSalesMemberRemoved", "fa-user-minus", "text-danger"); });
        }

        public void QueueStatusChanged(int salesId, IEnumerable<string> memberUserNames, string amUserName, IEnumerable<string> ccUserNames, string actionUserName)
        {
            Queue(delegate { Send(salesId, memberUserNames, new[] { amUserName }, ccUserNames, actionUserName, "DIGITAL_SALES_STATUS_CHANGED", "", "Cập nhật trạng thái Hồ sơ KD sản phẩm/dịch vụ số", "Hồ sơ được chuyển trạng thái", "MailTemplate_DigitalSalesStatusChanged", "fa-exchange", "text-primary"); });
        }

        public void QueueTrackingUpdated(int salesId, IEnumerable<string> notificationUserNames, string amUserName, IEnumerable<string> ccUserNames, string taskName, string statusName, string resultHtml, string actionUserName)
        {
            var label = "Cập nhật tiến trình: " + taskName + " (" + statusName + ")";
            Queue(delegate { Send(salesId, notificationUserNames, new[] { amUserName }, ccUserNames, actionUserName, "DIGITAL_SALES_TRACKING_UPDATED", "", "Cập nhật tiến trình Hồ sơ KD sản phẩm/dịch vụ số", label, "MailTemplate_DigitalSalesTrackingUpdated", "fa-tasks", "text-primary", resultHtml); });
        }

        /// <summary>Gửi kết quả rà soát một lần cho AM chủ trì, CC các thành viên còn lại và thông báo cho toàn bộ thành viên.</summary>
        public void QueueReviewCompleted(int salesId, IEnumerable<string> memberUserNames, string amUserName,
            string batchName, string reviewComment, bool isConfirmed, byte? conclusion, string actionUserName)
        {
            var members = Normalize(memberUserNames);
            var notificationRecipients = Normalize(members.Concat(new[] { amUserName }));
            var ccUserNames = members.Where(userName => !string.Equals(userName, amUserName, StringComparison.OrdinalIgnoreCase)).ToList();
            var conclusionName = conclusion == RM_ReviewConclusion.Accepted ? "Chấp nhận"
                : conclusion == RM_ReviewConclusion.Interested ? "Quan tâm" : "Không chấp nhận";
            var reviewSummary = "Đợt rà soát: " + (batchName ?? string.Empty)
                + "; Kết luận: " + conclusionName
                + "; " + (isConfirmed ? "Đã xác nhận" : "Chưa xác nhận");
            var extraData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "ReviewBatchName", batchName ?? string.Empty },
                { "ReviewConclusion", conclusionName },
                { "ReviewConfirmed", isConfirmed ? "Đã xác nhận" : "Chưa xác nhận" },
                { "ReviewComment", StripHtml(reviewComment) },
                { "ReviewDate", DateTime.Now.ToString("dd/MM/yyyy HH:mm") }
            };

            Queue(delegate
            {
                Send(salesId, notificationRecipients, new[] { amUserName }, ccUserNames, actionUserName,
                    "DIGITAL_SALES_REVIEW_COMPLETED", string.Empty,
                    "Kết quả rà soát Hồ sơ KD sản phẩm/dịch vụ số", "Đã thực hiện rà soát hồ sơ",
                    "MailTemplate_DigitalSalesReviewCompleted", "fa-clipboard-check", "text-success",
                    reviewComment, extraData, reviewSummary);
            });
        }

        private void Send(int salesId, IEnumerable<string> notificationUserNames, IEnumerable<string> toUserNames, IEnumerable<string> ccUserNames, string actionUserName, string notificationType, string roleNames, string notificationTitle, string actionLabel, string templateKey, string iconClass, string iconColor, string descriptionHtml = null, IDictionary<string, object> additionalData = null, string notificationDetail = null)
        {
            var sales = _salesCache.GetByID(salesId);
            var notificationRecipients = Normalize(notificationUserNames);
            var emailRecipients = Normalize(toUserNames);
            if (sales == null) return;

            var actionUser = GetUser(actionUserName);
            var actionName = actionUser == null ? actionUserName : actionUser.FullName;
            _notificationService.Push(NotificationSourceType.DigitalSales, salesId, notificationTitle + ": " + sales.Title,
                BuildContent(sales, roleNames, actionName, notificationDetail), notificationRecipients, notificationType, actionUserName, actionName, iconClass, iconColor);

            var toEmails = new HashSet<string>(emailRecipients.Select(GetUser).Where(CanSend).Select(x => x.Email.Trim()), StringComparer.OrdinalIgnoreCase);
            var ccEmails = Normalize(ccUserNames).Select(GetUser).Where(CanSend).Select(x => x.Email.Trim())
                .Where(x => !toEmails.Contains(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var pendingCc = ccEmails;
            foreach (var userName in emailRecipients)
            {
                var recipient = GetUser(userName);
                if (!CanSend(recipient)) continue;
                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "FullName", recipient.FullName ?? string.Empty }, { "ToEmail", recipient.Email ?? string.Empty },
                    { "DigitalSalesCode", sales.Code ?? string.Empty }, { "DigitalSalesName", sales.Title ?? string.Empty },
                    { "CustomerName", sales.CustomerName ?? string.Empty }, { "StatusName", sales.StatusName ?? string.Empty },
                    { "BusinessTypeName", sales.BusinessTypeName ?? string.Empty }, { "RoleNames", roleNames ?? string.Empty },
                    { "AssignedEmployeeName", sales.AssignedEmployeeName ?? string.Empty },
                    { "ExpectedRevenue", sales.TotalExpectedRevenue.HasValue ? sales.TotalExpectedRevenue.Value.ToString("N0") + " triệu VNĐ" : string.Empty },
                    { "ActualRevenue", sales.TotalActualRevenue.HasValue ? sales.TotalActualRevenue.Value.ToString("N0") + " triệu VNĐ" : string.Empty },
                    { "ExpectedDate", sales.ExpectedDate.HasValue ? sales.ExpectedDate.Value.ToString("dd/MM/yyyy") : string.Empty },
                    { "ActionByFullName", actionName ?? string.Empty }, { "ActionLabel", actionLabel },
                    { "Description", string.IsNullOrWhiteSpace(descriptionHtml) ? StripHtml(sales.Note) : descriptionHtml }, { "SentAt", DateTime.Now.ToString("dd/MM/yyyy HH:mm") }
                };
                if (additionalData != null)
                {
                    foreach (var item in additionalData)
                    {
                        data[item.Key] = item.Value ?? string.Empty;
                    }
                }
                _mailTemplateService.SendByConfigKey(templateKey, recipient.Email, JsonConvert.SerializeObject(data), actionUserName, pendingCc);
                pendingCc = null; // CC chỉ đính kèm một email, không gửi lặp theo từng người nhận chính.
            }
        }

        private static string BuildContent(RM_DigitalSalesModel sales, string roleNames, string actionName, string detail = null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(sales.Code)) parts.Add(sales.Code);
            if (!string.IsNullOrWhiteSpace(sales.CustomerName)) parts.Add("Khách hàng: " + sales.CustomerName);
            if (!string.IsNullOrWhiteSpace(sales.StatusName)) parts.Add("Trạng thái: " + sales.StatusName);
            if (!string.IsNullOrWhiteSpace(roleNames)) parts.Add("Vai trò: " + roleNames);
            if (!string.IsNullOrWhiteSpace(detail)) parts.Add(detail);
            if (!string.IsNullOrWhiteSpace(actionName)) parts.Add("Người thực hiện: " + actionName);
            return string.Join(" - ", parts);
        }

        private static List<string> Normalize(IEnumerable<string> values) => (values ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        private static bool CanSend(UserInfo user) => user != null && !string.IsNullOrWhiteSpace(user.Email) && UtilString.IsValidEmail(user.Email);
        private static string StripHtml(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(System.Web.HttpUtility.HtmlDecode(value), "<.*?>", string.Empty).Trim();
        private static void Queue(Action action) { if (action == null) return; HostingEnvironment.QueueBackgroundWorkItem(delegate { try { action(); } catch (Exception ex) { AppProcessor.Logger.Error(ex); } return Task.CompletedTask; }); }
        private static UserInfo GetUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            try { return AppProcessor.ProcedureProvider.ExecuteScalarObject<UserInfo>(UserProcedure, ProviderName, userName); }
            catch (Exception ex) { AppProcessor.Logger.Error(ex); return null; }
        }
        private class UserInfo { public string UserName { get; set; } public string FullName { get; set; } public string Email { get; set; } }
    }
}
