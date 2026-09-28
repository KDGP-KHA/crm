using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Core.Cate.Models;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class MN_BoPhanBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Major";
        private readonly string _mn_bophan_Save = "MN_BoPhan_Save";
        private readonly string _mn_bophan_Get = "MN_BoPhan_Get";
        private readonly string _mn_bophan_Delete = "MN_BoPhan_Delete";
        private readonly string _mn_bophan_GetByID = "MN_BoPhan_GetByID";
        private readonly string _mn_bophan_Export = "MN_BoPhan_Export";
        private readonly string _mn_boPhan_Sync = "MN_BoPhan_Sync";

        /// <summary>
        /// Lấy toàn bộ danh sách theo giá trị lọc
        /// </summary>
        /// <returns>Danh sách MN_BoPhan</returns>
        public List<MN_BoPhanModel> LoadList(string search = "")
        {
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<MN_BoPhanModel>(
                _mn_bophan_Get,
                DATA_PROVIDER_NAME,
                search
                );
            return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách MN_BoPhan
        /// </summary>
        /// <returns>Danh sách MN_BoPhan</returns>
        public List<MN_BoPhanModel> GetAll()
        {
            var list = LoadList("");
            return list;
        }

        /// <summary>
        /// Lấy danh sách MN_BoPhan theo ID
        /// </summary>
        /// <returns>Danh sách MN_BoPhan</returns>
        public MN_BoPhanModel LoadDetail(int ID)
        {
            var data = AppProcessor.ProcedureProvider.ExecuteScalarObject<MN_BoPhanModel>(_mn_bophan_GetByID, DATA_PROVIDER_NAME, ID);
            return data;
        }

        /// <summary>
        /// Xóa danh sách MN_BoPhan theo ID
        /// </summary>
        /// <returns> Kết quả thực hiện</returns>
        public int Delete(int ID, string UserName)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_mn_bophan_Delete, DATA_PROVIDER_NAME, ID, UserName);
            return result.GetValueOrDefault(0);
        }

        /// <summary>
        /// Cập nhật danh sách MN_BoPhan theo dữ liệu đầu vào
        /// </summary>
        /// <returns> Kết quả thực hiện</returns>
        public int Save(MN_BoPhanModel model, string savedBy)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_mn_bophan_Save, DATA_PROVIDER_NAME
               , model.BoPhan_ID
               , model.TenBoPhan
               , model.MaBoPhan
               , model.BoPhanCha_ID ?? 0
               , savedBy);
            return result.GetValueOrDefault(0);
        }

        private object[] BuildExportParams(MN_BoPhanSearchModel model)
        {
            return new object[]
            {
                string.IsNullOrEmpty(model.Keyword)    ? (object)DBNull.Value : model.Keyword,
            };
        }

        public List<MN_BoPhanExportModel> Export(MN_BoPhanSearchModel model)
        {
            var data = BuildExportParams(model);
            return AppProcessor.ProcedureProvider.ExecuteTypedList<MN_BoPhanExportModel>(
                _mn_bophan_Export, 
                DATA_PROVIDER_NAME, 
                data);
        }

        /// <summary>
        /// Đồng bộ thông tin phòng ban
        /// </summary>
        public int Sync(DataTable data, string savedBy)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _mn_boPhan_Sync,
                DATA_PROVIDER_NAME,
                data,
                savedBy
            ).GetValueOrDefault(0);
            return result;
        }
    }
}
