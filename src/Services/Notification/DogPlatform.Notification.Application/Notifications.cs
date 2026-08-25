using System.Text.Json;
using DogPlatform.Notification.Domain.Entities;
using DogPlatform.Notification.Domain.Enums;
using DogPlatform.Notification.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DogPlatform.Notification.Application;

public sealed record VaccinationReminderCandidate(
    Guid UserId,
    Guid PetId,
    string PetName,
    int VaccineId,
    string VaccineName,
    string Status,
    bool Eligible,
    DateTime? RecommendedDueAtUtc,
    DateTime? NextDueAtUtc,
    int? DaysRemaining,
    int? DaysOverdue);

public sealed record NotificationResponse(
    Guid NotificationId,
    string Type,
    string Title,
    string Message,
    Guid? PetId,
    int? VaccineId,
    string Status,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc,
    string? MetadataJson);

public sealed record NotificationPageResponse(
    IReadOnlyCollection<NotificationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record UnreadCountResponse(int Count);

public sealed record VaccinationReminderRunResult(
    int CandidateCount,
    int CreatedCount,
    int DuplicateCount,
    int FailedCount);

public interface INotificationRealtimePublisher
{
    Task PublishAsync(NotificationResponse notification, Guid userId,
        CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid UserId { get; }
}

public interface IVaccinationReminderSource
{
    Task<IReadOnlyCollection<VaccinationReminderCandidate>> GetCandidatesAsync(
        DateOnly dateUtc, CancellationToken cancellationToken = default);
}

public interface IVaccinationNotificationGenerator
{
    Task<VaccinationReminderRunResult> GenerateAsync(
        IReadOnlyCollection<VaccinationReminderCandidate> candidates,
        CancellationToken cancellationToken = default);
}

public interface IVaccinationReminderRunner
{
    Task<VaccinationReminderRunResult> RunAsync(CancellationToken cancellationToken = default);
}

public sealed class VaccinationReminderRunner(
    IVaccinationReminderSource source,
    IVaccinationNotificationGenerator generator,
    TimeProvider timeProvider) : IVaccinationReminderRunner
{
    public async Task<VaccinationReminderRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var date = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var candidates = await source.GetCandidatesAsync(date, cancellationToken);
        return await generator.GenerateAsync(candidates, cancellationToken);
    }
}

public sealed class VaccinationNotificationGenerator(
    INotificationRepository repository,
    INotificationRealtimePublisher realtimePublisher,
    TimeProvider timeProvider,
    ILogger<VaccinationNotificationGenerator> logger) : IVaccinationNotificationGenerator
{
    public async Task<VaccinationReminderRunResult> GenerateAsync(
        IReadOnlyCollection<VaccinationReminderCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var date = DateOnly.FromDateTime(now);
        var created = 0;
        var duplicates = 0;
        var failed = 0;

        foreach (var candidate in candidates)
        {
            if (!TryMapType(candidate, out var type))
                continue;

            try
            {
                var (title, message) = BuildMessage(candidate, type);
                var key = $"vaccination:{candidate.UserId:D}:{candidate.PetId:D}:{candidate.VaccineId}:{type}:{date:yyyy-MM-dd}";
                var metadata = JsonSerializer.Serialize(new
                {
                    candidate.PetName,
                    candidate.VaccineName,
                    candidate.RecommendedDueAtUtc,
                    candidate.NextDueAtUtc,
                    candidate.DaysRemaining,
                    candidate.DaysOverdue
                });
                var notification = NotificationRecord.CreateVaccination(
                    candidate.UserId, candidate.PetId, candidate.VaccineId, type,
                    title, message, now, date, key, metadata);

                var insertResult = await repository.TryAddAsync(notification, cancellationToken);
                if (insertResult == NotificationInsertResult.Duplicate)
                {
                    duplicates++;
                    continue;
                }

                created++;
                try
                {
                    await realtimePublisher.PublishAsync(Map(notification), candidate.UserId, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception,
                        "Realtime delivery failed for notification {NotificationId}; REST recovery remains available.",
                        notification.NotificationId);
                }
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(exception,
                    "Vaccination notification generation failed for PetId={PetId} VaccineId={VaccineId}.",
                    candidate.PetId, candidate.VaccineId);
            }
        }

        return new(candidates.Count, created, duplicates, failed);
    }

    private static bool TryMapType(VaccinationReminderCandidate candidate, out NotificationType type)
    {
        type = candidate.Status switch
        {
            "DueSoon" => NotificationType.VaccinationDueSoon,
            "DueToday" => NotificationType.VaccinationDueToday,
            "Overdue" => NotificationType.VaccinationOverdue,
            "NotStarted" when candidate.Eligible => NotificationType.VaccinationNotStarted,
            _ => default
        };
        return candidate.Status is "DueSoon" or "DueToday" or "Overdue" ||
               candidate.Status == "NotStarted" && candidate.Eligible;
    }

    private static (string Title, string Message) BuildMessage(
        VaccinationReminderCandidate candidate, NotificationType type) => type switch
    {
        NotificationType.VaccinationDueSoon =>
            ("Vacuna próxima 💉",
             $"La vacuna de {candidate.VaccineName} de {candidate.PetName} se aproxima. Faltan {candidate.DaysRemaining ?? 0} días."),
        NotificationType.VaccinationDueToday =>
            ("Vacuna para hoy 💉",
             $"Hoy corresponde la vacuna de {candidate.VaccineName} de {candidate.PetName}."),
        NotificationType.VaccinationOverdue =>
            ("Vacuna pendiente",
             $"La vacuna de {candidate.VaccineName} de {candidate.PetName} está pendiente desde hace {candidate.DaysOverdue ?? 0} días."),
        NotificationType.VaccinationNotStarted =>
            ("Vacuna sin registrar",
             $"{candidate.PetName} ya tiene edad para iniciar la vacuna de {candidate.VaccineName}."),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    internal static NotificationResponse Map(NotificationRecord notification) =>
        new(notification.NotificationId, notification.Type.ToString(), notification.Title,
            notification.Message, notification.PetId, notification.VaccineId,
            notification.Status.ToString(), notification.IsRead, notification.ReadAtUtc,
            notification.CreatedAtUtc, notification.MetadataJson);
}

public static class NotificationErrors
{
    public static readonly Error InvalidPagination = Error.Validation(
        "Notification.InvalidPagination", "Page number must be positive and page size must be between 1 and 100.");
    public static readonly Error NotFound = Error.NotFound(
        "Notification.NotFound", "Notification was not found.");
}

public sealed record ListNotificationsQuery(Guid UserId, int PageNumber, int PageSize, bool UnreadOnly)
    : IRequest<Result<NotificationPageResponse>>;
public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<Result<UnreadCountResponse>>;
public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest<Result>;
public sealed record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Result>;

public sealed class ListNotificationsQueryHandler(INotificationRepository repository)
    : IRequestHandler<ListNotificationsQuery, Result<NotificationPageResponse>>
{
    public async Task<Result<NotificationPageResponse>> Handle(
        ListNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1 || request.PageSize is < 1 or > 100)
            return Result.Failure<NotificationPageResponse>(NotificationErrors.InvalidPagination);

        var page = await repository.GetPageAsync(request.UserId, request.PageNumber,
            request.PageSize, request.UnreadOnly, cancellationToken);
        return Result.Success(new NotificationPageResponse(
            page.Items.Select(VaccinationNotificationGenerator.Map).ToArray(),
            request.PageNumber, request.PageSize, page.TotalCount));
    }
}

public sealed class GetUnreadCountQueryHandler(INotificationRepository repository)
    : IRequestHandler<GetUnreadCountQuery, Result<UnreadCountResponse>>
{
    public async Task<Result<UnreadCountResponse>> Handle(
        GetUnreadCountQuery request, CancellationToken cancellationToken) =>
        Result.Success(new UnreadCountResponse(
            await repository.GetUnreadCountAsync(request.UserId, cancellationToken)));
}

public sealed class MarkNotificationReadCommandHandler(
    INotificationRepository repository, TimeProvider timeProvider)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(
            request.UserId, request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure(NotificationErrors.NotFound);

        notification.MarkAsRead(timeProvider.GetUtcNow().UtcDateTime);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository repository, TimeProvider timeProvider)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        await repository.MarkAllAsReadAsync(
            request.UserId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return Result.Success();
    }
}
