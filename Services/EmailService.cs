using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Services
{
    public class EmailService : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(_settings.Password) || string.IsNullOrWhiteSpace(_settings.Username))
            {
                _logger.LogWarning("⚠️ SMTP chưa được cấu hình Password hoặc Username! Không thể gửi email thực tế đến {Email}.", toEmail);
                return false;
            }

            try
            {
                using var client = new SmtpClient(_settings.Server, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000 // 15 giây
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                await client.SendMailAsync(mail);
                _logger.LogInformation("✅ [SMTP SUCCESS] Đã gửi email thành công tới {Email} với tiêu đề: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SMTP ERROR] Thất bại khi gửi email tới {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string? recipientName, string otpCode, string purpose)
        {
            var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Bạn" : recipientName;
            var subject = $"[CashFlow] Mã xác thực OTP {purpose}: {otpCode}";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f8fafc;
            margin: 0;
            padding: 24px;
            color: #334155;
        }}
        .container {{
            max-width: 520px;
            margin: 0 auto;
            background: #ffffff;
            border-radius: 18px;
            padding: 32px;
            box-shadow: 0 4px 16px rgba(0,0,0,0.06);
            border: 1px solid #e2e8f0;
        }}
        .header {{
            text-align: center;
            border-bottom: 2px dashed #f1f5f9;
            padding-bottom: 20px;
            margin-bottom: 24px;
        }}
        .logo {{
            font-size: 32px;
            margin-bottom: 6px;
        }}
        .brand-title {{
            font-size: 22px;
            font-weight: 800;
            color: #4f46e5;
            margin: 0;
        }}
        .brand-sub {{
            font-size: 13px;
            color: #64748b;
            margin-top: 4px;
        }}
        .otp-box {{
            text-align: center;
            margin: 28px 0;
            padding: 20px;
            background: linear-gradient(135deg, #eef2ff 0%, #e0e7ff 100%);
            border: 2px dashed #6366f1;
            border-radius: 14px;
        }}
        .otp-code {{
            font-family: 'Courier New', Courier, monospace;
            font-size: 36px;
            font-weight: 800;
            letter-spacing: 10px;
            color: #3730a3;
            margin: 8px 0;
        }}
        .badge {{
            display: inline-block;
            background: #4f46e5;
            color: white;
            font-size: 11px;
            font-weight: bold;
            padding: 4px 12px;
            border-radius: 20px;
            text-transform: uppercase;
        }}
        .info-text {{
            font-size: 14px;
            line-height: 1.6;
            color: #475569;
        }}
        .warning {{
            background: #fffbeb;
            border-left: 4px solid #f59e0b;
            padding: 12px 16px;
            border-radius: 8px;
            font-size: 12px;
            color: #92400e;
            margin-top: 20px;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #f1f5f9;
            font-size: 12px;
            color: #94a3b8;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🧸✨</div>
            <h1 class='brand-title'>CashFlow App</h1>
            <p class='brand-sub'>Hệ thống Quản lý Tài chính & Chi tiêu Cá nhân</p>
        </div>

        <p class='info-text'>
            Xin chào <strong>{displayName}</strong>,
        </p>
        <p class='info-text'>
            Bạn (hoặc ai đó) vừa gửi yêu cầu <strong>{purpose}</strong> trên hệ thống CashFlow. Vui lòng sử dụng mã xác thực bảo mật (OTP) dưới đây:
        </p>

        <div class='otp-box'>
            <span class='badge'>MÃ XÁC THỰC BẢO MẬT</span>
            <div class='otp-code'>{otpCode}</div>
            <div style='font-size: 12px; color: #4338ca;'>Mã này có hiệu lực trong vòng <strong>5 phút</strong></div>
        </div>

        <div class='warning'>
            ⚠️ <strong>Lưu ý bảo mật:</strong> Không cung cấp mã OTP này cho bất kỳ ai, kể cả nhân viên hỗ trợ CashFlow. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc đổi mật khẩu ngay.
        </div>

        <div class='footer'>
            <p style='margin: 0;'>© {DateTime.UtcNow.Year} CashFlow Project - HUFLIT CNPMNC</p>
            <p style='margin: 4px 0 0 0;'>Email này được gửi tự động, vui lòng không trả lời trực tiếp.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}
