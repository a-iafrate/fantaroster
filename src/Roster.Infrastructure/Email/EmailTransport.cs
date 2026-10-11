using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Roster.Infrastructure.Email;

/// <summary>
/// Sends a plain-text email through Azure Communication Services; without a connection string (local development) the
/// message is written to the log instead.
/// </summary>
public sealed partial class EmailTransport(IConfiguration configuration, ILogger<EmailTransport> logger)
{
    public async Task SendAsync(string toEmail, string subject, string textBody, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);

        var connectionString = configuration["AzureCommunicationServices:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            LogNoEmailConfiguration(logger, toEmail);
            LogDebugEmail(logger, toEmail, subject, textBody);
            return;
        }

        LogSendingAcs(logger, toEmail);
        var senderAddress = configuration["AzureCommunicationServices:SenderAddress"] ?? "donotreply@fantaroster.com";
        var message = new EmailMessage(senderAddress, toEmail, new EmailContent(subject) { PlainText = textBody });
        await new EmailClient(connectionString).SendAsync(Azure.WaitUntil.Completed, message, cancellationToken);
    }

    [LoggerMessage(LogLevel.Information, "=== EMAIL (DEBUG) ===\nTo: {ToEmail}\nSubject: {Subject}\nBody: {Body}\n=====================")]
    private static partial void LogDebugEmail(ILogger logger, string toEmail, string subject, string body);

    [LoggerMessage(LogLevel.Information, "Sending email to {ToEmail} via Azure Communication Services")]
    private static partial void LogSendingAcs(ILogger logger, string toEmail);

    [LoggerMessage(LogLevel.Warning, "No email configuration found. Email to {ToEmail} was not sent.")]
    private static partial void LogNoEmailConfiguration(ILogger logger, string toEmail);
}
