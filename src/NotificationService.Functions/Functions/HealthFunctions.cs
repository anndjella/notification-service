using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Functions.Functions;

public sealed class HealthFunctions
{
    private readonly NotificationDbContext _db;

    public HealthFunctions(NotificationDbContext db)
    {
        _db = db;
    }

    [Function("HealthLive")]
    public HttpResponseData Live(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/live")]
        HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.WriteString("Healthy");
        return response;
    }

    [Function("HealthReady")]
    public async Task<HttpResponseData> Ready(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/ready")]
        HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            var response = request.CreateResponse(
                canConnect ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
            await response.WriteStringAsync(canConnect ? "Healthy" : "Unhealthy");
            return response;
        }
        catch (Exception)
        {
            var response = request.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await response.WriteStringAsync("Unhealthy");
            return response;
        }
    }
}
