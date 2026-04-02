using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.RegularExpressions;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private static readonly HttpClient Http = BuildHttpClient();

    private static HttpClient BuildHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression =
                DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        return client;
    }

    [HttpGet("scrape")]
    public async Task<IActionResult> Scrape([FromQuery] string url, CancellationToken ct)
    {
        try
        {
            using var warmReq = new HttpRequestMessage(HttpMethod.Get, "https://www.pricecharting.com/");
            await Http.SendAsync(warmReq, ct);
            await Task.Delay(800, ct);
        }
        catch { }

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Referer", "https://www.pricecharting.com/");

        HttpResponseMessage resp;
        try { resp = await Http.SendAsync(req, ct); }
        catch (Exception ex) { return Ok(new { error = ex.Message }); }

        var html = await resp.Content.ReadAsStringAsync(ct);

        string? Ctx(string term)
        {
            var idx = html.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            return html[Math.Max(0, idx - 30)..Math.Min(html.Length, idx + 300)];
        }

        // All id= attributes that mention "price"
        var priceIds = Regex.Matches(html, @"id=""([^""]*price[^""]*)""", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value).Distinct().ToList();

        // All spans/divs/tds containing a dollar amount — first 20
        var dollarSnippets = Regex.Matches(html,
                @"<(?:span|td|div|p)[^>]*>\s*\$\s*[\d,]+\.?\d*\s*</(?:span|td|div|p)>",
                RegexOptions.IgnoreCase)
            .Select(m => m.Value).Distinct().Take(20).ToList();

        // Context around first dollar sign in the page
        var firstDollarIdx = html.IndexOf('$');
        var firstDollarCtx = firstDollarIdx >= 0
            ? html[Math.Max(0, firstDollarIdx - 50)..Math.Min(html.Length, firstDollarIdx + 200)]
            : null;

        // Search: all /game/ hrefs (numeric IDs included)
        var gameLinks = Regex.Matches(html, @"href=""(/game/[^""?#]+)""", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value).Distinct().Take(20).ToList();

        return Ok(new
        {
            status          = (int)resp.StatusCode,
            htmlLength      = html.Length,
            priceIds,
            dollarSnippets,
            firstDollarCtx,
            gameLinks,
            // Raw 2000-char slice around the word "price" in the body (skip CSS section)
            priceSectionCtx = Ctx("js-price-selector"),
            looseCtx        = Ctx("loose"),
            completeCtx     = Ctx("complete"),
        });
    }
}
