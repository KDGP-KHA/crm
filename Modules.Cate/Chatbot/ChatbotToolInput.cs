using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Modules.Cate.Chatbot
{
    public sealed class ChatbotToolInput
    {
        public string Keyword { get; private set; }
        public int? Id { get; private set; }
        public int? CustomerId { get; private set; }
        public int? DepartmentId { get; private set; }
        public int? EmployeeId { get; private set; }
        public int? StatusId { get; private set; }
        public byte? BusinessType { get; private set; }
        public int? Year { get; private set; }
        public int Limit { get; private set; }
        public int Offset { get; private set; }

        public static ChatbotToolInput Parse(string toolName, JObject input)
        {
            input = input ?? new JObject();
            bool detail = toolName.EndsWith("_detail", StringComparison.Ordinal);
            var allowed = new HashSet<string>(detail
                ? new[] { "id", "keyword", "limit", "offset" }
                : new[] { "keyword", "customerId", "departmentId", "employeeId", "statusId", "businessType", "year", "limit", "offset" }, StringComparer.Ordinal);
            if (input.Properties().Any(p => !allowed.Contains(p.Name))) Bad("Unsupported input property.");
            var keyword = input["keyword"];
            if (keyword != null && keyword.Type != JTokenType.String) Bad("keyword must be a string.");
            var result = new ChatbotToolInput
            {
                Keyword = ((string)keyword)?.Trim(),
                Id = Number(input, "id", 1, int.MaxValue),
                CustomerId = Number(input, "customerId", 1, int.MaxValue),
                DepartmentId = Number(input, "departmentId", 1, int.MaxValue),
                EmployeeId = Number(input, "employeeId", 1, int.MaxValue),
                StatusId = Number(input, "statusId", 1, int.MaxValue),
                BusinessType = (byte?)Number(input, "businessType", 0, 2),
                Year = Number(input, "year", 1900, 2100),
                Limit = Number(input, "limit", 1, 50) ?? 10,
                Offset = Number(input, "offset", 0, 5000) ?? 0
            };
            if (result.Keyword != null && result.Keyword.Length > 200) Bad("keyword must not exceed 200 characters.");
            if (detail && !result.Id.HasValue && string.IsNullOrWhiteSpace(result.Keyword))
                Bad("Provide id or keyword to identify the record.");
            if (detail && result.Id.HasValue && !string.IsNullOrWhiteSpace(result.Keyword))
                Bad("Provide either id or keyword, not both.");
            return result;
        }

        private static int? Number(JObject input, string name, int min, int max)
        {
            var token = input[name];
            if (token == null) return null;
            long value;
            if (token.Type != JTokenType.Integer || !long.TryParse(token.ToString(), out value) || value < min || value > max)
                Bad(name + " must be an integer between " + min + " and " + max + ".");
            return (int)(long)token;
        }

        private static void Bad(string message)
        { throw new ChatbotToolException(HttpStatusCode.BadRequest, "invalid_input", message); }
    }
}
