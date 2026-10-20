using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Core.Cate.Caches;
using Core.Cate.Models;
using Newtonsoft.Json;

namespace Core.Cate.Services
{
    /// <summary>
    /// Service đồng bộ dữ liệu user, donvi, chucvu từ API CAS HRM (/api/hrmcas) hoặc API HRM cũ.
    /// </summary>
    public class SyncDataService
    {
        private readonly MN_BoPhanCache _boPhanCache = new MN_BoPhanCache();
        private readonly MN_ChucVuCache _chucVuCache = new MN_ChucVuCache();

        #region === PUBLIC METHODS ===

        public async Task<DataTable> GetBoPhanAsync()
        {
            var casApiUrl = ConfigurationManager.AppSettings["CasHrm_ApiUrl"];
            if (!string.IsNullOrEmpty(casApiUrl))
            {
                var casData = await FetchCasDataAsync<CasDepartmentDto>("departments");
                return BuildCasBoPhanTable(casData);
            }

            var legacyUrl = ConfigurationManager.AppSettings["Key_Get_Department_API"];
            return await GetDataTableAsync<BoPhanDto>(legacyUrl, BuildBoPhanTable);
        }

        public async Task<DataTable> GetChucVuAsync()
        {
            var casApiUrl = ConfigurationManager.AppSettings["CasHrm_ApiUrl"];
            if (!string.IsNullOrEmpty(casApiUrl))
            {
                var casData = await FetchCasDataAsync<CasJobDto>("jobs");
                return BuildCasChucVuTable(casData);
            }

            var legacyUrl = ConfigurationManager.AppSettings["Key_Get_Position_API"];
            return await GetDataTableAsync<ChucVuDto>(legacyUrl, BuildChucVuTable);
        }

        public async Task<DataTable> GetNhanVienAsync()
        {
            var casApiUrl = ConfigurationManager.AppSettings["CasHrm_ApiUrl"];
            if (!string.IsNullOrEmpty(casApiUrl))
            {
                var casData = await FetchCasDataAsync<CasEmployeeDto>("employees");
                return BuildCasNhanVienTable(casData);
            }

            var legacyUrl = ConfigurationManager.AppSettings["Key_Get_User_API"];
            return await GetDataTableAsync<NhanVienDto>(legacyUrl, BuildNhanVienTable);
        }

        #endregion

        #region === CAS HRM API (/api/hrmcas) ===

        /// <summary>
        /// Tính chuỗi băm xác thực SHA-256 dạng hex chữ thường (64 ký tự) theo đúng quy tắc CAS HRM:
        /// checksum = SHA256( partnerCode | requestId | requestTime | resource | page | pageSize | keyword | secretKey )
        /// </summary>
        public static string ComputeSha256Checksum(string partnerCode, string requestId, string requestTime, string resource, int page, int pageSize, string keyword, string secretKey)
        {
            var raw = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                partnerCode ?? "",
                requestId ?? "",
                requestTime ?? "",
                resource ?? "",
                page,
                pageSize,
                keyword ?? "",
                secretKey ?? "");

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(raw);
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(64);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gọi API CAS HRM qua POST /api/hrmcas và tự động duyệt toàn bộ các trang dữ liệu (Pagination).
        /// </summary>
        private async Task<List<T>> FetchCasDataAsync<T>(string resource, string keyword = "")
        {
            var apiUrl = ConfigurationManager.AppSettings["CasHrm_ApiUrl"] ?? "http://pmncpt.cenit.vn/api/hrmcas";
            var partnerCode = ConfigurationManager.AppSettings["CasHrm_PartnerCode"] ?? "CRM";
            var secretKey = ConfigurationManager.AppSettings["CasHrm_SecretKey"] ?? "";
            var pageSizeStr = ConfigurationManager.AppSettings["CasHrm_PageSize"];
            int pageSize = 100;
            if (!string.IsNullOrEmpty(pageSizeStr) && int.TryParse(pageSizeStr, out var parsedPs))
            {
                pageSize = Math.Min(Math.Max(parsedPs, 1), 500);
            }

            var allItems = new List<T>();
            int currentPage = 1;
            int totalPages = 1;

            using (var http = new HttpClient())
            {
                do
                {
                    var requestId = "REQ-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
                    var requestTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var checksum = ComputeSha256Checksum(partnerCode, requestId, requestTime, resource, currentPage, pageSize, keyword, secretKey);

                    var requestPayload = new
                    {
                        partnerCode = partnerCode,
                        requestId = requestId,
                        requestTime = requestTime,
                        resource = resource,
                        page = currentPage,
                        pageSize = pageSize,
                        keyword = keyword ?? "",
                        checksum = checksum
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(requestPayload), Encoding.UTF8, "application/json");
                    var response = await http.PostAsync(apiUrl, jsonContent);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<CasApiResponse<T>>(responseBody);

                    if (apiResponse == null)
                    {
                        throw new Exception($"Phản hồi từ CAS API ({resource}, page {currentPage}) là null.");
                    }

                    if (apiResponse.code != "00")
                    {
                        throw new Exception($"CAS API trả về mã lỗi: {apiResponse.code} - {apiResponse.message} (requestId: {apiResponse.requestId})");
                    }

                    if (apiResponse.data != null && apiResponse.data.Count > 0)
                    {
                        allItems.AddRange(apiResponse.data);
                    }

                    totalPages = apiResponse.totalPages > 0 ? apiResponse.totalPages : 1;
                    currentPage++;
                }
                while (currentPage <= totalPages);
            }

            return allItems;
        }

        private DataTable BuildCasBoPhanTable(List<CasDepartmentDto> data)
        {
            var table = new DataTable();

            table.Columns.Add("DonVi_ID", typeof(int));
            table.Columns.Add("Ma_DV", typeof(string));
            table.Columns.Add("Ten_DV", typeof(string));
            table.Columns.Add("DonVi_Cha_ID", typeof(int));
            table.Columns.Add("Ngay_Tao", typeof(DateTime));
            table.Columns.Add("Ngay_CN", typeof(DateTime));
            table.Columns.Add("Nguoi_Tao", typeof(string));
            table.Columns.Add("Nguoi_CN", typeof(string));
            table.Columns.Add("Da_Xoa", typeof(bool));
            table.Columns.Add("Ma_DV_BCN", typeof(string));
            table.Columns.Add("CapQuanLy", typeof(int));

            if (data == null) return table;

            foreach (var item in data)
            {
                table.Rows.Add(
                    item.id,
                    item.code ?? "",
                    item.name ?? "",
                    item.parentId == 0 ? (object)DBNull.Value : item.parentId,
                    DateTime.Now,
                    DateTime.Now,
                    "job-system",
                    "job-system",
                    false,
                    item.parentCode ?? "",
                    0
                );
            }

            return table;
        }

        private DataTable BuildCasChucVuTable(List<CasJobDto> data)
        {
            var table = new DataTable();

            table.Columns.Add("ChucDanh_ID", typeof(int));
            table.Columns.Add("Ma_CD", typeof(string));
            table.Columns.Add("Ten_CD", typeof(string));
            table.Columns.Add("Lanh_Dao", typeof(bool));
            table.Columns.Add("Ngay_Tao", typeof(DateTime));
            table.Columns.Add("Ngay_CN", typeof(DateTime));

            if (data == null) return table;

            foreach (var item in data)
            {
                table.Rows.Add(
                    item.id,
                    item.code ?? "",
                    item.name ?? "",
                    false,
                    DateTime.Now,
                    DateTime.Now
                );
            }

            return table;
        }

        private DataTable BuildCasNhanVienTable(List<CasEmployeeDto> data)
        {
            var table = new DataTable();

            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("UserName", typeof(string));
            table.Columns.Add("MaNV", typeof(string));
            table.Columns.Add("FullName", typeof(string));
            table.Columns.Add("Phone", typeof(string));
            table.Columns.Add("MaChucVu", typeof(string));
            table.Columns.Add("MaBoPhan", typeof(string));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("IsDeleted", typeof(bool));
            table.Columns.Add("CreatedDate", typeof(DateTime));
            table.Columns.Add("CreatedBy", typeof(string));
            table.Columns.Add("IsCollaborator", typeof(bool));

            if (data == null) return table;

            foreach (var item in data)
            {
                var email = item.email;
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                email = email.Trim();
                var userName = email.Split('@')[0];

                // MaBoPhan: Lấy trực tiếp từ departmentCode
                var maBoPhan = item.departmentCode ?? "";

                // MaChucVu: Ưu tiên lấy theo jobCode, nếu không có lấy workPositionCode
                var maChucVu = !string.IsNullOrWhiteSpace(item.jobCode)
                    ? item.jobCode.Trim()
                    : (!string.IsNullOrWhiteSpace(item.workPositionCode) ? item.workPositionCode.Trim() : "");

                table.Rows.Add(
                    email,
                    userName,
                    item.code ?? "",
                    item.fullName ?? "",
                    item.phoneNumber ?? "",
                    maChucVu,
                    maBoPhan,
                    true,
                    false,
                    DateTime.Now,
                    "job-system",
                    item.isCollaborator
                );
            }

            return table;
        }

        #endregion

        #region === LEGACY API (Fallback) ===

        private async Task<DataTable> GetDataTableAsync<T>(
            string url,
            Func<List<T>, DataTable> builder)
        {
            using (var http = new HttpClient())
            {
                var json = await http.GetStringAsync(url);
                var obj = JsonConvert.DeserializeObject<ApiResponse<T>>(json);

                return builder(obj.Data);
            }
        }

        private DataTable BuildBoPhanTable(List<BoPhanDto> data)
        {
            var table = new DataTable();

            table.Columns.Add("DonVi_ID", typeof(int));
            table.Columns.Add("Ma_DV", typeof(string));
            table.Columns.Add("Ten_DV", typeof(string));
            table.Columns.Add("DonVi_Cha_ID", typeof(int));
            table.Columns.Add("Ngay_Tao", typeof(DateTime));
            table.Columns.Add("Ngay_CN", typeof(DateTime));
            table.Columns.Add("Nguoi_Tao", typeof(string));
            table.Columns.Add("Nguoi_CN", typeof(string));
            table.Columns.Add("Da_Xoa", typeof(bool));
            table.Columns.Add("Ma_DV_BCN", typeof(string));
            table.Columns.Add("CapQuanLy", typeof(int));

            if (data == null) return table;

            foreach (var item in data)
            {
                table.Rows.Add(
                    item.DonVi_ID,
                    item.Ma_DV,
                    item.Ten_DV,
                    item.DonVi_Cha_ID == 0 ? (object)DBNull.Value : item.DonVi_Cha_ID,
                    item.Ngay_Tao ?? (object)DBNull.Value,
                    item.Ngay_CN ?? (object)DBNull.Value,
                    item.Nguoi_Tao,
                    item.Nguoi_CN,
                    item.Da_Xoa,
                    item.Ma_DV_BCN,
                    item.CapQuanLy
                );
            }

            return table;
        }

        private DataTable BuildChucVuTable(List<ChucVuDto> data)
        {
            var table = new DataTable();

            table.Columns.Add("ChucDanh_ID", typeof(int));
            table.Columns.Add("Ma_CD", typeof(string));
            table.Columns.Add("Ten_CD", typeof(string));
            table.Columns.Add("Lanh_Dao", typeof(bool));
            table.Columns.Add("Ngay_Tao", typeof(DateTime));
            table.Columns.Add("Ngay_CN", typeof(DateTime));

            if (data == null) return table;

            foreach (var item in data)
            {
                table.Rows.Add(
                    item.ChucDanh_ID,
                    item.Ma_CD,
                    item.Ten_CD,
                    item.Lanh_Dao,
                    item.Ngay_Tao ?? (object)DBNull.Value,
                    item.Ngay_CN ?? (object)DBNull.Value
                );
            }

            return table;
        }

        private DataTable BuildNhanVienTable(List<NhanVienDto> data)
        {
            var table = new DataTable();

            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("UserName", typeof(string));
            table.Columns.Add("MaNV", typeof(string));
            table.Columns.Add("FullName", typeof(string));
            table.Columns.Add("Phone", typeof(string));
            table.Columns.Add("MaChucVu", typeof(string));
            table.Columns.Add("MaBoPhan", typeof(string));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("IsDeleted", typeof(bool));
            table.Columns.Add("CreatedDate", typeof(DateTime));
            table.Columns.Add("CreatedBy", typeof(string));
            table.Columns.Add("IsCollaborator", typeof(bool));

            if (data == null) return table;

            var boPhanMap = _boPhanCache.GetAll()
                .ToDictionary(x => x.BoPhan_ID, x => x.MaBoPhan);

            var chucVuMap = _chucVuCache.GetAll()
                .ToDictionary(x => x.ChucVu_ID, x => x.MaChucVu);

            foreach (var item in data)
            {
                var email = item.Email;
                if (string.IsNullOrEmpty(email)) continue;

                var userName = email.Split('@')[0];

                var maBoPhan = boPhanMap.ContainsKey(item.DonVi_ID)
                    ? boPhanMap[item.DonVi_ID]
                    : null;

                var maChucVu = chucVuMap.ContainsKey(item.ChucDanh_ID)
                    ? chucVuMap[item.ChucDanh_ID]
                    : null;

                table.Rows.Add(
                    email,
                    userName,
                    item.Ma_NV,
                    $"{item.Ho_NV} {item.Ten_NV}",
                    item.DienThoai_DD,
                    maChucVu,
                    maBoPhan,
                    item.HoatDong,
                    item.Da_Xoa,
                    item.Ngay_Tao ?? DateTime.Now,
                    item.Nguoi_Tao ?? "job-system",
                    false
                );
            }

            return table;
        }

        #endregion
    }
}
