using Microsoft.EntityFrameworkCore;

namespace PersonalFinanceApp.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Khai báo 3 bảng sẽ được tạo trong Database
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
    }
}
