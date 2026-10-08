using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roster.Infrastructure.Data;

namespace Roster.Infrastructure.Email;

public sealed partial class IdentityEmailSender : IEmailSender<OrganizerUser>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityEmailSender> _logger;

    public IdentityEmailSender(IConfiguration configuration, ILogger<IdentityEmailSender> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendConfirmationLinkAsync(OrganizerUser user, string email, string confirmationLink)
    {
        await SendEmailAsync(email, "Conferma la tua email", $"Clicca qui per confermare il tuo account: {confirmationLink}");
    }

    public async Task SendPasswordResetLinkAsync(OrganizerUser user, string email, string resetLink)
    {
        await SendEmailAsync(email, "Accedi a FantaRoster", $"Clicca qui per accedere in modo sicuro: {resetLink}");
    }

    public async Task SendPasswordResetCodeAsync(OrganizerUser user, string email, string resetCode)
    {
        await SendEmailAsync(email, "Codice di accesso FantaRoster", $"Il tuo codice di accesso è: {resetCode}");
    }

    private async Task SendEmailAsync(string toEmail, string subject, string textBody)
    {
        var smtpHost = _configuration["Smtp:Host"];
        if (!string.IsNullOrEmpty(smtpHost))
        {
            var smtpPort = _configuration.GetValue<int>("Smtp:Port", 1025);
            LogSendingSmtp(_logger, toEmail, smtpHost, smtpPort);

#pragma warning disable SYSLIB0014 // Type or member is obsolete
            using var client = new SmtpClient(smtpHost, smtpPort);
            using var message = new MailMessage("noreply@fantaroster.local", toEmail, subject, textBody);
            await client.SendMailAsync(message);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
        }
        else
        {
            var acsConnectionString = _configuration["AzureCommunicationServices:ConnectionString"];
            if (!string.IsNullOrEmpty(acsConnectionString))
            {
                LogSendingAcs(_logger, toEmail);
                var emailClient = new EmailClient(acsConnectionString);
                var senderAddress = _configuration["AzureCommunicationServices:SenderAddress"] ?? "donotreply@fantaroster.com";
                
                var emailMessage = new EmailMessage(
                    senderAddress: senderAddress,
                    recipientAddress: toEmail,
                    content: new EmailContent(subject)
                    {
                        PlainText = textBody
                    });
                    
                await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
            }
            else
            {
                LogNoEmailConfiguration(_logger, toEmail);
            }
        }
    }

    [LoggerMessage(LogLevel.Information, "Sending email to {ToEmail} via SMTP {SmtpHost}:{SmtpPort}")]
    private static partial void LogSendingSmtp(ILogger logger, string toEmail, string smtpHost, int smtpPort);

    [LoggerMessage(LogLevel.Information, "Sending email to {ToEmail} via Azure Communication Services")]
    private static partial void LogSendingAcs(ILogger logger, string toEmail);

    [LoggerMessage(LogLevel.Warning, "No email configuration found. Email to {ToEmail} was not sent.")]
    private static partial void LogNoEmailConfiguration(ILogger logger, string toEmail);
}
