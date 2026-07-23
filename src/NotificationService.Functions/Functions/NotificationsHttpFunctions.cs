using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NotificationService.Application.Notifications;
using NotificationService.Functions.Http;

namespace NotificationService.Functions.Functions;

public sealed class NotificationsHttpFunctions
{
    private readonly NotificationQueryService _queryService;
    private readonly InternalApiKeyAuthorizer _authorizer;
    private readonly TimeProvider _timeProvider;

    public NotificationsHttpFunctions(
        NotificationQueryService queryService,
        InternalApiKeyAuthorizer authorizer,
        TimeProvider timeProvider)
    {
        _queryService = queryService;
        _authorizer = authorizer;
        _timeProvider = timeProvider;
    }

    [Function("ListUserNotifications")]
    public async Task<HttpResponseData> ListAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notifications/users/{userId:int}")]
        HttpRequestData request,
        int userId,
        CancellationToken cancellationToken)
    {
        if (!_authorizer.IsAuthorized(request))
            return request.CreateResponse(HttpStatusCode.Unauthorized);

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            await _queryService.ListAsync(userId, cancellationToken),
            cancellationToken);
        return response;
    }

    [Function("CountUnreadUserNotifications")]
    public async Task<HttpResponseData> CountUnreadAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notifications/users/{userId:int}/unread-count")]
        HttpRequestData request,
        int userId,
        CancellationToken cancellationToken)
    {
        if (!_authorizer.IsAuthorized(request))
            return request.CreateResponse(HttpStatusCode.Unauthorized);

        var count = await _queryService.CountUnreadAsync(userId, cancellationToken);
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new UnreadNotificationCountResponse(count), cancellationToken);
        return response;
    }

    [Function("MarkUserNotificationAsRead")]
    public async Task<HttpResponseData> MarkAsReadAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "notifications/users/{userId:int}/{notificationId:guid}/read")]
        HttpRequestData request,
        int userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!_authorizer.IsAuthorized(request))
            return request.CreateResponse(HttpStatusCode.Unauthorized);

        var found = await _queryService.MarkAsReadAsync(
            notificationId,
            userId,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return request.CreateResponse(found ? HttpStatusCode.NoContent : HttpStatusCode.NotFound);
    }
}
