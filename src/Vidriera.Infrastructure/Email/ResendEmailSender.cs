using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Email;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendOptions _options;

    public ResendEmailSender(HttpClient httpClient, IOptions<ResendOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var request = new SendEmailRequest($"{_options.FromName} <{_options.FromEmail}>", toEmail, subject, htmlBody);

        var response = await _httpClient.PostAsJsonAsync("emails", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Resend devolvió {(int)response.StatusCode} {response.StatusCode}: {body}");
        }
    }

    private record SendEmailRequest(
        string From,
        string To,
        string Subject,
        [property: JsonPropertyName("html")] string Html);
}
