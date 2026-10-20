using System;
using System.Collections.Generic;
using System.Linq;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class NhanVienDto
    {
        public int NhanVien_ID { get; set; }
        public string Ma_NV { get; set; }
        public string Ho_NV { get; set; }
        public string Ten_NV { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string DienThoai_DD { get; set; }
        public string DienThoai_CD { get; set; }
        public bool Gioi_Tinh { get; set; }
        public string Email { get; set; }
        public string Mat_Khau { get; set; }
        public int DonVi_ID { get; set; }
        public int ChucDanh_ID { get; set; }
        public DateTime? Ngay_Tao { get; set; }
        public DateTime? Ngay_CN { get; set; }
        public string Nguoi_Tao { get; set; }
        public string Nguoi_CN { get; set; }
        public bool Da_Xoa { get; set; }
        public decimal? He_So_HS3P { get; set; }
        public int CapQuanLy { get; set; }
        public bool HoatDong { get; set; }
        public int? LevelManager { get; set; }
    }
    public class BoPhanDto
    {
        public int DonVi_ID { get; set; }
        public string Ma_DV { get; set; }
        public string Ten_DV { get; set; }
        public int DonVi_Cha_ID { get; set; }
        public DateTime? Ngay_Tao { get; set; }
        public DateTime? Ngay_CN { get; set; }
        public string Nguoi_Tao { get; set; }
        public string Nguoi_CN { get; set; }
        public bool Da_Xoa { get; set; }
        public string Ma_DV_BCN { get; set; }
        public int CapQuanLy { get; set; }
    }

    public class ChucVuDto
    {
        public int ChucDanh_ID { get; set; }
        public string Ma_CD { get; set; }
        public string Ten_CD { get; set; }
        public bool Lanh_Dao { get; set; }
        public DateTime? Ngay_Tao { get; set; }
        public DateTime? Ngay_CN { get; set; }
    }
    public class ApiResponse<T>
    {
        public List<T> Data { get; set; }
    }

    #region === CAS HRM DTOs (/api/hrmcas) ===

    /// <summary>
    /// Tham số gửi yêu cầu lấy dữ liệu từ API CAS HRM (/api/hrmcas)
    /// </summary>
    public class CasApiRequest
    {
        public string partnerCode { get; set; }
        public string requestId { get; set; }
        public string requestTime { get; set; }
        public string resource { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 100;
        public string keyword { get; set; } = "";
        public string checksum { get; set; }
    }

    /// <summary>
    /// Kết quả phản hồi từ API CAS HRM (/api/hrmcas)
    /// </summary>
    public class CasApiResponse<T>
    {
        public string code { get; set; }
        public string message { get; set; }
        public string requestId { get; set; }
        public string resource { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public int totalItems { get; set; }
        public int totalPages { get; set; }
        public List<T> data { get; set; }
        public string checksum { get; set; }
    }

    /// <summary>
    /// DTO thông tin nhân sự từ API CAS HRM
    /// </summary>
    public class CasEmployeeDto
    {
        public string code { get; set; }
        public string fullName { get; set; }
        public string phoneNumber { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public string birthday { get; set; }
        public int departmentId { get; set; }
        public string departmentCode { get; set; }
        public string department { get; set; }
        public string jobCode { get; set; }
        public string job { get; set; }
        public string workPositionCode { get; set; }
        public string workPosition { get; set; }
        public bool isCollaborator { get; set; }
    }

    /// <summary>
    /// DTO thông tin phòng ban từ API CAS HRM
    /// </summary>
    public class CasDepartmentDto
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public int parentId { get; set; }
        public string parentCode { get; set; }
        public int employeeCount { get; set; }
    }

    /// <summary>
    /// DTO thông tin chức danh từ API CAS HRM
    /// </summary>
    public class CasJobDto
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public int employeeCount { get; set; }
    }

    #endregion
}
