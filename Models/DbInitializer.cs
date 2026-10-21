using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PersonalFinanceApp.Models
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 1. Khởi tạo các Vai trò (Roles)
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Khởi tạo Tài khoản Admin
            var adminEmail = "admin@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(adminUser, "Admin@2026");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // 3. Khởi tạo Tài khoản Người dùng Mon
            var monEmail = "mon@gmail.com";
            var monUser = await userManager.FindByEmailAsync(monEmail);
            if (monUser == null)
            {
                monUser = new ApplicationUser
                {
                    UserName = monEmail,
                    Email = monEmail,
                    FullName = "Mon",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(monUser, "Mon@2026");
                await userManager.AddToRoleAsync(monUser, "User");
            }

            // 4. Khởi tạo Danh mục mẫu (Categories) - dùng chung cho toàn hệ thống
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    // Chi tiêu (Type = 0)
                    new Category { Name = "Ăn uống", Type = 0, Icon = "fa-utensils", Color = "#ff6b81" },
                    new Category { Name = "Xăng xe & Đi lại", Type = 0, Icon = "fa-motorcycle", Color = "#f59f00" },
                    new Category { Name = "Mua sắm cá nhân", Type = 0, Icon = "fa-bag-shopping", Color = "#9775fa" },
                    new Category { Name = "Học tập & Sách vở", Type = 0, Icon = "fa-book", Color = "#4dabf7" },
                    new Category { Name = "Tiền trọ & Điện nước", Type = 0, Icon = "fa-house", Color = "#ff922b" },
                    new Category { Name = "Cà phê & Giải trí", Type = 0, Icon = "fa-mug-hot", Color = "#20c997" },
                    // Thu nhập (Type = 1)
                    new Category { Name = "Tiền lương part-time", Type = 1, Icon = "fa-money-bill-wave", Color = "#20c997" },
                    new Category { Name = "Dự án Freelance", Type = 1, Icon = "fa-laptop-code", Color = "#12b886" },
                    new Category { Name = "Ba mẹ chu cấp", Type = 1, Icon = "fa-heart", Color = "#e64980" }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // 5. Gắn các Ví mẫu cho tài khoản Mon (Issue #15)
            if (!context.Wallets.Any(w => w.UserId == monUser.Id))
            {
                var wallets = new List<Wallet>
                {
                    new Wallet { Name = "Ví Tiền Mặt", InitialBalance = 2500000, Icon = "fa-wallet", UserId = monUser.Id },
                    new Wallet { Name = "Ví Điện Tử MoMo", InitialBalance = 1800000, Icon = "fa-mobile-screen", UserId = monUser.Id },
                    new Wallet { Name = "Thẻ Ngân Hàng Techcombank", InitialBalance = 15000000, Icon = "fa-building-columns", UserId = monUser.Id }
                };
                context.Wallets.AddRange(wallets);
                await context.SaveChangesAsync();
            }

            // Cập nhật UserId cho các ví cũ nếu chưa có
            var legacyWallets = await context.Wallets.Where(w => w.UserId == null).ToListAsync();
            foreach (var lw in legacyWallets)
            {
                lw.UserId = monUser.Id;
            }
            if (legacyWallets.Any())
            {
                await context.SaveChangesAsync();
            }

            // 6. Gắn các Giao dịch mẫu cho tài khoản Mon (Issue #15)
            if (!context.Transactions.Any(t => t.UserId == monUser.Id))
            {
                var monWallets = await context.Wallets.Where(w => w.UserId == monUser.Id).ToListAsync();
                var categories = await context.Categories.ToListAsync();

                var cashWallet = monWallets.FirstOrDefault(w => w.Name.Contains("Tiền Mặt")) ?? monWallets.First();
                var momoWallet = monWallets.FirstOrDefault(w => w.Name.Contains("MoMo")) ?? monWallets.First();
                var bankWallet = monWallets.FirstOrDefault(w => w.Name.Contains("Techcombank")) ?? monWallets.Last();

                var foodCat = categories.FirstOrDefault(c => c.Name == "Ăn uống") ?? categories.First();
                var travelCat = categories.FirstOrDefault(c => c.Name.Contains("Xăng")) ?? categories.First();
                var salaryCat = categories.FirstOrDefault(c => c.Name.Contains("lương")) ?? categories.First();
                var cafeCat = categories.FirstOrDefault(c => c.Name.Contains("Cà phê")) ?? categories.First();
                var rentCat = categories.FirstOrDefault(c => c.Name.Contains("trọ")) ?? categories.First();
                var parentCat = categories.FirstOrDefault(c => c.Name.Contains("chu cấp")) ?? categories.First();

                var now = DateTime.UtcNow;

                var sampleTransactions = new List<Transaction>
                {
                    new Transaction { Amount = 4500000, TransactionDate = now.AddDays(-2), Note = "Nhận lương làm thêm tháng này", WalletId = bankWallet.Id, CategoryId = salaryCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 1500000, TransactionDate = now.AddDays(-3), Note = "Ba mẹ gửi tiền ăn tháng", WalletId = bankWallet.Id, CategoryId = parentCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 65000, TransactionDate = now.AddHours(-5), Note = "Ăn trưa cơm tấm sườn trứng", WalletId = cashWallet.Id, CategoryId = foodCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 50000, TransactionDate = now.AddDays(-1), Note = "Đổ xăng xe máy", WalletId = cashWallet.Id, CategoryId = travelCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 45000, TransactionDate = now.AddDays(-1), Note = "Cà phê trà sữa với bạn", WalletId = momoWallet.Id, CategoryId = cafeCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 2000000, TransactionDate = now.AddDays(-4), Note = "Thanh toán tiền trọ tháng", WalletId = bankWallet.Id, CategoryId = rentCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 85000, TransactionDate = now.AddDays(-5), Note = "Lẩu gà lá é ăn tối", WalletId = momoWallet.Id, CategoryId = foodCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 4200000, TransactionDate = now.AddMonths(-1).AddDays(5), Note = "Lương tháng trước", WalletId = bankWallet.Id, CategoryId = salaryCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 1900000, TransactionDate = now.AddMonths(-1).AddDays(6), Note = "Tiền trọ tháng trước", WalletId = bankWallet.Id, CategoryId = rentCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 120000, TransactionDate = now.AddMonths(-1).AddDays(8), Note = "Mua sách tham khảo", WalletId = cashWallet.Id, CategoryId = foodCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 4000000, TransactionDate = now.AddMonths(-2).AddDays(2), Note = "Lương tháng kì trước", WalletId = bankWallet.Id, CategoryId = salaryCat.Id, UserId = monUser.Id },
                    new Transaction { Amount = 1850000, TransactionDate = now.AddMonths(-2).AddDays(4), Note = "Tiền trọ", WalletId = bankWallet.Id, CategoryId = rentCat.Id, UserId = monUser.Id }
                };

                context.Transactions.AddRange(sampleTransactions);
                await context.SaveChangesAsync();
            }

            // Cập nhật UserId cho các giao dịch cũ nếu chưa có
            var legacyTransactions = await context.Transactions.Where(t => t.UserId == null).ToListAsync();
            foreach (var lt in legacyTransactions)
            {
                lt.UserId = monUser.Id;
            }
            if (legacyTransactions.Any())
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
