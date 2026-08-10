using System.Collections.Concurrent;
using System.Text.Json;
using FortniteSpriteTracker.Models;

var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
var outputDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(repositoryRoot, "src", "FortniteSpriteTracker", "wwwroot", "images", "sprites");
Directory.CreateDirectory(outputDirectory);

var assets = SpriteData.Sprites
    .SelectMany(sprite => sprite.Variants.Select(variant => new Asset(
        sprite.Name,
        variant.ToString(),
        SourceImageUrl(sprite.Slug, variant),
        Path.GetFileName(SpriteData.VariantImageUrl(sprite.Slug, variant)))))
    .DistinctBy(asset => asset.FileName)
    .OrderBy(asset => asset.FileName)
    .ToArray();

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
client.DefaultRequestHeaders.UserAgent.ParseAdd("SpriteScoutAssetDownloader/1.0");
var failures = new ConcurrentBag<string>();

await Parallel.ForEachAsync(assets, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (asset, token) =>
{
    try
    {
        using var response = await client.GetAsync(asset.SourceUrl, token);
        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Unexpected content type: {contentType ?? "missing"}");
        var bytes = await response.Content.ReadAsByteArrayAsync(token);
        if (bytes.Length < 100) throw new InvalidDataException("Image response was unexpectedly small.");
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, asset.FileName), bytes, token);
        Console.WriteLine($"{asset.FileName} ({bytes.Length:N0} bytes)");
    }
    catch (Exception exception)
    {
        failures.Add($"{asset.SourceUrl}: {exception.Message}");
    }
});

var manifest = new
{
    generatedAtUtc = DateTimeOffset.UtcNow,
    source = "https://fortnitespritetracker.org/images/sprites/",
    count = assets.Length,
    assets
};
await File.WriteAllTextAsync(
    Path.Combine(outputDirectory, "manifest.json"),
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

if (!failures.IsEmpty)
{
    foreach (var failure in failures.Order()) Console.Error.WriteLine(failure);
    return 1;
}

Console.WriteLine($"Downloaded {assets.Length} Sprite assets to {outputDirectory}");
return 0;

static string FindRepositoryRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FortniteSpriteTracker.sln")))
        directory = directory.Parent;
    return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate FortniteSpriteTracker.sln.");
}

static string SourceImageUrl(string slug, SpriteVariant variant)
{
    // The upstream source predates the Holofoil naming convention for these two assets.
    var sourceSuffix = variant == SpriteVariant.Holofoil && slug is "air" or "ghost"
        ? "holo"
        : SpriteData.Variants[variant].ImageSuffix;
    return $"https://fortnitespritetracker.org/images/sprites/{slug}_{sourceSuffix}.webp";
}

internal sealed record Asset(string Sprite, string Variant, string SourceUrl, string FileName);
