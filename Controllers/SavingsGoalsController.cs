using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    [Authorize]
    public class SavingsGoalsController : Controller
    {
        private readonly string _connectionString;

        public SavingsGoalsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Không tìm thấy ConnectionStrings:DefaultConnection.");
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Không xác định được người dùng hiện tại.");

        public async Task<IActionResult> Index()
        {
            var goals = new List<SavingsGoal>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT "Id", "UserId", "Name", "TargetAmount", "CurrentAmount", "TargetDate", "CreatedAt"
                FROM "Goals"
                WHERE "UserId" = @userId
                ORDER BY "CreatedAt" DESC, "Id" DESC;
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", CurrentUserId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                goals.Add(ReadGoal(reader));

            return View(goals);
        }

        [HttpGet]
        public IActionResult Create() => View(new SavingsGoal { TargetDate = DateTime.Today.AddMonths(1) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SavingsGoal goal)
        {
            goal.Name = (goal.Name ?? string.Empty).Trim();
            goal.CurrentAmount = 0;

            if (!ModelState.IsValid)
                return View(goal);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                INSERT INTO "Goals" ("UserId", "Name", "TargetAmount", "CurrentAmount", "TargetDate")
                VALUES (@userId, @name, @targetAmount, 0, @targetDate);
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", CurrentUserId);
            command.Parameters.AddWithValue("name", goal.Name);
            command.Parameters.AddWithValue("targetAmount", goal.TargetAmount);
            command.Parameters.AddWithValue("targetDate", (object?)goal.TargetDate?.Date ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();

            TempData["SuccessMessage"] = "Đã tạo mục tiêu tiết kiệm thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var goal = await FindGoalAsync(id);
            return goal == null ? NotFound() : View(goal);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var goal = await FindGoalAsync(id);
            return goal == null ? NotFound() : View(goal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SavingsGoal input)
        {
            if (id != input.Id)
                return NotFound();

            input.Name = (input.Name ?? string.Empty).Trim();
            ModelState.Remove(nameof(input.UserId));
            ModelState.Remove(nameof(input.CurrentAmount));
            ModelState.Remove(nameof(input.CreatedAt));

            if (!ModelState.IsValid)
                return View(input);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                UPDATE "Goals"
                SET "Name" = @name,
                    "TargetAmount" = @targetAmount,
                    "TargetDate" = @targetDate
                WHERE "Id" = @id AND "UserId" = @userId;
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("name", input.Name);
            command.Parameters.AddWithValue("targetAmount", input.TargetAmount);
            command.Parameters.AddWithValue("targetDate", (object?)input.TargetDate?.Date ?? DBNull.Value);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", CurrentUserId);

            if (await command.ExecuteNonQueryAsync() == 0)
                return NotFound();

            TempData["SuccessMessage"] = "Đã cập nhật mục tiêu tiết kiệm!";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFunds(int id, decimal amount)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền nạp thêm phải lớn hơn 0.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Cập nhật nguyên tử để tránh ghi đè khi có nhiều yêu cầu cùng lúc.
            const string sql = """
                UPDATE "Goals"
                SET "CurrentAmount" = "CurrentAmount" + @amount
                WHERE "Id" = @id AND "UserId" = @userId;
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("amount", amount);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", CurrentUserId);

            if (await command.ExecuteNonQueryAsync() == 0)
                return NotFound();

            TempData["SuccessMessage"] = "Đã thêm tiền vào mục tiêu tiết kiệm!";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                DELETE FROM "Goals"
                WHERE "Id" = @id AND "UserId" = @userId;
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", CurrentUserId);

            if (await command.ExecuteNonQueryAsync() == 0)
                return NotFound();

            TempData["SuccessMessage"] = "Đã xóa mục tiêu tiết kiệm.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<SavingsGoal?> FindGoalAsync(int id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                SELECT "Id", "UserId", "Name", "TargetAmount", "CurrentAmount", "TargetDate", "CreatedAt"
                FROM "Goals"
                WHERE "Id" = @id AND "UserId" = @userId;
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", CurrentUserId);

            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadGoal(reader) : null;
        }

        private static SavingsGoal ReadGoal(NpgsqlDataReader reader)
        {
            return new SavingsGoal
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetString(1),
                Name = reader.GetString(2),
                TargetAmount = reader.GetDecimal(3),
                CurrentAmount = reader.GetDecimal(4),
                TargetDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                CreatedAt = reader.GetDateTime(6)
            };
        }
    }
}
