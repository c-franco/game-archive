namespace GameArchive.Web.Models;

public record CollectionItemDto(
    Guid Id,
    string Name,
    string Type,
    string Platform,
    string Region,
    string Condition,
    decimal? PurchasePrice,
    decimal? EstimatedValue,
    DateTime? PurchaseDate,
    string Notes,
    string Status,
    List<ChecklistEntryDto> ChecklistEntries,
    DateTimeOffset CreatedAt
);

public record ChecklistEntryDto(Guid Id, string Label, bool IsChecked, int SortOrder);

public record ChecklistTemplateDto(Guid Id, string ItemType, string Label, int SortOrder);

public record PlatformDto(Guid Id, string Name, int SortOrder);
public record RegionDto(Guid Id, string Name, int SortOrder);

public record StatsDto(
    int TotalItems,
    int TotalOwned,
    int TotalWishlist,
    decimal TotalEstimatedValue,
    decimal TotalSpent,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByPlatform
);

public class ItemFormModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Game";
    public string Platform { get; set; } = "";
    public string Region { get; set; } = "";
    public string Condition { get; set; } = "";
    public decimal? PurchasePrice { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string Notes { get; set; } = "";
    public string Status { get; set; } = "Owned";
    public List<ChecklistEntryForm> ChecklistEntries { get; set; } = [];
}

public class ChecklistEntryForm
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public bool IsChecked { get; set; }
    public int SortOrder { get; set; }
}
