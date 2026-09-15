using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Cate.Biz;
using Core.Cate.Models;
using TSFramework.Libs.Processors;

namespace Core.Cate.Services
{
    /// <summary>
    /// Loại đối tượng mà thông báo trỏ tới.
    /// </summary>
    public static class NotificationSourceType
    {
        /// <summary>
        /// Hồ sơ kinh doanh sản phẩm dịch vụ số.
        /// </summary>
        public const string DigitalSales = "DigitalSales";

        /// <summary>
        /// Cơ hội kinh doanh.
        /// </summary>
        public const string Opportunity = "Opportunity";

        /// <summary>
        /// Dự án.
        /// </summary>
        public const string Project = "Project";

        /// <summary>
        /// Công việc.
        /// </summary>
        public const string Task = "Task";
    }

    /// <summary>
    /// Tạo và gửi thông báo hiển thị trên chuông thông báo của thanh header.
    /// Các nghiệp vụ gửi mail sẽ gọi service này để gửi kèm thông báo tương ứng.
    /// </summary>
    public class NotificationService
    {
        private const string OpportunityDetailUrlFormat = "/Cate/BusinessOpportunityOverview/Index/{0}";
        private const string ProjectDetailUrlFormat = "/Cate/ProjectOverview/Index/{0}";
        private const string TaskDetailUrlFormat = "/Cate/TaskManagement/Detail/{0}";
        private const string DigitalSalesDiscussionUrlFormat = "/Cate/DigitalSales/Detail/{0}#tab-discussions";
        private const int MaxContentLength = 300;

        private static readonly Regex HtmlTagRegex = new Regex("<[^>]*>", RegexOptions.Compiled);

        private readonly RM_NotificationBiz _notificationBiz;

        /// <summary>
        /// Khởi tạo service thông báo.
        /// </summary>
        public NotificationService()
        {
            _notificationBiz = new RM_NotificationBiz();
        }

        #region Public Methods

        /// <summary>
        /// Tạo thông báo gắn với phần trao đổi của hồ sơ kinh doanh sản phẩm dịch vụ số.
        /// </summary>
        /// <param name="digitalSalesId">Mã hồ sơ kinh doanh sản phẩm dịch vụ số.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="content">Nội dung tóm tắt.</param>
        /// <param name="userNames">Danh sách username người nhận.</param>
        /// <param name="notificationType">Mã loại nghiệp vụ.</param>
        /// <param name="createdBy">Username người thực hiện.</param>
        /// <param name="createdByFullName">Họ tên người thực hiện.</param>
        public void PushDigitalSalesNotification(
            int digitalSalesId,
            string title,
            string content,
            IEnumerable<string> userNames,
            string notificationType,
            string createdBy = null,
            string createdByFullName = null)
        {
            Push(NotificationSourceType.DigitalSales, digitalSalesId, title, content, userNames,
                notificationType, createdBy, createdByFullName, "fa-at", "text-primary");
        }

        /// <summary>
        /// Tạo thông báo gắn với một cơ hội kinh doanh.
        /// </summary>
        /// <param name="businessOpportunityId">Mã cơ hội kinh doanh.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="content">Nội dung tóm tắt.</param>
        /// <param name="userNames">Danh sách username người nhận.</param>
        /// <param name="notificationType">Mã loại nghiệp vụ.</param>
        /// <param name="createdBy">Username người thực hiện.</param>
        /// <param name="createdByFullName">Họ tên người thực hiện.</param>
        /// <param name="iconClass">Class icon hiển thị.</param>
        /// <param name="iconColor">Class màu icon hiển thị.</param>
        public void PushOpportunityNotification(
            int businessOpportunityId,
            string title,
            string content,
            IEnumerable<string> userNames,
            string notificationType,
            string createdBy = null,
            string createdByFullName = null,
            string iconClass = "fa-handshake",
            string iconColor = "text-primary")
        {
            Push(NotificationSourceType.Opportunity, businessOpportunityId, title, content, userNames,
                notificationType, createdBy, createdByFullName, iconClass, iconColor);
        }

        /// <summary>
        /// Tạo thông báo gắn với một dự án.
        /// </summary>
        /// <param name="projectId">Mã dự án.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="content">Nội dung tóm tắt.</param>
        /// <param name="userNames">Danh sách username người nhận.</param>
        /// <param name="notificationType">Mã loại nghiệp vụ.</param>
        /// <param name="createdBy">Username người thực hiện.</param>
        /// <param name="createdByFullName">Họ tên người thực hiện.</param>
        /// <param name="iconClass">Class icon hiển thị.</param>
        /// <param name="iconColor">Class màu icon hiển thị.</param>
        public void PushProjectNotification(
            int projectId,
            string title,
            string content,
            IEnumerable<string> userNames,
            string notificationType,
            string createdBy = null,
            string createdByFullName = null,
            string iconClass = "fa-folder-open",
            string iconColor = "text-success")
        {
            Push(NotificationSourceType.Project, projectId, title, content, userNames,
                notificationType, createdBy, createdByFullName, iconClass, iconColor);
        }

        /// <summary>
        /// Tạo thông báo gắn với một công việc; ưu tiên điều hướng về dự án hoặc cơ hội chứa công việc đó.
        /// </summary>
        /// <param name="taskId">Mã công việc.</param>
        /// <param name="projectId">Mã dự án chứa công việc, có thể null.</param>
        /// <param name="businessOpportunityId">Mã cơ hội chứa công việc, có thể null.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="content">Nội dung tóm tắt.</param>
        /// <param name="userNames">Danh sách username người nhận.</param>
        /// <param name="notificationType">Mã loại nghiệp vụ.</param>
        /// <param name="createdBy">Username người thực hiện.</param>
        /// <param name="createdByFullName">Họ tên người thực hiện.</param>
        public void PushTaskNotification(
            int taskId,
            int? projectId,
            int? businessOpportunityId,
            string title,
            string content,
            IEnumerable<string> userNames,
            string notificationType,
            string createdBy = null,
            string createdByFullName = null)
        {
            // Ưu tiên điều hướng tới dự án, sau đó tới cơ hội, cuối cùng mới tới chi tiết công việc
            if (projectId.HasValue && projectId.Value > 0)
            {
                Push(NotificationSourceType.Project, projectId.Value, title, content, userNames,
                    notificationType, createdBy, createdByFullName, "fa-tasks", "text-orange");
                return;
            }

            if (businessOpportunityId.HasValue && businessOpportunityId.Value > 0)
            {
                Push(NotificationSourceType.Opportunity, businessOpportunityId.Value, title, content, userNames,
                    notificationType, createdBy, createdByFullName, "fa-tasks", "text-orange");
                return;
            }

            Push(NotificationSourceType.Task, taskId, title, content, userNames,
                notificationType, createdBy, createdByFullName, "fa-tasks", "text-orange");
        }

        /// <summary>
        /// Tạo thông báo với loại đối tượng và mã đối tượng chỉ định.
        /// </summary>
        /// <param name="sourceType">Loại đối tượng liên kết.</param>
        /// <param name="sourceId">Mã đối tượng liên kết.</param>
        /// <param name="title">Tiêu đề thông báo.</param>
        /// <param name="content">Nội dung tóm tắt.</param>
        /// <param name="userNames">Danh sách username người nhận.</param>
        /// <param name="notificationType">Mã loại nghiệp vụ.</param>
        /// <param name="createdBy">Username người thực hiện.</param>
        /// <param name="createdByFullName">Họ tên người thực hiện.</param>
        /// <param name="iconClass">Class icon hiển thị.</param>
        /// <param name="iconColor">Class màu icon hiển thị.</param>
        public void Push(
            string sourceType,
            int sourceId,
            string title,
            string content,
            IEnumerable<string> userNames,
            string notificationType,
            string createdBy = null,
            string createdByFullName = null,
            string iconClass = "fa-bell",
            string iconColor = "text-primary")
        {
            try
            {
                var receivers = NormalizeUserNames(userNames);
                if (receivers.Count == 0 || string.IsNullOrWhiteSpace(title))
                {
                    return;
                }

                var model = new RM_NotificationSaveModel
                {
                    Title = Truncate(StripHtml(title), 500),
                    Content = Truncate(StripHtml(content), MaxContentLength),
                    NotificationType = notificationType,
                    SourceType = sourceType,
                    SourceID = sourceId > 0 ? sourceId : (int?)null,
                    DetailUrl = BuildDetailUrl(sourceType, sourceId),
                    IconClass = iconClass,
                    IconColor = iconColor,
                    CreatedBy = createdBy,
                    CreatedByFullName = createdByFullName,
                    Usernames = string.Join(",", receivers)
                };

                var notificationId = _notificationBiz.Save(model);
                if (notificationId <= 0)
                {
                    return;
                }

                PushRealtime(receivers);
            }
            catch (Exception ex)
            {
                // Thông báo là chức năng phụ trợ: lỗi ở đây không được làm hỏng nghiệp vụ chính
                AppProcessor.Logger.Error(ex);
            }
        }

        /// <summary>
        /// Tạo đường dẫn tới màn hình chi tiết theo loại đối tượng.
        /// </summary>
        /// <param name="sourceType">Loại đối tượng liên kết.</param>
        /// <param name="sourceId">Mã đối tượng liên kết.</param>
        /// <returns>Đường dẫn tương đối tới màn hình chi tiết.</returns>
        public static string BuildDetailUrl(string sourceType, int sourceId)
        {
            if (sourceId <= 0)
            {
                return null;
            }

            if (string.Equals(sourceType, NotificationSourceType.Opportunity, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(OpportunityDetailUrlFormat, sourceId);
            }

            if (string.Equals(sourceType, NotificationSourceType.Project, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(ProjectDetailUrlFormat, sourceId);
            }

            if (string.Equals(sourceType, NotificationSourceType.Task, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(TaskDetailUrlFormat, sourceId);
            }

            if (string.Equals(sourceType, NotificationSourceType.DigitalSales, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(DigitalSalesDiscussionUrlFormat, sourceId);
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Đẩy tín hiệu realtime để trình duyệt của người nhận tải lại danh sách thông báo.
        /// </summary>
        /// <param name="receivers">Danh sách username người nhận.</param>
        private void PushRealtime(IEnumerable<string> receivers)
        {
            foreach (var receiver in receivers)
            {
                try
                {
                    AppProcessor.Notifider.PushNotifyToUser("Sys", receiver, "reloadHeaderNotifications();");
                }
                catch (Exception ex)
                {
                    AppProcessor.Logger.Error(ex);
                }
            }
        }

        /// <summary>
        /// Chuẩn hóa danh sách username: bỏ rỗng, bỏ trùng và cắt khoảng trắng thừa.
        /// </summary>
        /// <param name="userNames">Danh sách username đầu vào.</param>
        /// <returns>Danh sách username đã chuẩn hóa.</returns>
        private List<string> NormalizeUserNames(IEnumerable<string> userNames)
        {
            var source = userNames ?? Enumerable.Empty<string>();

            return source
                .Where(userName => !string.IsNullOrWhiteSpace(userName))
                .Select(userName => userName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Loại bỏ thẻ HTML để nội dung thông báo hiển thị gọn trên chuông.
        /// </summary>
        /// <param name="value">Chuỗi cần xử lý.</param>
        /// <returns>Chuỗi đã loại bỏ thẻ HTML.</returns>
        private string StripHtml(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var text = HtmlTagRegex.Replace(value, " ");
            text = System.Net.WebUtility.HtmlDecode(text);

            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Cắt bớt chuỗi theo độ dài tối đa.
        /// </summary>
        /// <param name="value">Chuỗi cần cắt.</param>
        /// <param name="maxLength">Độ dài tối đa.</param>
        /// <returns>Chuỗi sau khi cắt.</returns>
        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength - 3) + "...";
        }

        #endregion
    }
}
