using MailKit.Net.Smtp;
using MimeKit;

namespace WebApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var smtpClient = new SmtpClient())
            {
                await smtpClient.ConnectAsync(configuration["EmailSettings:SmtpServer"],
                                              int.Parse(configuration["EmailSettings:SmtpPort"]),
                                              true);

                smtpClient.Authenticate(configuration["EmailSettings:SmtpUsername"],
                                         configuration["EmailSettings:SmtpPassword"]);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Your Name", configuration["EmailSettings:SmtpUsername"]));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                message.Body = bodyBuilder.ToMessageBody();

                await smtpClient.SendAsync(message);

                await smtpClient.DisconnectAsync(true);
            }
        }
    }
}