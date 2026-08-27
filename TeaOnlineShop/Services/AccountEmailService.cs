using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using System.Text;

namespace TeaOnlineShop.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "SmartTeaShop";
}

public interface IAccountEmailService
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public sealed class SmtpAccountEmailService : IAccountEmailService
{
    private readonly EmailOptions _options;

    public SmtpAccountEmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public bool IsConfigured => _options.Enabled &&
                                !string.IsNullOrWhiteSpace(_options.Host) &&
                                !string.IsNullOrWhiteSpace(_options.FromAddress);

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Transactional email is not configured.");

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.UserName, _options.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}

/// <summary>
/// Development-only mail sink. Messages stay outside wwwroot so confirmation and
/// reset tokens are never exposed by the web server. Production never registers it.
/// </summary>
public sealed class DevelopmentFileAccountEmailService : IAccountEmailService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DevelopmentFileAccountEmailService> _logger;

    public DevelopmentFileAccountEmailService(
        IWebHostEnvironment environment,
        ILogger<DevelopmentFileAccountEmailService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public bool IsConfigured => _environment.IsDevelopment();

    public async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("The development mail sink cannot run outside Development.");

        var outbox = Path.Combine(_environment.ContentRootPath, "App_Data", "development-mail");
        Directory.CreateDirectory(outbox);

        var safeRecipient = string.Concat(recipient.Select(c =>
            char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
        var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}_{safeRecipient}_{Guid.NewGuid():N}.html";
        var path = Path.Combine(outbox, fileName);
        var document = $"""
                        <!doctype html>
                        <html lang="en"><head><meta charset="utf-8"><title>{WebUtility.HtmlEncode(subject)}</title></head>
                        <body>
                        <p><strong>Development mail — not delivered externally</strong></p>
                        <p><strong>To:</strong> {WebUtility.HtmlEncode(recipient)}<br>
                        <strong>Subject:</strong> {WebUtility.HtmlEncode(subject)}</p>
                        <hr>
                        {htmlBody}
                        </body></html>
                        """;

        await File.WriteAllTextAsync(path, document, Encoding.UTF8, cancellationToken);
        _logger.LogInformation("Development account email written to {MailFile}", path);
    }
}
