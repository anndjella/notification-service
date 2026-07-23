using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace NotificationService.Functions.Http;

public sealed class InternalApiKeyAuthorizer
{
    private readonly IConfiguration _configuration;

    public InternalApiKeyAuthorizer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsAuthorized(HttpRequestData request)
    {
        var expected = _configuration["InternalApiKey"];
        var supplied = request.Headers.TryGetValues("X-Internal-Api-Key", out var values)
            ? values.FirstOrDefault()
            : null;

        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(supplied));
    }
}
