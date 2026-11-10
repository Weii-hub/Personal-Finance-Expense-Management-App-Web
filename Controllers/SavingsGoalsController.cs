using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    [Authorize]
    public class SavingsGoalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public SavingsGoalsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
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
            if (goal == null)
                return NotFound();

            // Nạp danh sách ví của tài khoản để chọn trích/rút tiền
            var wallets = await _context.Wallets
                .Where(w => w.UserId == CurrentUserId)
                .OrderBy(w => w.Name)
                .ToListAsync();

            ViewBag.Wallets = wallets;
            return View(goal);
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

        // POST: Nạp tiền vào mục tiêu tiết kiệm bằng cách trích từ một ví nguồn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFunds(int id, int walletId, decimal amount)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền nạp thêm phải lớn hơn 0 đ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var goal = await FindGoalAsync(id);
            if (goal == null)
                return NotFound();

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == walletId && w.UserId == CurrentUserId);
            if (wallet == null)
            {
                TempData["ErrorMessage"] = "Ví nguồn không tồn tại hoặc không thuộc quyền sở hữu của bạn.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Kiểm tra số dư ví nguồn có đủ tiền để trích nạp không
            if (wallet.InitialBalance < amount)
            {
                TempData["ErrorMessage"] = $"Ví '{wallet.Name}' không đủ số dư để nạp (Số dư khả dụng: {wallet.InitialBalance:N0} đ).";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Đảm bảo có danh mục "Tiết kiệm & Tích lũy" (Type = 0: Chi tiêu / Trích tiền)
            var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Tiết kiệm & Tích lũy" && c.Type == 0);
            if (cat == null)
            {
                cat = new Category
                {
                    Name = "Tiết kiệm & Tích lũy",
                    Type = 0,
                    Icon = "fa-bullseye",
                    Color = "#0d6efd"
                };
                _context.Categories.Add(cat);
                await _context.SaveChangesAsync();
            }

            // 1. Trừ tiền ở ví nguồn
            wallet.InitialBalance -= amount;

            // 2. Ghi nhận giao dịch trích tiền vào Sổ Giao Dịch
            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                CategoryId = cat.Id,
                Amount = amount,
                TransactionDate = DateTime.UtcNow,
                Note = $"[Tiết kiệm] Trích nạp mục tiêu: {goal.Name}",
                UserId = CurrentUserId
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // 3. Cập nhật tăng số dư tích lũy của mục tiêu
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = """
                    UPDATE "Goals"
                    SET "CurrentAmount" = "CurrentAmount" + @amount
                    WHERE "Id" = @id AND "UserId" = @userId;
                    """;

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("amount", amount);
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", CurrentUserId);
                await command.ExecuteNonQueryAsync();
            }

            TempData["SuccessMessage"] = $"Đã trích {amount:N0} đ từ ví '{wallet.Name}' nạp vào mục tiêu '{goal.Name}' thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Rút tiền từ mục tiêu tiết kiệm hoàn trả về ví
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawFunds(int id, int walletId, decimal amount)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền rút phải lớn hơn 0 đ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var goal = await FindGoalAsync(id);
            if (goal == null)
                return NotFound();

            if (goal.CurrentAmount < amount)
            {
                TempData["ErrorMessage"] = $"Số tiền muốn rút ({amount:N0} đ) vượt quá số tiền hiện có trong mục tiêu ({goal.CurrentAmount:N0} đ).";
                return RedirectToAction(nameof(Details), new { id });
            }

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == walletId && w.UserId == CurrentUserId);
            if (wallet == null)
            {
                TempData["ErrorMessage"] = "Ví nhận không tồn tại hoặc không thuộc quyền sở hữu của bạn.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Đảm bảo có danh mục "Thu hồi quỹ tiết kiệm" (Type = 1: Thu nhập / Hoàn tiền)
            var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Thu hồi quỹ tiết kiệm" && c.Type == 1);
            if (cat == null)
            {
                cat = new Category
                {
                    Name = "Thu hồi quỹ tiết kiệm",
                    Type = 1,
                    Icon = "fa-piggy-bank",
                    Color = "#20c997"
                };
                _context.Categories.Add(cat);
                await _context.SaveChangesAsync();
            }

            // 1. Cộng tiền hoàn trả vào ví nhận
            wallet.InitialBalance += amount;

            // 2. Ghi nhận giao dịch thu hoàn tiền vào Sổ Giao Dịch
            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                CategoryId = cat.Id,
                Amount = amount,
                TransactionDate = DateTime.UtcNow,
                Note = $"[Hoàn quỹ] Rút từ mục tiêu: {goal.Name}",
                UserId = CurrentUserId
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // 3. Cập nhật giảm số dư tích lũy của mục tiêu
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = """
                    UPDATE "Goals"
                    SET "CurrentAmount" = "CurrentAmount" - @amount
                    WHERE "Id" = @id AND "UserId" = @userId;
                    """;

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("amount", amount);
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", CurrentUserId);
                await command.ExecuteNonQueryAsync();
            }

            TempData["SuccessMessage"] = $"Đã rút {amount:N0} đ từ mục tiêu '{goal.Name}' chuyển về ví '{wallet.Name}' thành công!";
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
