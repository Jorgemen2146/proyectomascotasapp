namespace DogPlatform.Contracts.IntegrationEvents.Health;

public sealed class AppointmentScheduledIntegrationEvent : IntegrationEvent
{
    public AppointmentScheduledIntegrationEvent(Guid appointmentId, Guid petId, Guid veterinarianId, DateTime scheduledAt)
    {
        AppointmentId = appointmentId;
        PetId = petId;
        VeterinarianId = veterinarianId;
        ScheduledAt = scheduledAt;
    }

    public Guid AppointmentId { get; }
    public Guid PetId { get; }
    public Guid VeterinarianId { get; }
    public DateTime ScheduledAt { get; }
}
