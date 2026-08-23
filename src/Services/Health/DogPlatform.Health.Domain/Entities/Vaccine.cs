namespace DogPlatform.Health.Domain.Entities;

public sealed class Vaccine
{
    private Vaccine() { }

    public Vaccine(int vaccineId, int speciesId, string name, string? description, bool isCore,
        bool isActive, DateTime createdAt, DateTime? updatedAt = null)
    {
        VaccineId = vaccineId;
        SpeciesId = speciesId;
        Name = name;
        Description = description;
        IsCore = isCore;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public int VaccineId { get; private set; }
    public int SpeciesId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCore { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
