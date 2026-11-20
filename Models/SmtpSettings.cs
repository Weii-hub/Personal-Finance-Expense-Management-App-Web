namespace PersonalFinanceApp.Models
{
    public class SmtpSettings
    {
        public string Server { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string SenderEmail { get; set; } = "cashflow.app.project@gmail.com";
        public string SenderName { get; set; } = "CashFlow Support";
        public string Username { get; set; } = "cashflow.app.project@gmail.com";
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
