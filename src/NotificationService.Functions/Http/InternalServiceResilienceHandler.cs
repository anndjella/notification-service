using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NotificationService.Functions.Http;

public sealed class InternalServiceResilienceHandler : DelegatingHandler
{
    private readonly ILogger<InternalServiceResilienceHandler> _logger;
    private readonly int _retryCount;
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _attemptTimeout;

    public InternalServiceResilienceHandler(
        IConfiguration configuration,
        ILogger<InternalServiceResilienceHandler> logger)
    {
        _logger = logger;
        _retryCount = Math.Max(0, configuration.GetValue("InternalHttpRetryCount", 2));
        _baseDelay = TimeSpan.FromMilliseconds(Math.Max(
            0,
            configuration.GetValue("InternalHttpRetryBaseDelayMilliseconds", 200)));
        _attemptTimeout = TimeSpan.FromSeconds(Math.Max(
            1,
            configuration.GetValue("InternalHttpTimeoutSeconds", 10)));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var content = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentHeaders = request.Content?.Headers
            .ToDictionary(header => header.Key, header => header.Value.ToArray());
        var maxAttempts = IsIdempotent(request.Method) ? _retryCount + 1 : 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var attemptRequest = Clone(request, content, contentHeaders);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_attemptTimeout);

            try
            {
                var response = await base.SendAsync(attemptRequest, timeout.Token);
                if (!IsTransient(response.StatusCode) || attempt == maxAttempts)
                    return response;

                _logger.LogWarning(
                    "Internal HTTP request {Method} {Uri} returned {StatusCode}. Retry {Attempt}/{RetryCount}.",
                    request.Method,
                    request.RequestUri,
                    (int)response.StatusCode,
                    attempt,
                    _retryCount);
                response.Dispose();
            }
            catch (HttpRequestException exception) when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Internal HTTP request {Method} {Uri} failed. Retry {Attempt}/{RetryCount}.",
                    request.Method,
                    request.RequestUri,
                    attempt,
                    _retryCount);
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                _logger.LogWarning(
                    "Internal HTTP request {Method} {Uri} exceeded the {TimeoutSeconds}s timeout. Retry {Attempt}/{RetryCount}.",
                    request.Method,
                    request.RequestUri,
                    _attemptTimeout.TotalSeconds,
                    attempt,
                    _retryCount);
            }

            await Task.Delay(Backoff(attempt), cancellationToken);
        }

        throw new InvalidOperationException("The internal HTTP retry loop completed unexpectedly.");
    }

    private TimeSpan Backoff(int attempt)
        => TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

    private static bool IsIdempotent(HttpMethod method)
        => method == HttpMethod.Get ||
           method == HttpMethod.Head ||
           method == HttpMethod.Put ||
           method == HttpMethod.Delete ||
           method == HttpMethod.Options;

    private static bool IsTransient(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
           (int)statusCode >= 500;

    private static HttpRequestMessage Clone(
        HttpRequestMessage source,
        byte[]? content,
        IReadOnlyDictionary<string, string[]>? contentHeaders)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy
        };

        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (content is not null)
        {
            clone.Content = new ByteArrayContent(content);
            if (contentHeaders is not null)
            {
                foreach (var header in contentHeaders)
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
