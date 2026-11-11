using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _connectionString;

        public SavingsGoalsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
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
                SELECT "Id", "UserId", "Name", "TargetAmount", "CurrentAmount", "TargetDate", "CreatedAt", "GoalType"
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
        public IActionResult Create() => View(new SavingsGoal
        {
            GoalType = 0,
            TargetDate = DateTime.Today.AddMonths(1)
        });

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
                INSERT INTO "Goals" ("UserId", "Name", "TargetAmount", "CurrentAmount", "TargetDate", "GoalType")
                VALUES (@userId, @name, @targetAmount, 0, @targetDate, @goalType);
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("userId", CurrentUserId);
            command.Parameters.AddWithValue("name", goal.Name);
            command.Parameters.AddWithValue("targetAmount", goal.TargetAmount);
            command.Parameters.AddWithValue("targetDate", (object?)goal.TargetDate?.Date ?? DBNull.Value);
            command.Parameters.AddWithValue("goalType", goal.GoalType);
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

            var allWallets = await _context.Wallets
                .Where(w => w.UserId == CurrentUserId)
                .OrderBy(w => w.Name)
                .ToListAsync();

            // Phân loại nguồn ví nghiêm ngặt theo loại mục tiêu (Strict Wallet Type Lock):
            // - Nếu mục tiêu Tiền mặt (GoalType == 0): chỉ cho phép lấy/rút từ ví tiền mặt (WalletType == 0)
            // - Nếu mục tiêu Ngân hàng (GoalType == 1): chỉ cho phép lấy/rút từ ví ngân hàng (WalletType == 1 && IsSavingsAllowed == true)
            List<Wallet> allowedWallets;
            if (goal.GoalType == 0)
            {
                allowedWallets = allWallets.Where(w => w.WalletType == 0).ToList();
            }
            else
            {
                allowedWallets = allWallets.Where(w => w.WalletType == 1 && w.IsSavingsAllowed).ToList();
            }

            ViewBag.Wallets = allowedWallets;
            ViewBag.TotalWalletsCount = allWallets.Count;
            ViewBag.BankWalletsCount = allWallets.Count(w => w.WalletType == 1);
            ViewBag.BankWalletsWithoutPermissionCount = allWallets.Count(w => w.WalletType == 1 && !w.IsSavingsAllowed);

            var user = await _userManager.FindByIdAsync(CurrentUserId);
            ViewBag.HasPin = !string.IsNullOrEmpty(user?.SecurityPinHash);

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
                    "TargetDate" = @targetDate,
                    "GoalType" = @goalType
                WHERE "Id" = @id AND "UserId" = @userId;
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("name", input.Name);
            command.Parameters.AddWithValue("targetAmount", input.TargetAmount);
            command.Parameters.AddWithValue("targetDate", (object?)input.TargetDate?.Date ?? DBNull.Value);
            command.Parameters.AddWithValue("goalType", input.GoalType);
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
        public async Task<IActionResult> AddFunds(int id, int walletId, decimal amount, string? pin = null)
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

            // Kiểm tra quy tắc loại ví (Strict Type Match)
            if (goal.GoalType == 0 && wallet.WalletType != 0)
            {
                TempData["ErrorMessage"] = "Mục tiêu này là mục tiêu 'Tiền mặt', vui lòng chọn một chiếc ví tiền mặt để trích nạp!";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (goal.GoalType == 1 && wallet.WalletType != 1)
            {
                TempData["ErrorMessage"] = "Mục tiêu này là mục tiêu 'Điện tử / Ngân hàng', vui lòng chọn một ví ngân hàng để trích nạp!";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Nếu là ví ngân hàng: Kiểm tra quyền và bắt buộc xác thực Mã PIN 6 số
            if (wallet.WalletType == 1)
            {
                if (!wallet.IsSavingsAllowed)
                {
                    TempData["ErrorMessage"] = $"Ví '{wallet.Name}' hiện đang TẮT 'Quyền trích cho mục tiêu tiết kiệm'. Vui lòng bật quyền này trong Quản lý Ví trước khi trích nạp!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null || string.IsNullOrEmpty(user.SecurityPinHash))
                {
                    TempData["ErrorMessage"] = "Bạn chưa thiết lập Mã PIN bảo mật. Vui lòng vào Quản lý Ví để tạo mã PIN trước khi trích tiền ngân hàng!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (string.IsNullOrWhiteSpace(pin) || _userManager.PasswordHasher.VerifyHashedPassword(user, user.SecurityPinHash, pin) == PasswordVerificationResult.Failed)
                {
                    TempData["ErrorMessage"] = "🔒 Mã PIN bảo mật không chính xác! Giao dịch trích tiền từ tài khoản ngân hàng đã bị hủy bỏ để bảo vệ an toàn.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            // Kiểm tra số dư ví nguồn có đủ tiền để trích nạp không
            if (wallet.InitialBalance < amount)
            {
                TempData["ErrorMessage"] = $"Ví '{wallet.Name}' không đủ số dư để nạp (Số dư khả dụng: {wallet.InitialBalance:N0} đ).";
                return RedirectToAction(nameof(Details), new { id });
            }

            // 1. Trừ tiền ở ví nguồn
            wallet.InitialBalance -= amount;
            await _context.SaveChangesAsync();

            // 2. Cập nhật tăng số dư tích lũy của mục tiêu
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

            if (wallet.WalletType == 0)
            {
                TempData["SuccessMessage"] = $"Đã trích {amount:N0} đ từ ví '{wallet.Name}' nạp vào mục tiêu '{goal.Name}' thành công!";
                TempData["AdvisoryNotice"] = "💡 Lời khuyên thực tế: Hãy nhớ trích cất số tiền mặt này riêng ra (như bỏ heo đất hoặc phong bì) để tránh tiêu nhầm nhé!";
            }
            else
            {
                TempData["SuccessMessage"] = $"🔒 Xác thực PIN thành công! Đã trích {amount:N0} đ từ tài khoản '{wallet.Name}' nạp vào mục tiêu '{goal.Name}'. Số tiền này đã được bảo vệ trên hệ thống!";
            }

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

            // Kiểm tra khớp loại ví khi hoàn tiền
            if (goal.GoalType == 0 && wallet.WalletType != 0)
            {
                TempData["ErrorMessage"] = "Mục tiêu này là mục tiêu 'Tiền mặt', vui lòng chọn một chiếc ví tiền mặt để nhận lại tiền!";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (goal.GoalType == 1 && wallet.WalletType != 1)
            {
                TempData["ErrorMessage"] = "Mục tiêu này là mục tiêu 'Điện tử / Ngân hàng', vui lòng chọn ví ngân hàng để nhận lại tiền!";
                return RedirectToAction(nameof(Details), new { id });
            }

            // 1. Cộng tiền hoàn trả vào ví nhận
            wallet.InitialBalance += amount;
            await _context.SaveChangesAsync();

            // 2. Cập nhật giảm số dư tích lũy của mục tiêu
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

            if (wallet.WalletType == 0)
            {
                TempData["SuccessMessage"] = $"Đã rút {amount:N0} đ từ mục tiêu '{goal.Name}' chuyển về ví '{wallet.Name}' thành công! Bạn có thể lấy tiền từ heo đất / phong bì bỏ lại vào ví.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã rút {amount:N0} đ từ mục tiêu '{goal.Name}' hoàn trả về tài khoản '{wallet.Name}' thành công!";
            }

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
                SELECT "Id", "UserId", "Name", "TargetAmount", "CurrentAmount", "TargetDate", "CreatedAt", "GoalType"
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
                CreatedAt = reader.GetDateTime(6),
                GoalType = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetInt32(7) : 0
            };
        }
    }
}
