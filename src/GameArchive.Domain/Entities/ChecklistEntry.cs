namespace GameArchive.Domain.Entities;

public class ChecklistEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CollectionItemId { get; set; }
    public CollectionItem CollectionItem { get; set; } = null!;
    public required string Label { get; set; }
    public bool IsChecked { get; set; }
    public int SortOrder { get; set; }
}
