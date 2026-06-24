using Microsoft.Extensions.Logging;
using PeopleGrid.Application.Abstractions;

namespace PeopleGrid.Infrastructure.Email;

public sealed class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email placeholder: To={To}, Subject={Subject}", to, subject);
        return Task.CompletedTask;
    }
}
