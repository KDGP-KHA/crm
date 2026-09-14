using Core.Cate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class RM_ContactPersonsBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Major";
        private const string CONN_NAME = "TOC.Conn.Major";
        private readonly string _RM_ContactPersons_Save = "RM_ContactPersons_Save";
        private readonly string _RM_ContactPersons_Get = "RM_ContactPersons_Get";
        private readonly string _RM_ContactPersons_Delete = "RM_ContactPersons_Delete";
        private readonly string _RM_ContactPersons_GetByID = "RM_ContactPersons_GetByID";
        private readonly string _RM_ContactPersons_GetByCustomerID = "RM_ContactPersons_GetByCustomerID";
        private readonly string _RM_ContactPersons_BulkImport = "RM_ContactPersons_BulkImport";

        public List<RM_ContactPersonsModel> LoadList(out int total, BaseSearchModel search)
        {
            var filter = search as RM_ContactPersonsSearchModel;
            search = search ?? new BaseSearchModel
            {
                Search = null,
                Order = "0",
                OrderDir = "ASC",
                StartIndex = 0,
                PageSize = -1
            };

            int pageNumber = (search.PageSize <= 0) ? 1 : (search.StartIndex / search.PageSize) + 1;
            int pageSize = (search.PageSize <= 0) ? -1 : search.PageSize;

            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ContactPersonsModel>(
                 _RM_ContactPersons_Get,
                 DATA_PROVIDER_NAME,
                 filter?.Keyword, 
                 filter?.Gender,
                 filter?.Status,
                 filter?.CustomerID,
                 pageNumber,
                 pageSize
             );
            total = 0;
            if (data != null && data.Count > 0)
                total = int.Parse(data.First()?.TotalRow.ToString() ?? "0");
            return data;
        }

        public RM_ContactPersonsModel LoadDetail(int id)
        {
            return AppProcessor.ProcedureProvider.ExecuteScalarObject<RM_ContactPersonsModel>(
                _RM_ContactPersons_GetByID,
                DATA_PROVIDER_NAME,
                id);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RM_CustomerStatus
        /// </summary>
        /// <returns>Danh sách RM_CustomerStatus</returns>
        public List<RM_ContactPersonsModel> GetAll()
        {
            int total;
            var list = LoadList(out total, null);
            return list;
        }

        public int Save(RM_ContactPersonsModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _RM_ContactPersons_Save,
                DATA_PROVIDER_NAME,
                model.ContactPerson_ID,
                model.FullName,
                model.Gender,
                model.Position,
                model.WorkUnit,
                model.Phone,
                model.Mobile,
                model.Zalo,
                model.Email,
                model.Address,
                model.Birthday,
                model.Note,
                model.Status,
                model.IsDeleted,
                username 
            );

            return result.GetValueOrDefault(0);
        }

        public int Delete(RM_ContactPersonsModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _RM_ContactPersons_Delete,
                DATA_PROVIDER_NAME,
                model.ContactPerson_ID,
                username);

            return result.GetValueOrDefault(0);
        }

        /// <summary>
        /// Lấy danh sách RM_ContactPersonsModel theo CustomerID
        /// </summary>
        /// <returns>Danh sách RM_ContactPersonsModel</returns>
        public List<RM_ContactPersonsModel> GetByCustomerID(int? ID)
        {
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ContactPersonsModel>(_RM_ContactPersons_GetByCustomerID, DATA_PROVIDER_NAME, ID);
            return list;
        }
        public RM_ContactPersonsImportResultModel BulkImport(List<RM_ContactPersonsImportRowModel> rows, string username)
        {
            var dt = BuildContactPersonDataTable(rows);
            var duplicates = new List<RM_ContactPersonsDuplicateModel>();

            var connectionString = System.Web.Configuration.WebConfigurationManager
                .ConnectionStrings[CONN_NAME]?.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception($"Không tìm thấy ConnectionString '{CONN_NAME}'");

            using (var conn = new System.Data.SqlClient.SqlConnection(connectionString))
            using (var cmd = new System.Data.SqlClient.SqlCommand(_RM_ContactPersons_BulkImport, conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;

                var tvpParam = cmd.Parameters.AddWithValue("@persons", dt);
                tvpParam.SqlDbType = System.Data.SqlDbType.Structured;
                tvpParam.TypeName = "dbo.ContactPersonTableType";

                cmd.Parameters.AddWithValue("@created_by", username);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        duplicates.Add(new RM_ContactPersonsDuplicateModel
                        {
                            FullName = reader["FullName"]?.ToString(),
                            Phone = reader["Phone"]?.ToString(),
                            Reason = reader["Reason"]?.ToString()
                        });
                    }
                }
            }

            return new RM_ContactPersonsImportResultModel
            {
                SuccessCount = rows.Count - duplicates.Count,
                FailCount = duplicates.Count,
                DuplicateRows = duplicates
            };
        }

        private System.Data.DataTable BuildContactPersonDataTable(List<RM_ContactPersonsImportRowModel> rows)
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("FullName", typeof(string));
            dt.Columns.Add("Gender", typeof(int));
            dt.Columns.Add("Birthday", typeof(DateTime));
            dt.Columns.Add("Position", typeof(string));
            dt.Columns.Add("Phone", typeof(string));
            dt.Columns.Add("Mobile", typeof(string));
            dt.Columns.Add("Address", typeof(string));
            dt.Columns.Add("Email", typeof(string));
            dt.Columns.Add("Zalo", typeof(string));
            dt.Columns.Add("Note", typeof(string));
            dt.Columns.Add("WorkUnit", typeof(string));

            foreach (var r in rows)
            {
                dt.Rows.Add(
                    r.FullName,
                    r.Gender.HasValue ? (object)r.Gender.Value : DBNull.Value,
                    r.Birthday.HasValue ? (object)r.Birthday.Value : DBNull.Value,
                    r.Position,
                    r.Phone,
                    r.Mobile,
                    r.Address,
                    r.Email,
                    r.Zalo,
                    r.Note,
                    r.WorkUnit
                );
            }
            return dt;
        }
    }
}
