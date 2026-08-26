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
    public string Type { get; init; } = "WebApplication";

    [JsonPropertyName("@id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("applicationCategory")]
    public string ApplicationCategory { get; init; } = "GameApplication";

    [JsonPropertyName("operatingSystem")]
    public string OperatingSystem { get; init; } = "All";

    [JsonPropertyName("offers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OfferSchema? Offer { get; init; }
}

public sealed class OfferSchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Offer";

    [JsonPropertyName("price")]
    public required string Price { get; init; }

    [JsonPropertyName("priceCurrency")]
    public required string Currency { get; init; }
}

public sealed class WebSiteSchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "WebSite";

    [JsonPropertyName("@id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

public sealed class WebPageSchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "WebPage";

    [JsonPropertyName("@id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("isPartOf")]
    public required WebSiteSchema IsPartOf { get; init; }
}

public sealed class BreadcrumbListSchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "BreadcrumbList";

    [JsonPropertyName("itemListElement")]
    public required IReadOnlyList<BreadcrumbListItemSchema> Items { get; init; }
}

public sealed class BreadcrumbListItemSchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "ListItem";

    [JsonPropertyName("position")]
    public required int Position { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("item")]
    public required string Item { get; init; }
}

public sealed class ItemListSchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "ItemList";

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("numberOfItems")]
    public required int NumberOfItems { get; init; }

    [JsonPropertyName("itemListElement")]
    public required IReadOnlyList<ItemListEntrySchema> Items { get; init; }
}

public sealed class ItemListEntrySchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "ListItem";

    [JsonPropertyName("position")]
    public required int Position { get; init; }

    [JsonPropertyName("item")]
    public required SchemaItem Item { get; init; }
}

public sealed class SchemaItem
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Thing";

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}
