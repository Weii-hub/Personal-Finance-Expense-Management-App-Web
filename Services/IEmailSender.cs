namespace PersonalFinanceApp.Services
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task<bool> SendOtpEmailAsync(string toEmail, string? recipientName, string otpCode, string purpose);
    }
}
