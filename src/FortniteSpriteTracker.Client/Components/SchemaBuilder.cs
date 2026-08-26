namespace FortniteSpriteTracker.Components;

public static class SchemaBuilder
{
    private const string Origin = "https://spritescout.com/";

    public static string Url(string path) => $"{Origin}{path.TrimStart('/')}";

    public static BreadcrumbListSchema Breadcrumbs(
        IReadOnlyList<(string Name, string Path)> items) => new()
    {
        Items = items
            .Select((item, index) => new BreadcrumbListItemSchema
            {
                Position = index + 1,
                Name = item.Name,
                Item = Url(item.Path)
            })
            .ToArray()
    };

    public static ItemListSchema ItemList(
        string name,
        IReadOnlyList<SchemaItem> items) => new()
    {
        Name = name,
        NumberOfItems = items.Count,
        Items = items
            .Select((item, index) => new ItemListEntrySchema
            {
                Position = index + 1,
                Item = item
            })
            .ToArray()
    };
}
