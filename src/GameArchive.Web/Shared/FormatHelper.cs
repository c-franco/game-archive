namespace GameArchive.Web.Shared;

public static class FormatHelper
{
    public static string FormatPrice(decimal? v, bool showDecimals = true)
    {
        if (!v.HasValue) return "—";
        var rounded = Math.Round(v.Value);
        return showDecimals ? $"{v:F2}€" : $"{rounded}€";
    }
}
