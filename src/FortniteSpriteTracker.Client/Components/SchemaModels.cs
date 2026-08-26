using System.Text.Json.Serialization;

namespace FortniteSpriteTracker.Components;

public sealed class SchemaGraph
{
    [JsonPropertyName("@context")]
    public string Context { get; init; } = "https://schema.org";

    [JsonPropertyName("@graph")]
    public required IReadOnlyList<object> Nodes { get; init; }
}

public sealed class WebApplicationSchema
{
    [JsonPropertyName("@type")]
    public string Type => "WebApplication";

    [JsonIgnore]
    public string? Id { get; init; }

    [JsonPropertyName("@id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CanonicalId => Id is null ? null : SchemaUrl.Absolute(Id);

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonIgnore]
    public required string Path { get; init; }

    [JsonPropertyName("url")]
    public string Url => SchemaUrl.Absolute(Path);

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("applicationCategory")]
    public string ApplicationCategory { get; init; } = "GameApplication";

    [JsonPropertyName("operatingSystem")]
    public string OperatingSystem { get; init; } = "All";

    [JsonPropertyName("offers")]
    public OfferSchema? Offer { get; init; }
}

public sealed class OfferSchema
{
    [JsonPropertyName("@type")]
    public string Type => "Offer";

    [JsonPropertyName("price")]
    public required string Price { get; init; }

    [JsonPropertyName("priceCurrency")]
    public required string Currency { get; init; }
}

public sealed class WebSiteSchema
{
    [JsonPropertyName("@type")]
    public string Type => "WebSite";

    [JsonIgnore]
    public string? Id { get; init; }

    [JsonPropertyName("@id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CanonicalId => Id is null ? null : SchemaUrl.Absolute(Id);

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonIgnore]
    public required string Path { get; init; }

    [JsonPropertyName("url")]
    public string Url => SchemaUrl.Absolute(Path);

}

public sealed class WebPageSchema
{
    [JsonPropertyName("@type")]
    public string Type => "WebPage";

    [JsonIgnore]
    public string? Id { get; init; }

    [JsonPropertyName("@id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CanonicalId => Id is null ? null : SchemaUrl.Absolute(Id);

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonIgnore]
    public required string Path { get; init; }

    [JsonPropertyName("url")]
    public string Url => SchemaUrl.Absolute(Path);

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("isPartOf")]
    public required WebSiteSchema IsPartOf { get; init; }
}

public sealed class BreadcrumbListSchema
{
    [JsonPropertyName("@type")]
    public string Type => "BreadcrumbList";

    [JsonIgnore]
    public required IReadOnlyList<BreadcrumbSchema> Items { get; init; }

    [JsonPropertyName("itemListElement")]
    public IReadOnlyList<object> ItemListElement => Items
        .Select((item, index) => (object)new BreadcrumbListItemSchema
        {
            Position = index + 1,
            Name = item.Name,
            Path = item.Path
        })
        .ToArray();
}

public sealed class BreadcrumbSchema
{
    public required string Name { get; init; }
    public required string Path { get; init; }
}

public sealed class ItemListSchema
{
    [JsonPropertyName("@type")]
    public string Type => "ItemList";

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonIgnore]
    public required IReadOnlyList<SchemaItem> Items { get; init; }

    [JsonPropertyName("numberOfItems")]
    public int NumberOfItems => Items.Count;

    [JsonPropertyName("itemListElement")]
    public IReadOnlyList<object> ItemListElement => Items
        .Select((item, index) => (object)new ThingListItemSchema
        {
            Position = index + 1,
            Item = item
        })
        .ToArray();
}

public sealed class SchemaItem
{
    [JsonPropertyName("@type")]
    public string Type => "Thing";

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}

internal sealed class BreadcrumbListItemSchema
{
    [JsonPropertyName("@type")]
    public string Type => "ListItem";

    [JsonPropertyName("position")]
    public required int Position { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonIgnore]
    public required string Path { get; init; }

    [JsonPropertyName("item")]
    public string Item => SchemaUrl.Absolute(Path);
}

internal sealed class ThingListItemSchema
{
    [JsonPropertyName("@type")]
    public string Type => "ListItem";

    [JsonPropertyName("position")]
    public required int Position { get; init; }

    [JsonPropertyName("item")]
    public required SchemaItem Item { get; init; }
}

internal static class SchemaUrl
{
    private const string Origin = "https://spritescout.com/";

    public static string Absolute(string path) => $"{Origin}{path.TrimStart('/')}";
}
