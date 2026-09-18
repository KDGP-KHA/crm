using System;
using System.Collections.Generic;
using Core.Sys.Models;
using TSFramework.Libs.Processors;

namespace Core.Sys.BLL
{
    public class SharedDocumentBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Sys";

        private readonly string _spCategoryGetList = "Sys_DocumentCategory_GetList";
        private readonly string _spCategoryGetById = "Sys_DocumentCategory_GetById";
        private readonly string _spCategoryInsertUpdate = "Sys_DocumentCategory_InsertUpdate";
        private readonly string _spCategoryDelete = "Sys_DocumentCategory_Delete";

        private readonly string _spDocumentGetList = "Sys_SharedDocument_GetList";
        private readonly string _spDocumentGetById = "Sys_SharedDocument_GetById";
        private readonly string _spDocumentInsert = "Sys_SharedDocument_Insert";
        private readonly string _spDocumentUpdate = "Sys_SharedDocument_Update";
        private readonly string _spDocumentDelete = "Sys_SharedDocument_Delete";
        private readonly string _spDocumentTrackDownload = "Sys_SharedDocument_TrackDownload";

        public SharedDocumentBiz()
        {
            EnsureProcedureMapping();
        }

        public static void EnsureProcedureMapping()
        {
            try
            {
                var provider = AppProcessor.ProcedureProvider?.DicStoreProceduresProvider?[DATA_PROVIDER_NAME];
                if (provider != null)
                {
                    var prop = provider.GetType().GetProperty("MappingProcedure",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    var dict = prop?.GetValue(provider) as Dictionary<string, string>;
                    if (dict != null)
                    {
                        lock (dict)
                        {
                            var sps = new[]
                            {
                                "Sys_DocumentCategory_GetList",
                                "Sys_DocumentCategory_GetById",
                                "Sys_DocumentCategory_InsertUpdate",
                                "Sys_DocumentCategory_Delete",
                                "Sys_SharedDocument_GetList",
                                "Sys_SharedDocument_GetById",
                                "Sys_SharedDocument_Insert",
                                "Sys_SharedDocument_Update",
                                "Sys_SharedDocument_Delete",
                                "Sys_SharedDocument_TrackDownload"
                            };
                            foreach (var sp in sps)
                            {
                                if (!dict.ContainsKey(sp))
                                {
                                    dict[sp] = sp;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback an toan
            }
        }

        #region Category Methods

        public List<SharedDocumentCategoryModel> GetCategories(bool? isActive = true)
        {
            EnsureProcedureMapping();
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<SharedDocumentCategoryModel>(
                _spCategoryGetList,
                DATA_PROVIDER_NAME,
                isActive
            );
            return list ?? new List<SharedDocumentCategoryModel>();
        }

        public SharedDocumentCategoryModel GetCategoryById(int categoryId)
        {
            EnsureProcedureMapping();
            return AppProcessor.ProcedureProvider.ExecuteScalarObject<SharedDocumentCategoryModel>(
                _spCategoryGetById,
                DATA_PROVIDER_NAME,
                categoryId
            );
        }

        public int SaveCategory(SharedDocumentCategoryModel model, string userName)
        {
            EnsureProcedureMapping();
            if (model == null) return 0;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spCategoryInsertUpdate,
                DATA_PROVIDER_NAME,
                model.CategoryId,
                model.CategoryName,
                model.Description,
                model.DisplayOrder,
                model.IsActive,
                userName
            );
            return result.GetValueOrDefault(0);
        }

        public int DeleteCategory(int categoryId, string userName)
        {
            EnsureProcedureMapping();
            var result = AppProcessor.ProcedureProvider.Execute(
                _spCategoryDelete,
                DATA_PROVIDER_NAME,
                categoryId,
                userName
            );
            return result.GetValueOrDefault(0);
        }

        #endregion

        #region Shared Document Methods

        public List<SharedDocumentModel> GetList(out int total, SharedDocumentSearchModel search)
        {
            search = search ?? new SharedDocumentSearchModel();
            return GetList(out total, search.Keyword, search.CategoryId, search.FromDate, search.ToDate, search.PageNumber, search.PageSize);
        }

        public List<SharedDocumentModel> GetList(out int total, string keyword, int? categoryId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize)
        {
            EnsureProcedureMapping();

            int pageNum = pageNumber < 1 ? 1 : pageNumber;
            int pSize = pageSize < 1 ? 20 : pageSize;
            object catParam = (categoryId.HasValue && categoryId.Value > 0) ? (object)categoryId.Value : null;
            string keyParam = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<SharedDocumentModel>(
                _spDocumentGetList,
                DATA_PROVIDER_NAME,
                keyParam,
                catParam,
                fromDate,
                toDate,
                pageNum,
                pSize,
                0
            );

            total = 0;
            if (list != null && list.Count > 0)
            {
                total = list[0].TotalRows ?? list[0].TotalRow ?? list.Count;
            }

            return list ?? new List<SharedDocumentModel>();
        }

        public SharedDocumentModel GetById(int documentId)
        {
            EnsureProcedureMapping();
            if (documentId <= 0) return null;

            return AppProcessor.ProcedureProvider.ExecuteScalarObject<SharedDocumentModel>(
                _spDocumentGetById,
                DATA_PROVIDER_NAME,
                documentId
            );
        }

        public int Save(SharedDocumentModel model, string userName)
        {
            EnsureProcedureMapping();
            if (model == null) return 0;

            if (model.DocumentId == 0)
            {
                var result = AppProcessor.ProcedureProvider.Execute(
                    _spDocumentInsert,
                    DATA_PROVIDER_NAME,
                    model.CategoryId,
                    model.DocumentName,
                    model.Description,
                    model.FileName,
                    model.OriginalFileName,
                    model.FilePath,
                    model.FileSize,
                    model.FileExtension,
                    userName
                );
                return result.GetValueOrDefault(0);
            }
            else
            {
                var result = AppProcessor.ProcedureProvider.Execute(
                    _spDocumentUpdate,
                    DATA_PROVIDER_NAME,
                    model.DocumentId,
                    model.CategoryId,
                    model.DocumentName,
                    model.Description,
                    model.FileName,
                    model.OriginalFileName,
                    model.FilePath,
                    model.FileSize > 0 ? (object)model.FileSize : null,
                    model.FileExtension,
                    userName
                );
                return result.GetValueOrDefault(0);
            }
        }

        public int Delete(int documentId, string userName)
        {
            EnsureProcedureMapping();
            if (documentId <= 0) return 0;

            var result = AppProcessor.ProcedureProvider.Execute(
                _spDocumentDelete,
                DATA_PROVIDER_NAME,
                documentId,
                userName
            );
            return result.GetValueOrDefault(0);
        }

        public int TrackDownload(int documentId, string downloadedBy)
        {
            EnsureProcedureMapping();
            if (documentId <= 0) return 0;

            var result = AppProcessor.ProcedureProvider.Execute(
                _spDocumentTrackDownload,
                DATA_PROVIDER_NAME,
                documentId,
                downloadedBy
            );
            return result.GetValueOrDefault(0);
        }

        #endregion
    }
}
