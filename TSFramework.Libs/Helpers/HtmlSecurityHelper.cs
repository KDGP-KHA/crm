using System;
using System.Text.RegularExpressions;

namespace TSFramework.Libs.Helpers
{
    /// <summary>
    /// Helper phòng thủ tấn công Cross-Site Scripting (XSS) cho nội dung HTML từ CKEditor / RichText.
    /// </summary>
    public static class HtmlSecurityHelper
    {
        // 1. Các thẻ nguy hiểm tuyệt đối bị cấm (loại bỏ cả thẻ tự đóng và thẻ có cặp đóng mở)
        private static readonly Regex DangerousTagsRegex = new Regex(
            @"<\s*(script|iframe|object|embed|applet|meta|link|style|form|base|svg)\b[^>]*>([\s\S]*?<\s*/\s*\1\s*>)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // 2. Các thuộc tính sự kiện inline JavaScript (onload, onerror, onclick, onmouseover, ...)
        private static readonly Regex InlineEventsRegex = new Regex(
            @"\s+on[a-zA-Z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // 3. Các giao thức nguy hiểm trong href hoặc src (javascript:, vbscript:, data:text/html)
        private static readonly Regex DangerousProtocolsRegex = new Regex(
            @"\b(href|src)\s*=\s*[""']?\s*(javascript|vbscript|data\s*:\s*text/html):[^""'>\s]*[""']?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // 4. Các biểu thức CSS nguy hiểm như expression(...) hoặc behavior:...
        private static readonly Regex DangerousStylesRegex = new Regex(
            @"\bstyle\s*=\s*[""'][^""']*(expression\(|behavior:|javascript:)[^""']*[""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Làm sạch chuỗi HTML, loại bỏ mã script độc hại, giữ nguyên các thẻ định dạng văn bản hợp lệ.
        /// </summary>
        /// <param name="rawHtml">Nội dung HTML gốc từ form / editor</param>
        /// <returns>Chuỗi HTML an toàn</returns>
        public static string SanitizeHtml(string rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
                return rawHtml;

            string sanitized = rawHtml;

            // Loại bỏ các thẻ độc hại (script, iframe, style, form, embed...)
            sanitized = DangerousTagsRegex.Replace(sanitized, string.Empty);

            // Loại bỏ inline event handlers (onclick, onerror, onload...)
            sanitized = InlineEventsRegex.Replace(sanitized, string.Empty);

            // Loại bỏ pseudo-protocols (javascript:, vbscript:)
            sanitized = DangerousProtocolsRegex.Replace(sanitized, string.Empty);

            // Loại bỏ dangerous CSS expressions
            sanitized = DangerousStylesRegex.Replace(sanitized, string.Empty);

            return sanitized;
        }

        /// <summary>
        /// Kiểm tra nhanh xem nội dung có chứa payload nghi ngờ XSS hay không.
        /// </summary>
        public static bool ContainsXssPayload(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return DangerousTagsRegex.IsMatch(input)
                   || InlineEventsRegex.IsMatch(input)
                   || DangerousProtocolsRegex.IsMatch(input)
                   || DangerousStylesRegex.IsMatch(input);
        }
    }
}
