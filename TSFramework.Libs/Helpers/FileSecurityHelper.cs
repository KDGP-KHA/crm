using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;

namespace TSFramework.Libs.Helpers
{
    /// <summary>
    /// Tiện ích kiểm soát an toàn tệp tin (Chống Web Shell, Zip Slip, Path Traversal).
    /// </summary>
    public static class FileSecurityHelper
    {
        /// <summary>
        /// Danh sách các phần mở rộng nguy hiểm TUYỆT ĐỐI CẤM tải lên hoặc giải nén.
        /// </summary>
        private static readonly HashSet<string> BlockedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".aspx", ".ashx", ".asmx", ".asp", ".cshtml", ".vbhtml", ".config", ".dll",
            ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar", ".php", ".phtml",
            ".php3", ".php4", ".php5", ".cgi", ".cer", ".shtml", ".jsp", ".jspx",
            ".hta", ".scr", ".com", ".pif", ".msi", ".reg", ".sh"
        };

        /// <summary>
        /// Danh mục phần mở rộng tệp tài liệu / văn phòng và tệp nén thông thường được phép.
        /// </summary>
        private static readonly HashSet<string> DefaultAllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".csv", ".rtf", ".odt", ".ods",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
            ".zip", ".rar", ".7z"
        };

        /// <summary>
        /// Danh mục phần mở rộng ảnh được phép (dành cho Avatar, ảnh đại diện).
        /// </summary>
        private static readonly HashSet<string> DefaultImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
        };

        /// <summary>
        /// Kiểm tra phần mở rộng của tệp tải lên có an toàn hay không (Anti-Webshell).
        /// </summary>
        /// <param name="fileName">Tên tệp gốc</param>
        /// <param name="customAllowedExtensions">Danh mục mở rộng cho phép tùy biến (nếu có)</param>
        /// <returns>True nếu hợp lệ, False nếu là tệp nguy hiểm hoặc ngoài danh mục cho phép</returns>
        public static bool IsAllowedUpload(string fileName, IEnumerable<string> customAllowedExtensions = null)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            // Chống tấn công Null-byte injection
            if (fileName.IndexOf('\0') >= 0) return false;

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext)) return false;

            // Kiểm tra Blacklist tuyệt đối
            if (BlockedExtensions.Contains(ext)) return false;

            // Chặn kỹ thuật tấn công Double Extension (vd: shell.aspx.txt, exploit.cer.png)
            var baseWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var secondExt = Path.GetExtension(baseWithoutExt);
            if (!string.IsNullOrEmpty(secondExt) && BlockedExtensions.Contains(secondExt)) return false;

            var allowedSet = customAllowedExtensions != null
                ? new HashSet<string>(customAllowedExtensions, StringComparer.OrdinalIgnoreCase)
                : DefaultAllowedExtensions;

            return allowedSet.Contains(ext);
        }

        /// <summary>
        /// Kiểm tra tệp ảnh đại diện (Avatar) có hợp lệ hay không.
        /// </summary>
        public static bool IsAllowedImage(string fileName)
        {
            return IsAllowedUpload(fileName, DefaultImageExtensions);
        }

        /// <summary>
        /// Giải nén an toàn một entry trong file ZIP (Chống triệt để lỗ hổng Zip Slip - CWE-29).
        /// </summary>
        /// <param name="entry">Entry trong tệp ZIP</param>
        /// <param name="destinationDirectory">Thư mục đích giải nén</param>
        /// <param name="allowExecutable">Cho phép file nhị phân / dll hay không (chỉ dùng khi cài module)</param>
        /// <returns>Đường dẫn tệp đã giải nén an toàn</returns>
        public static string SafeExtractZipEntry(ZipArchiveEntry entry, string destinationDirectory, bool allowExecutable = false)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(destinationDirectory)) throw new ArgumentNullException(nameof(destinationDirectory));

            var destFolderFull = Path.GetFullPath(destinationDirectory);
            if (!destFolderFull.EndsWith(Path.DirectorySeparatorChar.ToString()))
                destFolderFull += Path.DirectorySeparatorChar;

            // Kiểm tra đường dẫn tệp đích
            var targetFilePath = Path.GetFullPath(Path.Combine(destFolderFull, entry.FullName));

            // Xác minh tệp đích bắt buộc phải nằm trọn vẹn trong thư mục giải nén
            if (!targetFilePath.StartsWith(destFolderFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Phát hiện tệp độc hại cố tình ghi đè ra ngoài thư mục đích (Zip Slip): {entry.FullName}");
            }

            var ext = Path.GetExtension(targetFilePath);

            // Nếu không phải là thư mục và không cho phép thực thi, chặn các extension nguy hiểm
            if (!string.IsNullOrEmpty(ext))
            {
                if (!allowExecutable && BlockedExtensions.Contains(ext))
                {
                    throw new SecurityException($"Tệp giải nén chứa định dạng nguy hại không được phép: {ext}");
                }

                var dir = Path.GetDirectoryName(targetFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(targetFilePath)) File.Delete(targetFilePath);
                entry.ExtractToFile(targetFilePath);
            }
            else
            {
                // Là thư mục
                if (!Directory.Exists(targetFilePath))
                {
                    Directory.CreateDirectory(targetFilePath);
                }
            }

            return targetFilePath;
        }

        /// <summary>
        /// Lấy và xác thực đường dẫn an toàn của file Log (Chống Path Traversal).
        /// </summary>
        /// <param name="fileName">Tên file log người dùng yêu cầu</param>
        /// <param name="logFolderPhysicalPath">Đường dẫn vật lý của thư mục log</param>
        /// <returns>Đường dẫn an toàn hoặc null nếu phát hiện gian lận đường dẫn</returns>
        public static string GetSafeLogPath(string fileName, string logFolderPhysicalPath)
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(logFolderPhysicalPath))
                return null;

            // Chống tấn công Null-byte
            if (fileName.IndexOf('\0') >= 0) return null;

            // Lấy tên tệp thuần túy, loại bỏ hoàn toàn các ký tự ../ hoặc ..\
            var pureFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(pureFileName) || !string.Equals(pureFileName, fileName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var baseFolderFull = Path.GetFullPath(logFolderPhysicalPath);
            if (!baseFolderFull.EndsWith(Path.DirectorySeparatorChar.ToString()))
                baseFolderFull += Path.DirectorySeparatorChar;

            var targetPath = Path.GetFullPath(Path.Combine(baseFolderFull, pureFileName));
            if (!targetPath.StartsWith(baseFolderFull, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return targetPath;
        }
    }
}
