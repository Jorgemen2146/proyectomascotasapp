# DogPlatform vaccination notifications

## Current development architecture

The Notifications service uses Quartz.NET for its daily schedule, Health over HTTP as
the sole source of vaccination status, SQL Server for durable notification storage and
raw ASP.NET Core WebSockets for best-effort realtime delivery. REST remains the recovery
path when a mobile client was disconnected or realtime delivery failed.

## Realtime WebSocket contract

- Direct API endpoint: `ws://localhost:5109/ws/notifications`
- Gateway endpoint: `ws://localhost:5101/ws/notifications`
- Authentication: send the JWT in `Authorization: Bearer <token>` when the client supports
  headers. Mobile/proxy clients may instead use `?access_token=<token>`. The header wins
  when both are present, and the query token is redacted from request/error logs.
- One authenticated user can keep multiple concurrent sockets. The user id always comes
  from the validated JWT `sub`/name-identifier claim and never from query or payload data.
- Server message envelope:

```json
{
  "event": "notificationReceived",
  "data": {
    "notificationId": "00000000-0000-0000-0000-000000000000",
    "type": "VaccinationDueSoon",
    "title": "Title",
    "message": "Message",
    "petId": "00000000-0000-0000-0000-000000000000",
    "vaccineId": 1,
    "status": "Created",
    "isRead": false,
    "readAtUtc": null,
    "createdAtUtc": "2026-08-25T00:00:00Z",
    "metadataJson": null
  }
}
```

The server receive loop only detects close/disconnect frames; it does not accept client
commands. Persisting the notification happens first, so failed WebSocket delivery never
rolls back storage and the existing REST endpoints remain the recovery mechanism.

## IIS prerequisite (operator instructions only)

Publishing with the Web SDK generates the normal ASP.NET Core Module V2 `web.config`; no
custom WebSocket handler is required. The IIS WebSocket Protocol Windows feature must be
enabled on both the Gateway and Notifications hosts. Run these commands only from an
elevated administrator shell during deployment:

```powershell
Get-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets -All
```

After enabling it, recycle the affected application pools. These commands are documented
here but are not executed by the repository or by this change.

The internal Health and Pets endpoints require `X-DogPlatform-Internal-Key`. The value is
configuration supplied through `InternalServices__ApiKey`; it is never committed. This is
the minimum development service-authentication mechanism because Identity does not yet
provide OAuth2 client credentials. Replace it before exposing internal routes outside the
trusted service network.

Daily idempotency is enforced by the unique SQL index on `DeduplicationKey`:

`vaccination:{userId}:{petId}:{vaccineId}:{type}:{yyyy-MM-dd}`

## Future AWS evolution

- Schedule: EventBridge Scheduler with an ECS scheduled task, or keep Quartz in the service.
- Realtime: replace `INotificationRealtimePublisher` with API Gateway WebSocket or AppSync.
- Persistence: SQL Server/RDS can remain the durable notification store.
- Push: add a dedicated FCM/APNs delivery component later; it is not part of this module.

No AWS, email, SMS, WhatsApp or mobile integration is implemented here.
