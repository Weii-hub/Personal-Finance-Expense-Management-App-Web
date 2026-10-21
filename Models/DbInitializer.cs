using Microsoft.EntityFrameworkCore;

namespace PersonalFinanceApp.Models
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Tự động kiểm tra và thêm dữ liệu nếu DB còn trống
            if (!context.Wallets.Any())
            {
                var wallets = new List<Wallet>
                {
                    new Wallet { Name = "Ví Tiền Mặt", InitialBalance = 2500000, Icon = "fa-wallet" },
                    new Wallet { Name = "Ví Điện Tử MoMo", InitialBalance = 1800000, Icon = "fa-mobile-screen" },
                    new Wallet { Name = "Thẻ Ngân Hàng Techcombank", InitialBalance = 15000000, Icon = "fa-building-columns" }
                };
                context.Wallets.AddRange(wallets);
                context.SaveChanges();
            }

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    // Chi tiêu (Type = 0)
                    new Category { Name = "Ăn uống", Type = 0, Icon = "fa-utensils", Color = "#ff6b81" },
                    new Category { Name = "Xăng xe & Đi lại", Type = 0, Icon = "fa-motorcycle", Color = "#f59f00" },
                    new Category { Name = "Mua sắm cá nhân", Type = 0, Icon = "fa-bag-shopping", Color = "#9775fa" },
                    new Category { Name = "Học tập & Giáo trình", Type = 0, Icon = "fa-book", Color = "#4dabf7" },
                    new Category { Name = "Tiền trọ & Điện nước", Type = 0, Icon = "fa-house", Color = "#ff922b" },
                    new Category { Name = "Cà phê & Giải trí", Type = 0, Icon = "fa-mug-hot", Color = "#20c997" },
                    // Thu nhập (Type = 1)
                    new Category { Name = "Tiền lương part-time", Type = 1, Icon = "fa-money-bill-wave", Color = "#20c997" },
                    new Category { Name = "Dự án Freelance", Type = 1, Icon = "fa-laptop-code", Color = "#12b886" },
                    new Category { Name = "Ba mẹ chu cấp", Type = 1, Icon = "fa-heart", Color = "#e64980" }
                };
                context.Categories.AddRange(categories);
                context.SaveChanges();
            }

            // Nếu chưa có giao dịch, tạo một số giao dịch mẫu rải rác
            if (!context.Transactions.Any())
            {
                var wallets = context.Wallets.ToList();
                var categories = context.Categories.ToList();

                var cashWallet = wallets.FirstOrDefault(w => w.Name.Contains("Tiền Mặt")) ?? wallets.First();
                var momoWallet = wallets.FirstOrDefault(w => w.Name.Contains("MoMo")) ?? wallets.First();
                var bankWallet = wallets.FirstOrDefault(w => w.Name.Contains("Techcombank")) ?? wallets.Last();

                var foodCat = categories.FirstOrDefault(c => c.Name == "Ăn uống") ?? categories.First();
                var travelCat = categories.FirstOrDefault(c => c.Name.Contains("Xăng")) ?? categories.First();
                var salaryCat = categories.FirstOrDefault(c => c.Name.Contains("lương")) ?? categories.First();
                var cafeCat = categories.FirstOrDefault(c => c.Name.Contains("Cà phê")) ?? categories.First();
                var rentCat = categories.FirstOrDefault(c => c.Name.Contains("trọ")) ?? categories.First();
                var parentCat = categories.FirstOrDefault(c => c.Name.Contains("chu cấp")) ?? categories.First();

                var now = DateTime.UtcNow;

                var sampleTransactions = new List<Transaction>
                {
                    // Tháng hiện tại
                    new Transaction { Amount = 4500000, TransactionDate = now.AddDays(-2), Note = "Nhận lương làm thêm tháng này", WalletId = bankWallet.Id, CategoryId = salaryCat.Id },
                    new Transaction { Amount = 1500000, TransactionDate = now.AddDays(-3), Note = "Ba mẹ gửi tiền ăn tháng", WalletId = bankWallet.Id, CategoryId = parentCat.Id },
                    new Transaction { Amount = 65000, TransactionDate = now.AddHours(-5), Note = "Ăn trưa cơm tấm sườn trứng", WalletId = cashWallet.Id, CategoryId = foodCat.Id },
                    new Transaction { Amount = 50000, TransactionDate = now.AddDays(-1), Note = "Đổ xăng xe máy", WalletId = cashWallet.Id, CategoryId = travelCat.Id },
                    new Transaction { Amount = 45000, TransactionDate = now.AddDays(-1), Note = "Cà phê trà sữa với bạn", WalletId = momoWallet.Id, CategoryId = cafeCat.Id },
                    new Transaction { Amount = 2000000, TransactionDate = now.AddDays(-4), Note = "Thanh toán tiền trọ tháng", WalletId = bankWallet.Id, CategoryId = rentCat.Id },
                    new Transaction { Amount = 85000, TransactionDate = now.AddDays(-5), Note = "Lẩu gà lá é ăn tối", WalletId = momoWallet.Id, CategoryId = foodCat.Id },
                    // Tháng trước
                    new Transaction { Amount = 4200000, TransactionDate = now.AddMonths(-1).AddDays(5), Note = "Lương tháng trước", WalletId = bankWallet.Id, CategoryId = salaryCat.Id },
                    new Transaction { Amount = 1900000, TransactionDate = now.AddMonths(-1).AddDays(6), Note = "Tiền trọ tháng trước", WalletId = bankWallet.Id, CategoryId = rentCat.Id },
                    new Transaction { Amount = 120000, TransactionDate = now.AddMonths(-1).AddDays(8), Note = "Mua sách tham khảo", WalletId = cashWallet.Id, CategoryId = foodCat.Id },
                    // 2 Tháng trước
                    new Transaction { Amount = 4000000, TransactionDate = now.AddMonths(-2).AddDays(2), Note = "Lương tháng kì trước", WalletId = bankWallet.Id, CategoryId = salaryCat.Id },
                    new Transaction { Amount = 1850000, TransactionDate = now.AddMonths(-2).AddDays(4), Note = "Tiền trọ", WalletId = bankWallet.Id, CategoryId = rentCat.Id }
                };

                context.Transactions.AddRange(sampleTransactions);
                context.SaveChanges();
            }
        }
    }
}
