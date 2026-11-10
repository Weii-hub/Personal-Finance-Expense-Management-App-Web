using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    [Authorize]
    public class BudgetsController : Controller
    {
        private readonly string _connectionString;

        public BudgetsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Không tìm thấy ConnectionStrings:DefaultConnection.");
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Không xác định được người dùng hiện tại.");

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var selectedMonth = month ?? DateTime.Today.Month;
            var selectedYear = year ?? DateTime.Today.Year;
            if (selectedMonth < 1 || selectedMonth > 12 || selectedYear < 2000 || selectedYear > 2100)
                return BadRequest("Tháng hoặc năm không hợp lệ.");

            var budgets = new List<Budget>();
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT b."Id", b."UserId", b."CategoryId", b."Amount", b."Month", b."Year",
                       c."Name",
                       COALESCE(SUM(t."Amount"), 0) AS "SpentAmount"
                FROM "Budgets" b
                INNER JOIN "Categories" c ON c."Id" = b."CategoryId"
                LEFT JOIN "Transactions" t
                    ON t."CategoryId" = b."CategoryId"
                    AND t."UserId" = b."UserId"
                    AND t."TransactionDate" >= @monthStart
                    AND t."TransactionDate" < @monthEnd
                WHERE b."UserId" = @userId
                    AND b."Month" = @month
                    AND b."Year" = @year
                    AND c."Type" = 0
                GROUP BY b."Id", b."UserId", b."CategoryId", b."Amount", b."Month", b."Year", c."Name"
                ORDER BY c."Name";
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", CurrentUserId);
            command.Parameters.AddWithValue("month", selectedMonth);
            command.Parameters.AddWithValue("year", selectedYear);
            command.Parameters.AddWithValue("monthStart", new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc));
            command.Parameters.AddWithValue("monthEnd", new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1));

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                budgets.Add(new Budget
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetString(1),
                    CategoryId = reader.GetInt32(2),
                    Amount = reader.GetDecimal(3),
                    Month = reader.GetInt32(4),
                    Year = reader.GetInt32(5),
                    CategoryName = reader.GetString(6),
                    SpentAmount = reader.GetDecimal(7)
                });
            }

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            return View(budgets);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadExpenseCategoriesAsync();
            return View(new Budget { Month = DateTime.Today.Month, Year = DateTime.Today.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Budget budget)
        {
            ModelState.Remove(nameof(budget.UserId));
            ModelState.Remove(nameof(budget.CategoryName));
            ModelState.Remove(nameof(budget.SpentAmount));
            if (!ModelState.IsValid || !await IsExpenseCategoryAsync(budget.CategoryId))
            {
                if (ModelState.IsValid)
                    ModelState.AddModelError(nameof(budget.CategoryId), "Chỉ được chọn danh mục chi tiêu.");
                await LoadExpenseCategoriesAsync(budget.CategoryId);
                return View(budget);
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = """
                INSERT INTO "Budgets" ("UserId", "CategoryId", "Amount", "Month", "Year")
                VALUES (@userId, @categoryId, @amount, @month, @year)
                ON CONFLICT ("UserId", "CategoryId", "Month", "Year")
                DO UPDATE SET "Amount" = EXCLUDED."Amount";
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", CurrentUserId);
            command.Parameters.AddWithValue("categoryId", budget.CategoryId);
            command.Parameters.AddWithValue("amount", budget.Amount);
            command.Parameters.AddWithValue("month", budget.Month);
            command.Parameters.AddWithValue("year", budget.Year);
            await command.ExecuteNonQueryAsync();

            TempData["SuccessMessage"] = "Đã lưu ngân sách thành công!";
            return RedirectToAction(nameof(Index), new { month = budget.Month, year = budget.Year });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var budget = await FindBudgetAsync(id);
            if (budget == null) return NotFound();
            await LoadExpenseCategoriesAsync(budget.CategoryId);
            return View(budget);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Budget budget)
        {
            if (id != budget.Id) return NotFound();
            ModelState.Remove(nameof(budget.UserId));
            ModelState.Remove(nameof(budget.CategoryName));
            ModelState.Remove(nameof(budget.SpentAmount));
            if (!ModelState.IsValid || !await IsExpenseCategoryAsync(budget.CategoryId))
            {
                if (ModelState.IsValid)
                    ModelState.AddModelError(nameof(budget.CategoryId), "Chỉ được chọn danh mục chi tiêu.");
                await LoadExpenseCategoriesAsync(budget.CategoryId);
                return View(budget);
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = """
                UPDATE "Budgets"
                SET "CategoryId" = @categoryId, "Amount" = @amount, "Month" = @month, "Year" = @year
                WHERE "Id" = @id AND "UserId" = @userId;
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("categoryId", budget.CategoryId);
            command.Parameters.AddWithValue("amount", budget.Amount);
            command.Parameters.AddWithValue("month", budget.Month);
            command.Parameters.AddWithValue("year", budget.Year);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", CurrentUserId);
            if (await command.ExecuteNonQueryAsync() == 0) return NotFound();

            TempData["SuccessMessage"] = "Đã cập nhật ngân sách!";
            return RedirectToAction(nameof(Index), new { month = budget.Month, year = budget.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = "DELETE FROM \"Budgets\" WHERE \"Id\" = @id AND \"UserId\" = @userId;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", CurrentUserId);
            if (await command.ExecuteNonQueryAsync() == 0) return NotFound();
            TempData["SuccessMessage"] = "Đã xóa ngân sách.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<Budget?> FindBudgetAsync(int id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = """
                SELECT b."Id", b."UserId", b."CategoryId", b."Amount", b."Month", b."Year", c."Name"
                FROM "Budgets" b INNER JOIN "Categories" c ON c."Id" = b."CategoryId"
                WHERE b."Id" = @id AND b."UserId" = @userId AND c."Type" = 0;
                """;
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", CurrentUserId);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new Budget
            {
                Id = reader.GetInt32(0), UserId = reader.GetString(1), CategoryId = reader.GetInt32(2),
                Amount = reader.GetDecimal(3), Month = reader.GetInt32(4), Year = reader.GetInt32(5),
                CategoryName = reader.GetString(6)
            };
        }

        private async Task<bool> IsExpenseCategoryAsync(int categoryId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = "SELECT EXISTS (SELECT 1 FROM \"Categories\" WHERE \"Id\" = @id AND \"Type\" = 0);";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", categoryId);
            return (bool)(await command.ExecuteScalarAsync() ?? false);
        }

        private async Task LoadExpenseCategoriesAsync(int? selectedId = null)
        {
            var categories = new List<SelectListItem>();
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            const string sql = "SELECT \"Id\", \"Name\" FROM \"Categories\" WHERE \"Type\" = 0 ORDER BY \"Name\";";
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                categories.Add(new SelectListItem(reader.GetString(1), reader.GetInt32(0).ToString(), reader.GetInt32(0) == selectedId));
            ViewBag.Categories = categories;
        }
    }
}
