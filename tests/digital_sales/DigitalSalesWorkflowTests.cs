using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using Core.Cate.Biz;
using Core.Cate.Caches;
using Core.Cate.Models;

public static class DigitalSalesWorkflowTests
{
    private static int _passed = 0;
    private static int _failed = 0;
    private static string _connectionString;
    private static string _crmRoot;

    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: DigitalSalesWorkflowTests.exe <crmBinDir> <connectionString>");
            return 1;
        }

        var crmBin = args[0];
        _connectionString = args[1];
        _crmRoot = Directory.GetParent(Directory.GetParent(crmBin).FullName).FullName;

        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var assemblyName = new AssemblyName(e.Name).Name + ".dll";
            var path = Path.Combine(crmBin, assemblyName);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
            return null;
        };

        Console.WriteLine("==========================================================");
        Console.WriteLine("BẮT ĐẦU KIỂM THỬ: Quản lý Danh mục Trạng thái ➔ Quy trình ➔ Tiến trình");
        Console.WriteLine("==========================================================");

        try
        {
            // Set Connection String in AppDomain configuration
            RunClientPartialRefreshTests();
            RunDirectDatabaseTests();
            RunBizWorkflowTests();
            RunEdgeCaseAndErrorHandlingTests();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("FATAL EXCEPTION: " + ex);
            Console.ResetColor();
            _failed++;
        }

        Console.WriteLine("----------------------------------------------------------");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"KẾT QUẢ: TẤT CẢ {_passed} BÀI KIỂM THỬ ĐỀU ĐẠT (PASS 100%).");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"KẾT QUẢ: {_passed} passed, {_failed} failed.");
            Console.ResetColor();
            return 1;
        }
    }

    private static void RunClientPartialRefreshTests()
    {
        Console.WriteLine("\n[UI AJAX] Kiểm tra tải cục bộ Quy trình và Tiến trình:");

        var scriptPath = Path.Combine(
            _crmRoot,
            "Modules.Cate",
            "Areas",
            "Cate",
            "Views",
            "DigitalSalesWorkflow",
            "DigitalSalesWorkflow.js");
        var indexPath = Path.Combine(
            _crmRoot,
            "Modules.Cate",
            "Areas",
            "Cate",
            "Views",
            "DigitalSalesWorkflow",
            "Index.cshtml");

        var script = File.ReadAllText(scriptPath);
        var index = File.ReadAllText(indexPath);

        Assert(!script.Contains("location.reload") && !script.Contains("window.location"),
            "Chọn Trạng thái/Quy trình không được tải lại toàn bộ trang.");
        Assert(script.Contains("loadProcesses(state.statusId);")
            && script.Contains("function autoSelectFirstProcess()")
            && script.Contains("selectProcess(firstItem.data(\"id\"), firstItem[0]);"),
            "Chọn Trạng thái tải danh sách Quy trình và tự chọn Quy trình đầu tiên.");
        Assert(script.Contains("loadProgresses(state.processId);"),
            "Quy trình đầu tiên tải PartialView danh sách Tiến trình.");
        Assert(CountOccurrences(script, "global: false") >= 3,
            "Các request PartialView phải tắt global AJAX loader và chỉ hiển thị loading trong từng cột.");
        Assert(script.Contains("replacePartial($container, html, \"#processListItems\")")
            && script.Contains("replacePartial($container, html, \"#progressListItems\")"),
            "Client chỉ chấp nhận đúng markup PartialView của từng cột.");
        Assert(script.Contains("function resetProgressColumn()")
            && script.Contains("abortPendingRequest(\"progresses\")"),
            "Khi đổi Trạng thái, request Tiến trình cũ phải bị hủy để không ghi đè trạng thái chờ.");
        Assert(index.Contains("DigitalSalesWorkflow.js?v=@DateTime.Now.Ticks"),
            "Script DigitalSalesWorkflow phải có cache-busting để client nhận phiên bản mới.");
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var startIndex = 0;
        while ((startIndex = value.IndexOf(search, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += search.Length;
        }
        return count;
    }

    private static void Assert(bool condition, string testName)
    {
        if (condition)
        {
            _passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {testName}");
            Console.ResetColor();
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {testName}");
            Console.ResetColor();
            throw new Exception($"Assertion failed: {testName}");
        }
    }

    #region 1. Tầng 1: Happy Path Tests
    private static void RunDirectDatabaseTests()
    {
        Console.WriteLine("\n[TẦNG 1: HAPPY PATH] Kiểm tra truy vấn & dữ liệu cơ sở dữ liệu:");

        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // 1.1 Kiểm tra 8 Trạng thái tiêu chuẩn (3 Cơ hội, 5 Dự án)
            using (var cmd = new SqlCommand("SELECT StatusID, BusinessType, StatusCode, StatusName, IsDefault FROM dbo.RM_DigitalSalesStatus ORDER BY StatusID", conn))
            using (var reader = cmd.ExecuteReader())
            {
                var statuses = new List<dynamic>();
                while (reader.Read())
                {
                    statuses.Add(new
                    {
                        StatusID = reader.GetInt32(0),
                        BusinessType = reader.GetByte(1),
                        StatusCode = reader.GetString(2),
                        StatusName = reader.GetString(3),
                        IsDefault = reader.GetBoolean(4)
                    });
                }

                Assert(statuses.Count >= 8, "Cơ sở dữ liệu phải có ít nhất 8 trạng thái tiêu chuẩn.");
                Assert(statuses.Count(s => s.BusinessType == 1) == 3, "Có đúng 3 trạng thái cho Cơ hội (Chưa nắm bắt, Đang tiếp cận, Đã bỏ mất).");
                Assert(statuses.Count(s => s.BusinessType == 2) == 5, "Có đúng 5 trạng thái cho Dự án (Hình thành, Triển khai, Mất dự án, Ký HĐ, Hoàn thành).");

                var defaultOpportunity = statuses.FirstOrDefault(s => s.BusinessType == 1 && s.IsDefault);
                Assert(defaultOpportunity != null && defaultOpportunity.StatusID == 1, "Trạng thái mặc định của Cơ hội là ID=1 (Chưa nắm bắt).");

                var defaultProject = statuses.FirstOrDefault(s => s.BusinessType == 2 && s.IsDefault);
                Assert(defaultProject != null && defaultProject.StatusID == 4, "Trạng thái mặc định của Dự án là ID=4 (Giai đoạn Hình thành dự án).");
            }

            // 1.2 Kiểm tra Stored Procedure RM_DigitalSalesProgress_Save hỗ trợ DefaultDurationDays
            using (var cmd = new SqlCommand("SELECT PARAMETER_NAME FROM INFORMATION_SCHEMA.PARAMETERS WHERE SPECIFIC_NAME = 'RM_DigitalSalesProgress_Save' AND PARAMETER_NAME = '@DefaultDurationDays'", conn))
            {
                var result = cmd.ExecuteScalar();
                Assert(result != null && result.ToString().Equals("@DefaultDurationDays", StringComparison.OrdinalIgnoreCase),
                    "Stored procedure RM_DigitalSalesProgress_Save phải nhận tham số @DefaultDurationDays.");
            }
        }
    }

    private static void RunBizWorkflowTests()
    {
        Console.WriteLine("\n[TẦNG 1: HAPPY PATH] Kiểm tra CRUD Quy trình ➔ Tiến trình (với DefaultDurationDays):");

        var biz = new RM_DigitalSalesWorkflowBiz();
        int testProcessId = 0;
        int testProgressId = 0;

        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // 2.1 Tạo Quy trình mới thuộc StatusID = 5 (Triển khai dự án)
            var procCode = "TEST_PROC_" + DateTime.Now.Ticks.ToString().Substring(10);
            var procName = "Quy trình kiểm thử tự động";
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProcess_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessID", 0);
                cmd.Parameters.AddWithValue("@StatusID", 5);
                cmd.Parameters.AddWithValue("@ProcessCode", procCode);
                cmd.Parameters.AddWithValue("@ProcessName", procName);
                cmd.Parameters.AddWithValue("@Description", "Mô tả quy trình kiểm thử");
                cmd.Parameters.AddWithValue("@SortOrder", 99);
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");

                var outParam = new SqlParameter("@RetVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                cmd.Parameters.Add(outParam);

                var scalar = cmd.ExecuteScalar();
                testProcessId = scalar != null ? Convert.ToInt32(scalar) : (int)outParam.Value;
                Assert(testProcessId > 0, $"Thêm mới Quy trình thành công (ID = {testProcessId}).");
            }

            // 2.2 Tạo Tiến trình mới thuộc Quy trình vừa tạo với DefaultDurationDays = 7 ngày
            var progCode = "TEST_PROG_" + DateTime.Now.Ticks.ToString().Substring(10);
            var progName = "Tiến trình kiểm thử SLA 7 ngày";
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", 0);
                cmd.Parameters.AddWithValue("@ProcessID", testProcessId);
                cmd.Parameters.AddWithValue("@ProgressCode", progCode);
                cmd.Parameters.AddWithValue("@ProgressName", progName);
                cmd.Parameters.AddWithValue("@DefaultDurationDays", 7);
                cmd.Parameters.AddWithValue("@Description", "Mô tả tiến trình SLA");
                cmd.Parameters.AddWithValue("@SortOrder", 1);
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");

                var outParam = new SqlParameter("@RetVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                cmd.Parameters.Add(outParam);

                var scalar = cmd.ExecuteScalar();
                testProgressId = scalar != null ? Convert.ToInt32(scalar) : (int)outParam.Value;
                Assert(testProgressId > 0, $"Thêm mới Tiến trình có DefaultDurationDays = 7 thành công (ID = {testProgressId}).");
            }

            // 2.3 Truy vấn lại Tiến trình bằng RM_DigitalSalesProgress_GetByID để kiểm tra DefaultDurationDays
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_GetByID", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", testProgressId);

                using (var reader = cmd.ExecuteReader())
                {
                    Assert(reader.Read(), "Phải đọc được bản ghi Tiến trình vừa thêm.");
                    var retrievedDays = reader.GetInt32(reader.GetOrdinal("DefaultDurationDays"));
                    var retrievedName = reader.GetString(reader.GetOrdinal("ProgressName"));
                    var retrievedCode = reader.GetString(reader.GetOrdinal("ProgressCode"));

                    Assert(retrievedDays == 7, $"Tiến trình phải lưu đúng DefaultDurationDays = 7 (thực tế: {retrievedDays}).");
                    Assert(retrievedName == progName, "Tiến trình phải lưu đúng ProgressName.");
                    Assert(retrievedCode == progCode, "Tiến trình phải lưu đúng ProgressCode.");
                }
            }

            // 2.4 Cập nhật DefaultDurationDays từ 7 thành 14 ngày
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", testProgressId);
                cmd.Parameters.AddWithValue("@ProcessID", testProcessId);
                cmd.Parameters.AddWithValue("@ProgressCode", progCode);
                cmd.Parameters.AddWithValue("@ProgressName", progName + " - Đã sửa");
                cmd.Parameters.AddWithValue("@DefaultDurationDays", 14);
                cmd.Parameters.AddWithValue("@Description", "Đã cập nhật SLA 14 ngày");
                cmd.Parameters.AddWithValue("@SortOrder", 2);
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");

                var scalar = cmd.ExecuteScalar();
                var updateRes = Convert.ToInt32(scalar);
                Assert(updateRes > 0, "Cập nhật Tiến trình thành công.");
            }

            // Kiểm tra lại sau cập nhật
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_GetByID", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", testProgressId);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    var retrievedDays = reader.GetInt32(reader.GetOrdinal("DefaultDurationDays"));
                    Assert(retrievedDays == 14, $"Cập nhật thành công: DefaultDurationDays = 14 (thực tế: {retrievedDays}).");
                }
            }

            // 2.5 Xóa Tiến trình kiểm thử
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_Delete", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", testProgressId);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");
                var delRes = cmd.ExecuteNonQuery();
                Assert(delRes != 0, "Xóa Tiến trình kiểm thử thành công.");
            }

            // 2.6 Xóa Quy trình kiểm thử
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProcess_Delete", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessID", testProcessId);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");
                var delRes = cmd.ExecuteNonQuery();
                Assert(delRes != 0, "Xóa Quy trình kiểm thử thành công.");
            }
        }
    }
    #endregion

    #region 2. Tầng 2 & 3: Edge Cases & Error Handling Tests
    private static void RunEdgeCaseAndErrorHandlingTests()
    {
        Console.WriteLine("\n[TẦNG 2 & 3: EDGE CASES & ERROR HANDLING] Kiểm tra dữ liệu biên & bẫy lỗi:");

        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // 3.1 Error Handling: Thêm quy trình trùng mã trong cùng Status phải trả về -9
            int tempProcId1 = 0;
            var duplicateCode = "DUP_CODE_" + DateTime.Now.Ticks.ToString().Substring(10);

            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProcess_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessID", 0);
                cmd.Parameters.AddWithValue("@StatusID", 5);
                cmd.Parameters.AddWithValue("@ProcessCode", duplicateCode);
                cmd.Parameters.AddWithValue("@ProcessName", "Quy trình bản gốc");
                cmd.Parameters.AddWithValue("@Description", "");
                cmd.Parameters.AddWithValue("@SortOrder", 1);
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");

                var scalar = cmd.ExecuteScalar();
                tempProcId1 = Convert.ToInt32(scalar);
                Assert(tempProcId1 > 0, "Tạo quy trình bản gốc thành công.");
            }

            // Thử tạo quy trình thứ 2 cùng StatusID = 5 và cùng duplicateCode
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProcess_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessID", 0);
                cmd.Parameters.AddWithValue("@StatusID", 5);
                cmd.Parameters.AddWithValue("@ProcessCode", duplicateCode);
                cmd.Parameters.AddWithValue("@ProcessName", "Quy trình trùng mã");
                cmd.Parameters.AddWithValue("@Description", "");
                cmd.Parameters.AddWithValue("@SortOrder", 2);
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");

                var scalar = cmd.ExecuteScalar();
                var result = Convert.ToInt32(scalar);
                Assert(result == -9, $"Bẫy lỗi trùng mã Quy trình: Kết quả trả về phải là -9 (thực tế: {result}).");
            }

            // 3.2 Error Handling: Thêm tiến trình trùng mã trong cùng Process phải trả về -9
            int tempProgId1 = 0;
            var duplicateProgCode = "DUP_PROG_" + DateTime.Now.Ticks.ToString().Substring(10);

            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", 0);
                cmd.Parameters.AddWithValue("@ProcessID", tempProcId1);
                cmd.Parameters.AddWithValue("@ProgressCode", duplicateProgCode);
                cmd.Parameters.AddWithValue("@ProgressName", "Tiến trình bản gốc");
                cmd.Parameters.AddWithValue("@DefaultDurationDays", 3);
                cmd.Parameters.AddWithValue("@Description", "");
                cmd.Parameters.AddWithValue("@SortOrder", 1);
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");

                var scalar = cmd.ExecuteScalar();
                tempProgId1 = Convert.ToInt32(scalar);
                Assert(tempProgId1 > 0, "Tạo tiến trình bản gốc thành công.");
            }

            // Thử tạo tiến trình thứ 2 cùng ProcessID và cùng duplicateProgCode
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_Save", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", 0);
                cmd.Parameters.AddWithValue("@ProcessID", tempProcId1);
                cmd.Parameters.AddWithValue("@ProgressCode", duplicateProgCode);
                cmd.Parameters.AddWithValue("@ProgressName", "Tiến trình trùng mã");
                cmd.Parameters.AddWithValue("@DefaultDurationDays", 5);
                cmd.Parameters.AddWithValue("@Description", "");
                cmd.Parameters.AddWithValue("@SortOrder", 2);
                cmd.Parameters.AddWithValue("@IsActive", true);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");

                var scalar = cmd.ExecuteScalar();
                var result = Convert.ToInt32(scalar);
                Assert(result == -9, $"Bẫy lỗi trùng mã Tiến trình: Kết quả trả về phải là -9 (thực tế: {result}).");
            }

            // Dọn dẹp bản ghi kiểm thử
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_Delete", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", tempProgId1);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProcess_Delete", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessID", tempProcId1);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");
                cmd.ExecuteNonQuery();
            }

            // 3.3 Edge Cases: Query với ID không tồn tại (StatusID = 999999)
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProcess_Get", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Search", DBNull.Value);
                cmd.Parameters.AddWithValue("@BusinessType", DBNull.Value);
                cmd.Parameters.AddWithValue("@StatusID", 999999);
                cmd.Parameters.AddWithValue("@Order", "0");
                cmd.Parameters.AddWithValue("@OrderDir", "ASC");
                cmd.Parameters.AddWithValue("@PageIndex", 0);
                cmd.Parameters.AddWithValue("@PageSize", 100);

                using (var reader = cmd.ExecuteReader())
                {
                    var count = 0;
                    while (reader.Read()) count++;
                    Assert(count == 0, "Query Process với StatusID = 999999 phải trả về danh sách rỗng (0 bản ghi).");
                }
            }

            // 3.4 Edge Cases: Query Progress với ProcessID không tồn tại (ProcessID = 999999)
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_GetByProcess", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProcessID", 999999);

                using (var reader = cmd.ExecuteReader())
                {
                    var count = 0;
                    while (reader.Read()) count++;
                    Assert(count == 0, "Query Progress với ProcessID = 999999 phải trả về danh sách rỗng (0 bản ghi).");
                }
            }

            // 3.5 Edge Cases: Xóa ID không tồn tại
            using (var cmd = new SqlCommand("dbo.RM_DigitalSalesProgress_Delete", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProgressID", 999999);
                cmd.Parameters.AddWithValue("@UserName", "TestRunner");
                var scalar = cmd.ExecuteScalar();
                var affected = Convert.ToInt32(scalar);
                Assert(affected == 0, "Xóa Progress không tồn tại phải trả về 0 rows affected, không được crash.");
            }
        }
    }
    #endregion
}
