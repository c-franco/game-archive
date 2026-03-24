namespace GameArchive.Domain.Entities;

public class CollectionItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public ItemType Type { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal? PurchasePrice { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public ItemStatus Status { get; set; } = ItemStatus.Owned;
    public List<ChecklistEntry> ChecklistEntries { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
