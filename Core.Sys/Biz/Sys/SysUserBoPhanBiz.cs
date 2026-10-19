using System.Collections.Generic;
using Core.Cate.Models;
using Core.Sys.Models.Sys;
using TSFramework.Libs.Processors;

namespace Core.Sys.Biz.Sys
{
    public class SysUserBoPhanBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Sys";
        private readonly string _getAllProc = "Sys_UserBoPhan_GetAll";
        private readonly string _saveProc = "Sys_UserBoPhan_Save";
        private readonly string _permitReview = "Sys_User_PermitReview";
        private readonly string _getByEmailProc = "Sys_UserBoPhan_GetByEmail";

        /// <summary>
        /// Lưu danh sách bộ phận cho user theo Email (xóa cũ, insert mới)
        /// </summary>
        public int Save(string email, string maBophans, string savedBy)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _saveProc,
                DATA_PROVIDER_NAME,
                email,
                maBophans,
                savedBy);
            return result.GetValueOrDefault(1);
        }

        /// <summary>
        /// Lưu danh sách bộ phận cho user theo Email (xóa cũ, insert mới)
        /// </summary>
        public int PermitReview(UserPermitReviewModel model, string savedBy)
        {
            AppProcessor.ProcedureProvider.Execute(
                _permitReview,
                DATA_PROVIDER_NAME,
                model.UserId,
                model.ReviewDepartment,
                model.ReviewLevel,
                savedBy);
            return 1;
        }

        /// <summary>
        /// Lấy danh sách bộ phận đã gán cho user theo Email
        /// </summary>
        public List<MN_BoPhanModel> GetByEmail(string email)
        {
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<MN_BoPhanModel>(
                _getByEmailProc,
                DATA_PROVIDER_NAME,
                email);
            return data;
        }

        public List<MN_BoPhanModel> GetAll()
        {
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<MN_BoPhanModel>(
                _getAllProc,
                DATA_PROVIDER_NAME);
            return data ?? new List<MN_BoPhanModel>();
        }
    }
}
