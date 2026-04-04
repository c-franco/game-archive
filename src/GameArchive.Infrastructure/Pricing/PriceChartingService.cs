using GameArchive.Application.Common;
using GameArchive.Application.Resources;
using GameArchive.Domain;
using GameArchive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameArchive.Infrastructure.Pricing;

public record PriceFetchResult(
    bool Success,
    decimal? PriceEur,
    string Condition,
    string? ProductUrl,
    string? ErrorMessage
);

public record BatchPriceResult(int Total, int Updated, int Failed, List<string> Errors);

public class PriceChartingService(IAppDbContext db, ILogger<PriceChartingService> logger)
{
    private static readonly HttpClient Http = BuildHttpClient();
    private static bool _warmedUp;
    private static readonly SemaphoreSlim WarmupLock = new(1, 1);

    private static HttpClient BuildHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression =
                DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Pragma",        "no-cache");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        return client;
    }

    private async Task EnsureWarmUpAsync(CancellationToken ct)
    {
        if (_warmedUp) return;
        await WarmupLock.WaitAsync(ct);
        try
        {
            if (_warmedUp) return;
            logger.LogInformation("[WarmUp] Fetching homepage for session cookies…");
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.pricecharting.com/");
            var resp = await Http.SendAsync(req, ct);
            logger.LogInformation("[WarmUp] Status: {S}", resp.StatusCode);
            await Task.Delay(1200, ct);
            _warmedUp = true;
        }
        catch (Exception ex) { logger.LogWarning("[WarmUp] {E}", ex.Message); _warmedUp = true; }
        finally { WarmupLock.Release(); }
    }

    // ── Slug map ──────────────────────────────────────────────────────────────

    private static readonly Dictionary<string, string> SlugMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "nes|pal",              "pal-nes" },
        { "snes|pal",             "pal-super-nintendo" },
        { "nintendo 64|pal",      "pal-nintendo-64" },
        { "gamecube|pal",         "pal-gamecube" },
        { "wii|pal",              "pal-wii" },
        { "wii u|pal",            "pal-wii-u" },
        { "nintendo switch|pal",  "pal-nintendo-switch" },
        { "nintendo switch 2|pal",  "pal-nintendo-switch-2" },
        { "game boy|pal",         "pal-gameboy" },
        { "game boy color|pal",   "pal-gameboy-color" },
        { "game boy advance|pal", "pal-gameboy-advance" },
        { "nintendo ds|pal",      "pal-nintendo-ds" },
        { "nintendo 3ds|pal",     "pal-nintendo-3ds" },
        { "nes|ntsc-u",              "nes" },
        { "snes|ntsc-u",             "super-nintendo" },
        { "nintendo 64|ntsc-u",      "nintendo-64" },
        { "gamecube|ntsc-u",         "gamecube" },
        { "wii|ntsc-u",              "wii" },
        { "wii u|ntsc-u",            "wii-u" },
        { "nintendo switch|ntsc-u",  "nintendo-switch" },
        { "nintendo switch 2|ntsc-u",  "nintendo-switch-2" },
        { "game boy|ntsc-u",         "gameboy" },
        { "game boy color|ntsc-u",   "gameboy-color" },
        { "game boy advance|ntsc-u", "gameboy-advance" },
        { "nintendo ds|ntsc-u",      "nintendo-ds" },
        { "nintendo 3ds|ntsc-u",     "nintendo-3ds" },
        { "nes|ntsc-j",              "famicom" },
        { "snes|ntsc-j",             "super-famicom" },
        { "nintendo 64|ntsc-j",      "jp-nintendo-64" },
        { "gamecube|ntsc-j",         "jp-gamecube" },
        { "game boy|ntsc-j",         "jp-gameboy" },
        { "game boy color|ntsc-j",   "jp-gameboy-color" },
        { "game boy advance|ntsc-j", "jp-gameboy-advance" },
        { "nintendo ds|ntsc-j",      "jp-nintendo-ds" },
        { "nintendo 3ds|ntsc-j",     "jp-nintendo-3ds" },
        { "playstation|pal",      "pal-playstation" },
        { "playstation 2|pal",    "pal-ps2" },
        { "playstation 3|pal",    "pal-ps3" },
        { "playstation 4|pal",    "pal-ps4" },
        { "psp|pal",              "pal-psp" },
        { "ps vita|pal",          "pal-ps-vita" },
        { "playstation|ntsc-u",   "playstation" },
        { "playstation 2|ntsc-u", "playstation-2" },
        { "playstation 3|ntsc-u", "playstation-3" },
        { "playstation 4|ntsc-u", "playstation-4" },
        { "playstation 5|ntsc-u", "playstation-5" },
        { "psp|ntsc-u",           "psp" },
        { "ps vita|ntsc-u",       "ps-vita" },
        { "playstation|ntsc-j",   "jp-playstation" },
        { "playstation 2|ntsc-j", "jp-ps2" },
        { "xbox|pal",             "pal-xbox" },
        { "xbox 360|pal",         "pal-xbox-360" },
        { "xbox|ntsc-u",            "xbox" },
        { "xbox 360|ntsc-u",        "xbox-360" },
        { "xbox one|ntsc-u",        "xbox-one" },
        { "xbox series x/s|ntsc-u", "xbox-series-x" },
        { "sega mega drive|pal",    "pal-sega-genesis" },
        { "sega saturn|pal",        "pal-sega-saturn" },
        { "sega dreamcast|pal",     "pal-sega-dreamcast" },
        { "sega mega drive|ntsc-u", "sega-genesis" },
        { "sega saturn|ntsc-u",     "sega-saturn" },
        { "sega dreamcast|ntsc-u",  "sega-dreamcast" },
    };

    public static string? ResolveSlug(string platform, string region)
    {
        var r = region.Trim().ToUpperInvariant();
        if (r == "NTSC") r = "NTSC-U";
        return SlugMap.TryGetValue($"{platform.Trim()}|{r}", out var s) ? s : null;
    }

    public static string ResolveCondition(CollectionItem item, ILogger? logger = null)
    {
        if (item.Condition.Equals("New", StringComparison.OrdinalIgnoreCase)) return "New";
        var ticked = item.ChecklistEntries
            .Where(e => e.IsChecked).Select(e => e.Label.ToLowerInvariant()).ToHashSet();
        
        logger?.LogInformation("[ResolveCondition] Item='{Name}' Type={Type} Ticked=[{Ticked}]", 
            item.Name, item.Type, string.Join(", ", ticked));
        
        bool box = ticked.Any(l => l.Contains("box") || l.Contains("caja"));
        bool manual = ticked.Any(l => l.Contains("manual"));
        
        logger?.LogInformation("[ResolveCondition] HasBox={Box} HasManual={Manual}", box, manual);

        if (item.Type == ItemType.Console)
        {
            bool controller = ticked.Any(l => l.Contains("controller") || l.Contains("mando"));
            bool cables = ticked.Any(l => l.Contains("cable"));
            bool isCib = box && controller && cables;
            logger?.LogInformation("[ResolveCondition] Console: HasController={Ctrl} HasCables={Cables} Result={Result}", 
                controller, cables, isCib ? "CIB" : "Loose");
            return isCib ? "CIB" : "Loose";
        }
        else
        {
            bool media = ticked.Any(l => l.Contains("cartridge") || l.Contains("disc") || l.Contains("game")
                || l.Contains("cartucho") || l.Contains("disco"));
            bool isCib = box && manual && media;
            logger?.LogInformation("[ResolveCondition] Game: HasMedia={Media} Result={Result}", 
                media, isCib ? "CIB" : "Loose");
            return isCib ? "CIB" : "Loose";
        }
    }

    // ── EUR conversion ────────────────────────────────────────────────────────

    private static decimal _cachedRate;
    private static DateTime _rateExpiry = DateTime.MinValue;

    private async Task<decimal> GetEurRateAsync(CancellationToken ct)
    {
        if (_cachedRate > 0 && DateTime.UtcNow < _rateExpiry) return _cachedRate;
        try
        {
            var json = await Http.GetStringAsync("https://api.frankfurter.app/latest?from=USD&to=EUR", ct);
            using var doc = JsonDocument.Parse(json);
            _cachedRate = doc.RootElement.GetProperty("rates").GetProperty("EUR").GetDecimal();
            _rateExpiry = DateTime.UtcNow.AddHours(6);
        }
        catch { _cachedRate = 0.92m; }
        return _cachedRate;
    }

    // ── Name → URL slug ───────────────────────────────────────────────────────

    private static string ToSlug(string name)
    {
        var s = name.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[''']",          "");
        s = Regex.Replace(s, @"[^a-z0-9\s\-]", " ");
        s = Regex.Replace(s, @"\s+",            "-");
        return s.Trim('-');
    }

    // ── HTTP helper ───────────────────────────────────────────────────────────

    private async Task<string> GetAsync(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Referer", "https://www.pricecharting.com/");
        var resp = await Http.SendAsync(req, ct);
        logger.LogInformation("[HTTP] {Url} → {S}", url, resp.StatusCode);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    // ── Find product URL ──────────────────────────────────────────────────────
    //
    // Search returns numeric IDs: /game/4954712
    // We must verify the result belongs to the right console by loading the
    // page and checking its console name, OR by including the console slug
    // in the search query so PriceCharting ranks the right version first.

    private async Task<string?> FindProductUrlAsync(string name, string consoleSlug, CancellationToken ct)
    {
        // 1. Direct slug URL (works most of the time)
        var direct = $"https://www.pricecharting.com/game/{consoleSlug}/{ToSlug(name)}";
        logger.LogInformation("[Find] Direct: {U}", direct);
        try
        {
            var html = await GetAsync(direct, ct);
            if (html.Contains("used_price") || html.Contains("complete_price"))
            {
                logger.LogInformation("[Find] Direct hit");
                return direct;
            }
        }
        catch (Exception ex) { logger.LogInformation("[Find] Direct failed: {E}", ex.Message); }

        // 2. Search — include console slug in query for better ranking
        // e.g. "super mario world pal-super-nintendo"
        var query  = Uri.EscapeDataString($"{name} {consoleSlug}");
        var search = $"https://www.pricecharting.com/search-products?type=prices&q={query}";
        logger.LogInformation("[Find] Search: {U}", search);
        try
        {
            var html = await GetAsync(search, ct);

            // Results are numeric: href="/game/4954712"
            // The page also contains the console name in the row — look for
            // a link followed by the console name within ~500 chars
            var rowPattern = new Regex(
                @"href=""(/game/\d+)""[\s\S]{0,600}?" + Regex.Escape(consoleSlug.Replace("-", " ")),
                RegexOptions.IgnoreCase);

            var rowMatch = rowPattern.Match(html);
            if (rowMatch.Success)
            {
                var url = "https://www.pricecharting.com" + rowMatch.Groups[1].Value;
                logger.LogInformation("[Find] Row match with console: {U}", url);
                return url;
            }

            // Fallback: first numeric /game/ link
            var firstLink = Regex.Match(html, @"href=""(/game/\d+)""", RegexOptions.IgnoreCase);
            if (firstLink.Success)
            {
                var url = "https://www.pricecharting.com" + firstLink.Groups[1].Value;
                logger.LogInformation("[Find] First numeric link: {U}", url);
                return url;
            }
        }
        catch (Exception ex) { logger.LogError("[Find] Search threw: {E}", ex.Message); }

        return null;
    }

    // ── Scrape prices ─────────────────────────────────────────────────────────
    //
    // IDs in the HTML use UNDERSCORES: used_price, complete_price, new_price
    // Prices are in: <td id="used_price" ...> <span class="price js-price">$14.78</span>

    private async Task<(decimal? loose, decimal? cib, decimal? newP)> ScrapePricesAsync(
        string url, CancellationToken ct)
    {
        var html = await GetAsync(url, ct);
        logger.LogInformation("[Scrape] Page {L} chars", html.Length);

        // Try multiple patterns for each price type
        decimal? TryExtract(string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                var idx = html.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var window = html[idx..Math.Min(html.Length, idx + 400)];
                    logger.LogDebug("[Scrape] Found pattern '{P}', window: {W}", pattern, window[..Math.Min(100, window.Length)]);

                    var m = Regex.Match(window,
                        @"class=""[^""]*js-price[^""]*""[^>]*>\s*\$\s*([\d,]+\.?\d{0,2})\s*<",
                        RegexOptions.IgnoreCase);

                    if (m.Success)
                    {
                        var raw = m.Groups[1].Value.Replace(",", "");
                        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                        {
                            logger.LogInformation("[Scrape] Pattern '{P}' → ${V}", pattern, v);
                            return v;
                        }
                    }
                }
            }
            return null;
        }

        // Try multiple patterns for each price
        var loosePatterns = new[] { "used_price", "class=\"price numeric used_price\"", "used-price" };
        var cibPatterns = new[] { "cib_price", "class=\"price numeric cib_price\"", "complete_price", "complete-price", "cib-price" };
        var newPatterns = new[] { "new_price", "class=\"price numeric new_price\"", "new-price" };

        var loose = TryExtract(loosePatterns);
        var cib = TryExtract(cibPatterns);
        var newP = TryExtract(newPatterns);

        if (cib == null)
        {
            // Fallback: try to find CIB price by looking for the span with js-price after cib_price class
            var cibIdx = html.IndexOf("cib_price", StringComparison.OrdinalIgnoreCase);
            if (cibIdx >= 0)
            {
                var window = html[cibIdx..Math.Min(html.Length, cibIdx + 500)];
                var m = Regex.Match(window, @"<span[^>]*js-price[^>]*>\s*\$\s*([\d,]+\.?\d{0,2})", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var raw = m.Groups[1].Value.Replace(",", "");
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    {
                        logger.LogInformation("[Scrape] CIB fallback → ${V}", v);
                        cib = v;
                    }
                }
            }
        }

        return (loose, cib, newP);
    }

    // ── Public: single item ───────────────────────────────────────────────────

    public async Task<PriceFetchResult> FetchPriceAsync(CollectionItem item, CancellationToken ct = default)
    {
        await EnsureWarmUpAsync(ct);

        var slug = ResolveSlug(item.Platform, item.Region);
        if (slug is null)
            return new(false, null, "", null,
                ServerStrings.PriceCharting.UnsupportedPlatformRegion(item.Platform, item.Region));

        var condition = ResolveCondition(item, logger);
        logger.LogInformation("=== '{N}' | {S} | {C} ===", item.Name, slug, condition);

        string? productUrl;
        
        // Use manual URL if provided, otherwise search
        if (!string.IsNullOrWhiteSpace(item.ProductUrl))
        {
            productUrl = item.ProductUrl;
            logger.LogInformation("[FetchPrice] Using manual URL: {Url}", productUrl);
        }
        else
        {
            try   { productUrl = await FindProductUrlAsync(item.Name, slug, ct); }
            catch (Exception ex)
                { return new(false, null, condition, null, ServerStrings.PriceCharting.SearchError(ex.Message)); }

            if (productUrl is null)
            return new(false, null, condition, null, ServerStrings.PriceCharting.ProductNotFound);
        }

        decimal? usd;
        try
        {
            var (loose, cib, newP) = await ScrapePricesAsync(productUrl, ct);
            logger.LogInformation("Prices → loose={L} cib={C} new={N}", loose, cib, newP);
            usd = condition switch
            {
                "New" => newP  ?? cib ?? loose,
                "CIB" => cib   ?? loose,
                _     => loose ?? cib,
            };
        }
        catch (Exception ex)
            { return new(false, null, condition, productUrl, ServerStrings.PriceCharting.FetchPriceError(ex.Message)); }

        if (usd is null)
            return new(false, null, condition, productUrl, ServerStrings.PriceCharting.PriceNotAvailable);

        var rate = await GetEurRateAsync(ct);
        var eur  = Math.Round(usd.Value * rate, 2);
        logger.LogInformation("'{N}' → ${U} × {R} = €{E}", item.Name, usd, rate, eur);

        return new(true, eur, condition, productUrl, null);
    }

    // ── Public: batch ─────────────────────────────────────────────────────────

    public async Task<BatchPriceResult> FetchAllPricesAsync(
        IProgress<(int done, int total)> progress,
        CancellationToken ct = default)
    {
        var items = await db.Items
            .Include(i => i.ChecklistEntries)
            .Where(i => i.Status == ItemStatus.Owned)
            .ToListAsync(ct);

        int total = items.Count, updated = 0, failed = 0;
        var errors = new List<string>();

        for (int i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var result = await FetchPriceAsync(items[i], ct);

            if (result.Success && result.PriceEur.HasValue)
            {
                items[i].EstimatedValue     = result.PriceEur;
                items[i].PriceLastFetchedAt = DateTimeOffset.UtcNow;
                items[i].PriceSource        = "PriceCharting";
                items[i].UpdatedAt          = DateTimeOffset.UtcNow;
                updated++;
            }
            else
            {
                failed++;
                errors.Add($"{items[i].Name}: {result.ErrorMessage}");
            }

            progress.Report((i + 1, total));

            if (i < items.Count - 1)
                await Task.Delay(2000, ct);
        }

        if (updated > 0)
            await db.SaveChangesAsync(ct);

        return new(total, updated, failed, errors);
    }
}
