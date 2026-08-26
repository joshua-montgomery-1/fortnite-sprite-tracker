using System.Text;
using System.Xml.Linq;
using FortniteSpriteTracker.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class SitemapEndpoints
{
    private const string BaseUrl = "https://spritescout.com";
    private const string SitemapCacheKey = "sitemap_xml";
    private static readonly TimeSpan SitemapCacheDuration = TimeSpan.FromHours(1);

    public static IEndpointRouteBuilder MapSitemapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/robots.txt", () =>
        {
            var robots = new StringBuilder()
                .AppendLine("User-agent: *")
                .AppendLine("Allow: /")
                .AppendLine("Allow: /players")
                .AppendLine("Allow: /images/")
                .AppendLine("Allow: /css/")
                .AppendLine("Allow: /fonts/")
                .AppendLine("Disallow: /account")
                .AppendLine("Disallow: /tracked-players")
                .AppendLine("Disallow: /auth/")
                .AppendLine("Disallow: /api/")
                .AppendLine("Disallow: /error")
                .AppendLine()
                .AppendLine($"Sitemap: {BaseUrl}/sitemap.xml")
                .ToString();

            return Results.Content(robots, "text/plain", Encoding.UTF8);
        }).AllowAnonymous();

        endpoints.MapGet("/sitemap.xml", async (
            SpriteTrackerDbContext database,
            IMemoryCache memoryCache,
            CancellationToken cancellationToken) =>
        {
            if (memoryCache.TryGetValue<string>(SitemapCacheKey, out var cachedXml) && cachedXml is not null)
            {
                return Results.Content(cachedXml, "application/xml", Encoding.UTF8);
            }

            var publicPlayers = await database.Users.AsNoTracking()
                .Where(u => u.IsCollectionPublic && u.EpicDisplayName != null)
                .OrderByDescending(u => u.UpdatedAtUtc)
                .Take(10000)
                .Select(u => new { u.PublicId, u.UpdatedAtUtc })
                .ToListAsync(cancellationToken);

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var root = new XElement(ns + "urlset",
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}/"),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "1.0")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}/players"),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.8")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}/cheat-codes"),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.8")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}/privacy"),
                    new XElement(ns + "changefreq", "yearly"),
                    new XElement(ns + "priority", "0.2")
                ),
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}/terms"),
                    new XElement(ns + "changefreq", "yearly"),
                    new XElement(ns + "priority", "0.2")
                )
            );

            foreach (var player in publicPlayers)
            {
                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BaseUrl}/players/{player.PublicId:D}"),
                    new XElement(ns + "lastmod", player.UpdatedAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.6")
                ));
            }

            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                root
            );

            var xml = document.Declaration + Environment.NewLine + document.ToString();
            memoryCache.Set(SitemapCacheKey, xml, SitemapCacheDuration);

            return Results.Content(xml, "application/xml", Encoding.UTF8);
        }).AllowAnonymous();

        return endpoints;
    }
}
