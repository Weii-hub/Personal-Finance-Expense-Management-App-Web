namespace PersonalFinanceApp.Models
{
    public class HomeDashboardViewModel
    {
        public decimal TotalBalance { get; set; }
        public int TotalWallets { get; set; }
        public int TotalTransactions { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpense { get; set; }
        public List<Transaction> RecentTransactions { get; set; } = new();
    }
}
