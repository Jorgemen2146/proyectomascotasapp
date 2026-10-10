# Social and chat contract for Mobile

This contract describes the current Gateway-facing API. JSON uses camelCase. UUID values are strings, and UTC timestamps are ISO 8601 strings. Except for the WebSocket query-token option described below, all endpoints require `Authorization: Bearer <JWT>`; the current user and every `senderUserId` come only from the validated JWT.

## Gateway endpoints

| Method | Path | Success | Request / query |
|---|---|---:|---|
| GET | `/api/v1/social/users/search` | 200 | `query` required; `limit` 1..50, default 20 |
| GET | `/api/v1/social/friends` | 200 | `page` >= 1; `pageSize` 1..100, defaults 1/20 |
| GET | `/api/v1/social/friend-requests/incoming` | 200 | same page parameters |
| GET | `/api/v1/social/friend-requests/outgoing` | 200 | same page parameters |
| POST | `/api/v1/social/friend-requests` | 201 | `{"addresseeUserId":"uuid"}` |
| POST | `/api/v1/social/friend-requests/{id}/accept` | 200 | no body |
| POST | `/api/v1/social/friend-requests/{id}/reject` | 200 | no body |
| DELETE | `/api/v1/social/friend-requests/{id}` | 204 | no body; requester only |
| GET | `/api/v1/social/blocked-users` | 200 | same page parameters |
| POST | `/api/v1/social/users/{userId}/block` | 204 | no body |
| DELETE | `/api/v1/social/users/{userId}/block` | 204 | no body |
| GET | `/api/v1/social/conversations` | 200 | same page parameters |
| POST | `/api/v1/social/conversations` | 201 | `{"userId":"uuid"}` |
| GET | `/api/v1/social/conversations/{id}` | 200 | no body |
| GET | `/api/v1/social/conversations/{id}/messages` | 200 | `pageSize` 1..100, default 50; optional cursor `before` + `beforeId` |
| POST | `/api/v1/social/conversations/{id}/messages` | 201 | `{"content":"1..2000 characters"}` |
| POST | `/api/v1/social/conversations/{id}/read` | 204 | no body |

## JSON models

`UserCard`:

```json
{
  "userId": "uuid",
  "displayName": "Ada Lovelace",
  "profilePhotoUrl": "/api/v1/auth/users/{userId}/photo/content",
  "socialStatus": "None"
}
```

`profilePhotoUrl` is nullable. `socialStatus` is populated by user search and is one of exactly:

- `None`
- `Friends`
- `PendingSent`
- `PendingReceived`

Blocked users and the authenticated user are excluded from search. In nested cards outside search, `socialStatus` is normally `null`.

`FriendRequest` (also the payload of both friend-request events):

```json
{
  "id": "uuid",
  "requesterUserId": "uuid",
  "addresseeUserId": "uuid",
  "status": "Pending",
  "createdAtUtc": "2026-10-10T18:57:24.2887232Z",
  "respondedAtUtc": null,
  "user": null
}
```

`status` is one of `Pending`, `Accepted`, `Rejected`, `Cancelled`. `user` is the counterpart card on incoming/outgoing lists and can be null; command responses and realtime payloads currently return it as null.

`Friendship`:

```json
{
  "friendshipId": "uuid",
  "user": { "userId": "uuid", "displayName": "Ada Lovelace", "profilePhotoUrl": null, "socialStatus": null },
  "createdAtUtc": "2026-10-10T18:57:26.4133892Z"
}
```

`Block`:

```json
{
  "blockId": "uuid",
  "user": { "userId": "uuid", "displayName": "Ada Lovelace", "profilePhotoUrl": null, "socialStatus": null },
  "createdAtUtc": "2026-10-10T19:00:00Z"
}
```

`Message` (also the `chat.message.received` payload):

```json
{
  "id": "uuid",
  "conversationId": "uuid",
  "senderUserId": "uuid",
  "content": "Hello",
  "createdAtUtc": "2026-10-10T18:58:13.5047285Z",
  "editedAtUtc": null,
  "deletedAtUtc": null
}
```

`Conversation`:

```json
{
  "id": "uuid",
  "counterpart": { "userId": "uuid", "displayName": "Ada Lovelace", "profilePhotoUrl": null, "socialStatus": null },
  "lastMessage": null,
  "createdAtUtc": "2026-10-10T18:57:26.7064898Z",
  "lastMessageAtUtc": null,
  "unreadCount": 0
}
```

## Numbered pagination

Friends, requests, blocks, and conversations return:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0,
  "hasNextPage": false
}
```

Blocked relationships/conversations are excluded before `totalItems`, `totalPages`, and page slicing are calculated. Missing Identity cards use a safe public placeholder rather than silently shrinking `items`.

## Message cursor

Messages are newest first. A response is:

```json
{
  "items": [],
  "nextCursor": {
    "before": "2026-10-10T18:58:13.5047285Z",
    "beforeId": "uuid"
  },
  "hasMore": false
}
```

`nextCursor` is null only when `items` is empty; it can be non-null when `hasMore` is false. To request the next page, send both returned values as `before` and `beforeId`. Deleted messages are excluded.

## HTTP errors

Expected domain errors use:

```json
{
  "code": "Social.NotFriends",
  "description": "Only friends can start conversations or send messages.",
  "type": 3
}
```

| HTTP | Codes / meaning |
|---:|---|
| 400 | `Social.SelfRequest`, `Social.InvalidMessage`, `Social.InvalidPagination`; malformed JSON uses ASP.NET validation problem details |
| 401 | Missing, invalid, or expired JWT |
| 403 | `Social.Unauthorized` (`type: 4`): not the request owner/addressee or not a conversation participant |
| 404 | `Social.NotFound` (`type: 2`): missing or deliberately hidden blocked resource |
| 409 | `Social.AlreadyFriends`, `Social.DuplicateRequest`, `Social.Blocked`, `Social.NotFriends` (`type: 3`) |

A block removes the friendship and cancels pending requests atomically. While either direction is blocked, search/list access hides the relationship and conversation, messages cannot be sent, and conversations cannot be created. Unblock is idempotent and never restores friendship. The historical conversation becomes visible again after unblock, but sending/creating still returns `Social.NotFriends` until a new request is accepted.

## Unread and read

`unreadCount` counts non-deleted messages sent by the counterpart after the participant's last-read timestamp. `POST .../read` marks through the latest message and returns 204. It is idempotent. If a latest message exists, the other participant receives `chat.message.read`; this receipt is realtime-only and a delivery failure never changes the successful 204 or rolls back persisted read state.

## WebSocket

Gateway endpoint: `ws(s)://<gateway>/ws/notifications`.

Authentication options:

- preferred where supported: `Authorization: Bearer <JWT>` during the upgrade;
- mobile-compatible fallback: `/ws/notifications?access_token=<URL-encoded-JWT>`.

The query token is accepted only on this WebSocket path and only when no Authorization header is present. An unauthenticated upgrade returns 401. The server sends JSON text envelopes and expects no application messages from the client:

```json
{
  "event": "event.name",
  "data": {}
}
```

Exact events and data:

`social.friend_request.received` → target/addressee:

```json
{"event":"social.friend_request.received","data":{"id":"uuid","requesterUserId":"uuid","addresseeUserId":"uuid","status":"Pending","createdAtUtc":"timestamp","respondedAtUtc":null,"user":null}}
```

`social.friend_request.accepted` → original requester:

```json
{"event":"social.friend_request.accepted","data":{"id":"uuid","requesterUserId":"uuid","addresseeUserId":"uuid","status":"Accepted","createdAtUtc":"timestamp","respondedAtUtc":"timestamp","user":null}}
```

`chat.message.received` → counterpart:

```json
{"event":"chat.message.received","data":{"id":"uuid","conversationId":"uuid","senderUserId":"uuid","content":"Hello","createdAtUtc":"timestamp","editedAtUtc":null,"deletedAtUtc":null}}
```

`chat.message.read` → other participant:

```json
{"event":"chat.message.read","data":{"conversationId":"uuid","readerUserId":"uuid","lastReadMessageId":"uuid","readAtUtc":"timestamp"}}
```

The first three events are persisted as notifications before realtime delivery and can be recovered through the existing Notifications REST API; `chat.message.read` is not persisted. Realtime delivery is best-effort: a disconnected socket or transport failure is logged and never rolls back a friend request, acceptance, message, or read state.