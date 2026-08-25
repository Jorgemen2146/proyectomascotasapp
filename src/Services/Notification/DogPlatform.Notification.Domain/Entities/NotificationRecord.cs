using DogPlatform.Notification.Domain.Enums;

namespace DogPlatform.Notification.Domain.Entities;

public sealed class NotificationRecord
{
    private NotificationRecord() { }

    private NotificationRecord(Guid notificationId, Guid userId, Guid? petId, int? vaccineId,
        NotificationType type, string title, string message, string? referenceType,
        string? referenceId, DateTime createdAtUtc, DateOnly notificationDateUtc,
        string deduplicationKey, string? metadataJson)
    {
        NotificationId = notificationId;
        UserId = userId;
        PetId = petId;
        VaccineId = vaccineId;
        Type = type;
        Title = title;
        Message = message;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Status = NotificationStatus.Created;
        CreatedAtUtc = createdAtUtc;
        NotificationDateUtc = notificationDateUtc;
        DeduplicationKey = deduplicationKey;
        MetadataJson = metadataJson;
    }

    public Guid NotificationId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? PetId { get; private set; }
    public int? VaccineId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateOnly NotificationDateUtc { get; private set; }
    public string DeduplicationKey { get; private set; } = string.Empty;
    public string? MetadataJson { get; private set; }

    public static NotificationRecord CreateVaccination(Guid userId, Guid petId, int vaccineId,
        NotificationType type, string title, string message, DateTime createdAtUtc,
        DateOnly notificationDateUtc, string deduplicationKey, string metadataJson) =>
        new(Guid.NewGuid(), userId, petId, vaccineId, type, title, message,
            "Vaccination", $"{petId:D}:{vaccineId}", createdAtUtc,
            notificationDateUtc, deduplicationKey, metadataJson);

    public void MarkAsRead(DateTime utcNow)
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAtUtc = utcNow;
    }
}
