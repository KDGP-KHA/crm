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
    public class MN_ChucVuBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Sys";
        private readonly string _mn_chucvu_Save = "MN_ChucVu_Save";
        private readonly string _mn_chucvu_Get = "MN_ChucVu_Get";
        private readonly string _mn_chucvu_Delete = "MN_ChucVu_Delete";
        private readonly string _mn_chucvu_GetByID = "MN_ChucVu_GetByID";
        private readonly string _mn_chucvu_Export = "MN_ChucVu_Export";
        private readonly string _mn_chucvu_Sync = "MN_ChucVu_Sync";

        /// <summary>
        /// Lấy toàn bộ danh sách theo giá trị lọc
        /// </summary>
        /// <returns>Danh sách MN_ChucVu</returns>
        public List<MN_ChucVuModel> LoadList(out int total, MN_ChucVuSearchModel search)
        {
            search = search ?? new MN_ChucVuSearchModel { Search = null, Order = "1", OrderDir = "ASC", PageIndex = 0, PageSize = -1 };
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<MN_ChucVuModel>(
                _mn_chucvu_Get,
                DATA_PROVIDER_NAME, 
                search.Search, 
                search.Order, 
                search.OrderDir, 
                search.PageIndex, 
                search.PageSize);
            total = 0;
            if (data != null && data.Count > 0)
                total = int.Parse(data.First()?.TotalRow.ToString() ?? "0");
            return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách MN_ChucVu
        /// </summary>
        /// <returns>Danh sách MN_ChucVu</returns>
        public List<MN_ChucVuModel> GetAll()
        {
            int total;
            var list = LoadList(out total, null);
            return list;
        }

        /// <summary>
        /// Lấy danh sách MN_ChucVu theo ID
        /// </summary>
        /// <returns>Danh sách MN_ChucVu</returns>
        public MN_ChucVuModel LoadDetail(int ID)
        {
            var data = AppProcessor.ProcedureProvider.ExecuteScalarObject<MN_ChucVuModel>(_mn_chucvu_GetByID, DATA_PROVIDER_NAME, ID);
            return data;
        }

        /// <summary>
        /// Xóa danh sách MN_ChucVu theo ID
        /// </summary>
        /// <returns> Kết quả thực hiện</returns>
        public int Delete(int ID, string UserName)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_mn_chucvu_Delete, DATA_PROVIDER_NAME, ID, UserName);
            return result.GetValueOrDefault(0);
        }

        /// <summary>
        /// Cập nhật danh sách MN_ChucVu theo dữ liệu đầu vào
        /// </summary>
        /// <returns> Kết quả thực hiện</returns>
        public int Save(MN_ChucVuModel model, string savedBy)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_mn_chucvu_Save, DATA_PROVIDER_NAME
               , model.ChucVu_ID
               , model.TenChucVu
               , model.MaChucVu
               , savedBy);
            return result.GetValueOrDefault(0);
        }
        private object[] BuildExportParams(MN_ChucVuSearchModel model)
        {
            return new object[]
            {
                string.IsNullOrEmpty(model.Keyword)    ? (object)DBNull.Value : model.Keyword,
            };
        }

        public List<MN_ChucVuExportModel> Export(MN_ChucVuSearchModel model)
        {
            var data = BuildExportParams(model);
            return AppProcessor.ProcedureProvider.ExecuteTypedList<MN_ChucVuExportModel>(
                _mn_chucvu_Export,
                DATA_PROVIDER_NAME,
                data);
        }

        public int Sync(DataTable table, string user)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _mn_chucvu_Sync,
                DATA_PROVIDER_NAME,
                table,
                user
            );

            return result.GetValueOrDefault(0);
        }
    }
}
