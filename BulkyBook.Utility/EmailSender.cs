using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BulkyBook.Utility;

public class EmailSender : IEmailSender
{
    public string SendGridSecret { get; set; }

    public EmailSender(IConfiguration configuration)
    {
        SendGridSecret = configuration.GetValue<string>("SendGrid:SecretKey");
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var sendGridClient = new SendGridClient(SendGridSecret);
        var from = new EmailAddress("xolmirzayev.ogabek.2004@gmail.com", "Bulky Book");
        var to = new EmailAddress(email);
        var message = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);

        return sendGridClient.SendEmailAsync(message);
    }
}