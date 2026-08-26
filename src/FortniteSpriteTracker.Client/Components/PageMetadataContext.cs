namespace FortniteSpriteTracker.Components;

public sealed class PageMetadataContext
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string CanonicalUrl { get; init; }
    public required string SiteName { get; init; }
    public string? ImageUrl { get; init; }
    public required string ImageAlt { get; init; }
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }
}
