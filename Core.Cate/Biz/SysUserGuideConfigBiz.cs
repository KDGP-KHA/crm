using System;
using System.Collections.Generic;
using System.Linq;
using Core.Cate.Models;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class SysUserGuideConfigBiz
    {
        private const string ProviderName = "CenIT.Provider.Major";

        private const string SpGetByScreenCode = "Sys_UserGuideConfig_GetByScreenCode";
        private const string SpGetAll = "Sys_UserGuideConfig_GetAll";
        private const string SpSaveStep = "Sys_UserGuideConfig_SaveStep";
        private const string SpDeleteStep = "Sys_UserGuideConfig_DeleteStep";
        private const string SpDeleteByScreenCode = "Sys_UserGuideConfig_DeleteByScreenCode";

        public List<SysUserGuideConfigModel> GetStepsByScreenCode(string screenCode)
        {
            if (string.IsNullOrWhiteSpace(screenCode)) return new List<SysUserGuideConfigModel>();
            try
            {
                var list = AppProcessor.ProcedureProvider.ExecuteTypedList<SysUserGuideConfigModel>(
                    SpGetByScreenCode, ProviderName, screenCode.Trim());
                if (list != null && list.Count > 0)
                {
                    return list;
                }
                return GetDefaultSteps(screenCode.Trim());
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return GetDefaultSteps(screenCode.Trim());
            }
        }

        public List<SysUserGuideConfigModel> GetAllSteps(string screenPrefix = null)
        {
            try
            {
                var list = AppProcessor.ProcedureProvider.ExecuteTypedList<SysUserGuideConfigModel>(
                    SpGetAll, ProviderName, string.IsNullOrWhiteSpace(screenPrefix) ? null : screenPrefix.Trim());
                if (list != null && list.Count > 0)
                {
                    return list;
                }
                return GetAllDefaultSteps();
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return GetAllDefaultSteps();
            }
        }

        public bool SaveSteps(string screenCode, List<SysUserGuideConfigModel> steps, string userName)
        {
            if (string.IsNullOrWhiteSpace(screenCode)) return false;
            try
            {
                AppProcessor.ProcedureProvider.Execute(SpDeleteByScreenCode, ProviderName, screenCode.Trim());
                if (steps != null && steps.Count > 0)
                {
                    int order = 1;
                    foreach (var s in steps)
                    {
                        if (string.IsNullOrWhiteSpace(s.Title) && string.IsNullOrWhiteSpace(s.GuideText))
                            continue;

                        int id = 0;
                        AppProcessor.ProcedureProvider.Execute(
                            SpSaveStep, ProviderName,
                            id,
                            screenCode.Trim(),
                            s.StepOrder > 0 ? s.StepOrder : order++,
                            s.Selector ?? string.Empty,
                            s.Title ?? string.Empty,
                            s.GuideText ?? string.Empty,
                            s.IsActive,
                            userName ?? "system");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return false;
            }
        }

        public bool ResetDefaultSteps(string screenCode, string userName)
        {
            var defaultSteps = GetDefaultSteps(screenCode);
            if (defaultSteps == null || defaultSteps.Count == 0) return false;
            return SaveSteps(screenCode, defaultSteps, userName);
        }

        public List<SysUserGuideConfigModel> GetAllDefaultSteps()
        {
            var screens = new[]
            {
                "Cate.DigitalSales.Detail",
                "Cate.DigitalSales.Detail.Overview",
                "Cate.DigitalSales.Detail.Products",
                "Cate.DigitalSales.Detail.Tracking",
                "Cate.DigitalSales.Detail.Discussions",
                "Cate.DigitalSales.Detail.ReviewHistory"
            };
            var result = new List<SysUserGuideConfigModel>();
            foreach (var s in screens)
            {
                result.AddRange(GetDefaultSteps(s));
            }
            return result;
        }

        public List<SysUserGuideConfigModel> GetDefaultSteps(string screenCode)
        {
            var result = new List<SysUserGuideConfigModel>();
            switch (screenCode)
            {
                case "Cate.DigitalSales.Detail":
                    result.Add(new SysUserGuideConfigModel { StepOrder = 1, Selector = ".ds-header-title-container", Title = "Thông tin hồ sơ", GuideText = "Hiển thị mã hồ sơ, tên khách hàng, mã số thuế, loại dịch vụ và nhân viên phụ trách chính của hồ sơ chuyển đổi số.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 2, Selector = ".ds-header-action-bar", Title = "Thao tác hồ sơ", GuideText = "Các nút chức năng: Chuyển trạng thái quy trình, Chỉnh sửa thông tin hồ sơ, Làm mới dữ liệu và Mở hướng dẫn sử dụng.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 3, Selector = "#containerMetrics", Title = "Chỉ số tổng quan", GuideText = "Thẻ chỉ số tổng quan gồm: Tổng giá trị hợp đồng, Số sản phẩm dịch vụ, Tiến độ thực hiện nhiệm vụ và Số lượng trao đổi/hoạt động.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 4, Selector = ".nav-tabs", Title = "Các khu vực thông tin", GuideText = "Thanh điều hướng chuyển đổi giữa 5 tab: Thông tin tổng quan, Sản phẩm & Doanh thu, Tiến trình & Checklist, Trao đổi và Lịch sử rà soát.", IsActive = true });
                    break;
                case "Cate.DigitalSales.Detail.Overview":
                    result.Add(new SysUserGuideConfigModel { StepOrder = 1, Selector = "#tab-overview .col-md-6:first-child .card", Title = "Thông tin hồ sơ", GuideText = "Chi tiết các thông tin pháp lý của khách hàng, người liên hệ, cơ hội kinh doanh và nguồn gốc hồ sơ.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 2, Selector = "#tab-overview .col-md-6:nth-child(2) .card", Title = "Phân công và quản lý", GuideText = "Thông tin nhân sự phụ trách AM, cán bộ hỗ trợ giải pháp, quy trình và tiến trình hiện tại của hồ sơ.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 3, Selector = "#sectionMembers", Title = "Thành viên tham gia", GuideText = "Danh sách các cán bộ, chuyên viên thuộc đội ngũ phụ trách hồ sơ, hỗ trợ thêm/xóa thành viên tham gia.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 4, Selector = "#tab-overview .ds-html-note-view", Title = "Mô tả nhu cầu", GuideText = "Nội dung chi tiết về nhu cầu chuyển đổi số của khách hàng, phạm vi yêu cầu và ghi chú quan trọng.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 5, Selector = "#sectionAttachments", Title = "Tệp đính kèm", GuideText = "Khu vực lưu trữ hồ sơ, tài liệu đề xuất, hợp đồng scan và biên bản liên quan đến chuyển đổi số.", IsActive = true });
                    break;
                case "Cate.DigitalSales.Detail.Products":
                    result.Add(new SysUserGuideConfigModel { StepOrder = 1, Selector = "#tab-products h6", Title = "Danh sách sản phẩm", GuideText = "Thống kê tổng số lượng sản phẩm/dịch vụ số và tổng giá trị hợp đồng của hồ sơ.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 2, Selector = "#tab-products .btn-purple", Title = "Thêm sản phẩm", GuideText = "Nhấn nút này để thêm mới sản phẩm/dịch vụ số, cấu hình số lượng, đơn giá và giá trị hợp đồng.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 3, Selector = "#tab-products .card.bcard", Title = "Thông tin sản phẩm và hợp đồng", GuideText = "Danh sách các dịch vụ số đang tư vấn, trạng thái hợp đồng, ngày bắt đầu và kết thúc sử dụng.", IsActive = true });
                    break;
                case "Cate.DigitalSales.Detail.Tracking":
                    result.Add(new SysUserGuideConfigModel { StepOrder = 1, Selector = "#tab-tracking > .d-flex", Title = "Tổng quan checklist", GuideText = "Thanh tiến độ tổng thể phản ánh tỷ lệ hoàn thành các nhiệm vụ và tiến trình theo quy trình chuẩn.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 2, Selector = "#tblTracking", Title = "Danh sách tiến trình", GuideText = "Bảng phân rã từng bước triển khai, người thực hiện, thời hạn hoàn thành và kết quả thực hiện.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 3, Selector = "#treeTrackingBody .tree-status-row", Title = "Trạng thái và quy trình", GuideText = "Cây phân cấp các giai đoạn quy trình bán hàng giải pháp số và các tiến trình tương ứng.", IsActive = true });
                    break;
                case "Cate.DigitalSales.Detail.Discussions":
                    result.Add(new SysUserGuideConfigModel { StepOrder = 1, Selector = "#tab-discussions .ds-composer-card", Title = "Tạo trao đổi", GuideText = "Khung soạn thảo phản hồi, trao đổi nhanh giữa các thành viên phụ trách hồ sơ chuyển đổi số.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 2, Selector = "#frmPostDiscussion", Title = "Nội dung và tệp đính kèm", GuideText = "Nhập nội dung thảo luận, hỗ trợ định dạng văn bản và đính kèm tài liệu trao đổi.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 3, Selector = "#tab-discussions .ds-activity-timeline", Title = "Lịch sử hoạt động", GuideText = "Dòng thời gian ghi nhận toàn bộ lịch sử cập nhật, trao đổi, thay đổi trạng thái của hồ sơ.", IsActive = true });
                    break;
                case "Cate.DigitalSales.Detail.ReviewHistory":
                    result.Add(new SysUserGuideConfigModel { StepOrder = 1, Selector = "#tab-review-history .review-history-timeline", Title = "Lịch sử rà soát", GuideText = "Trục thời gian ghi nhận các đợt rà soát định kỳ đánh giá tính khả thi và tiến độ hồ sơ.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 2, Selector = "#tab-review-history .review-history-batch", Title = "Đợt rà soát", GuideText = "Thông tin đợt rà soát, người thực hiện rà soát và thời điểm thực hiện.", IsActive = true });
                    result.Add(new SysUserGuideConfigModel { StepOrder = 3, Selector = "#tab-review-history .review-history-entry", Title = "Chi tiết kết quả rà soát", GuideText = "Kết quả đánh giá hồ sơ trong đợt rà soát: đạt, cần khắc phục hoặc các ghi chú cảnh báo rủi ro.", IsActive = true });
                    break;
            }
            foreach (var item in result)
            {
                item.ScreenCode = screenCode;
            }
            return result;
        }
    }
}

