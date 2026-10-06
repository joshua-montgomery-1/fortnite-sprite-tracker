using SpriteScout.Auth;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace FortniteSpriteTracker.Components.Auth;

public sealed class AuthPagePresentation(IAuthArtworkSource source)
{
    public string? Nonce { get; private set; }
    public IReadOnlyList<string> Sprites { get; private set; } = [];
    public string SpriteStyles { get; private set; } = "";

    public async Task InitializeAsync(HttpContext context, string? formRedirect = null)
    {
        if (Nonce is not null) return;
        var artwork = await source.GetAsync(context.RequestAborted);
        Nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var groups = artwork.GroupBy(item => item.SeasonId).Select(group =>
        {
            var paths = group.Select(item => item.ImagePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == "https" ? uri.AbsoluteUri : "/" + path.TrimStart('/'))
                .Distinct().ToArray();
            Random.Shared.Shuffle(paths);
            return paths;
        }).ToArray();
        Random.Shared.Shuffle(groups);
        var selected = new HashSet<string>();
        for (var round = 0; selected.Count < 10 && groups.Any(group => round < group.Length); round++)
            foreach (var group in groups)
                if (round < group.Length && selected.Count < 10) selected.Add(group[round]);
        var sprites = selected.ToArray();
        Random.Shared.Shuffle(sprites);
        var positions = new List<(int X, int Y)>();
        var variation = string.Join("", sprites.Select((_, index) =>
        {
            // Scatter through the whole scene, allowing some artwork behind the card.
            // A small minimum distance prevents an accidental pile of sprites.
            var point = (X: Random.Shared.Next(2, 99), Y: Random.Shared.Next(8, 98));
            for (var attempt = 0; attempt < 30 && positions.Any(existing =>
                Math.Pow(existing.X - point.X, 2) + Math.Pow(existing.Y - point.Y, 2) < 225); attempt++)
                point = (Random.Shared.Next(2, 99), Random.Shared.Next(8, 98));
            positions.Add(point);
            var scale = Random.Shared.Next(72, 106);
            return $".layer-{index} {{ left:{point.X}%; top:{point.Y}%; --size:{Random.Shared.Next(15, 25)}vw; --tilt:{Random.Shared.Next(-12, 13)}deg; --depth:{Random.Shared.Next(-200, 51)}px; --travel-z:{Random.Shared.Next(45, 111)}px; --turn:{Random.Shared.Next(-12, 13)}deg; --scale-from:{scale}; --scale-to:{scale + Random.Shared.Next(14, 29)}; --travel-x:{Random.Shared.Next(-25, 26)}px; --travel-y:{Random.Shared.Next(-30, 31)}px; animation-duration:{Random.Shared.Next(18, 31)}s; animation-delay:-{Random.Shared.Next(0, 30)}s; }}";
        }));
        var imageOrigins = string.Join(" ", sprites.Select(path => Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == "https" ? uri.GetLeftPart(UriPartial.Authority) : "").Distinct());
        // Consent receives only the callback already validated by OpenIddict.
        var callbackOrigin = Uri.TryCreate(formRedirect, UriKind.Absolute, out var callback) && callback.Scheme is "http" or "https"
            ? callback.GetLeftPart(UriPartial.Authority) : "";
        context.Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'self' 'nonce-{Nonce}'; script-src 'nonce-{Nonce}'; img-src 'self' {imageOrigins}; font-src 'self'; form-action 'self' https://accounts.google.com {callbackOrigin}; frame-ancestors 'none'; base-uri 'none'";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        Sprites = sprites;
        SpriteStyles = variation;
    }
}
