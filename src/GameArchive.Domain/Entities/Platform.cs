namespace GameArchive.Domain.Entities;

public class Platform
{
    public Guid   Id        { get; set; } = Guid.NewGuid();
    public string Name      { get; set; } = string.Empty;
    public int    SortOrder { get; set; }
}
