namespace GameArchive.Domain.Entities;

public class ChecklistTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ItemType ItemType { get; set; }
    public required string Label { get; set; }
    public int SortOrder { get; set; }
}
