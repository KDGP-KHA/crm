using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using TSFramework.Libs.Helpers;
using TSFramework.Libs.Utils;

namespace TSFramework.Libs.Providers
{
    public class FileUploadResult
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
    }
    public class FileProvider
    {
        public FileUploadResult UploadFile(HttpPostedFileBase file, string folderPath, string[] allowedExtensions = null)
        {
            if (file == null || file.ContentLength == 0)
            {
                return new FileUploadResult { ErrorCode = -1, Message = "No file selected." };
            }

            if (!FileSecurityHelper.IsAllowedUpload(file.FileName, allowedExtensions))
            {
                return new FileUploadResult { ErrorCode = -2, Message = "Tệp tin không hợp lệ hoặc bị từ chối vì lý do an toàn thông tin." };
            }

            var extension = Path.GetExtension(file.FileName).ToLower();

            var absoluteAvatarFolderPath = HostingEnvironment.MapPath(folderPath);
            if (!Directory.Exists(absoluteAvatarFolderPath)) Directory.CreateDirectory(absoluteAvatarFolderPath);

            // Thêm hậu tố thời gian hiện tại vào tên file
            var fileNameWithoutExtension = UtilString.ConvertToUnSign(Path.GetFileNameWithoutExtension(file.FileName)).Replace(" ", "");
            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var newFileName = $"{fileNameWithoutExtension}_{timeStamp}{extension}";
            var path = Path.Combine(absoluteAvatarFolderPath, newFileName);

            try
            {
                file.SaveAs(path);
                return new FileUploadResult { ErrorCode = 1, Message = "File uploaded successfully.", FileName = newFileName };
            }
            catch (Exception ex)
            {
                return new FileUploadResult { ErrorCode = -3, Message = $"Error while uploading file: {ex.Message}" };
            }
        }
    }
}