namespace GameArchive.Application.DTOs;

public record CollectionItemDto(
    Guid Id,
    string Name,
    string Type,
    string Platform,
    string Region,
    string Condition,
    decimal? PurchasePrice,
    decimal? EstimatedValue,
    DateOnly? PurchaseDate,
    string Notes,
    string Status,
    List<ChecklistEntryDto> ChecklistEntries,
    DateTimeOffset CreatedAt,
    string ProductUrl
);

public record ChecklistEntryDto(Guid Id, string Label, bool IsChecked, int SortOrder);

public record ChecklistTemplateDto(Guid Id, string ItemType, string Label, int SortOrder);
