namespace SpriteScout.Auth;

public enum AuthPageKind { WebsiteLogin, Login, Error, SignedIn, Consent }

public sealed record AuthPageModel(AuthPageKind Kind)
{
    public string? Application { get; init; }
    public AuthFormModel? Form { get; init; }
    public bool Available { get; init; } = true;
    public bool Offline { get; init; }
    public bool Expired { get; init; }
    public string? Retry { get; init; }

    public string Title => Kind switch
    {
        AuthPageKind.Error => "Sign-in needs another try",
        AuthPageKind.SignedIn => "Signed in to Sprite Scout",
        AuthPageKind.Consent => "Connect to Sprite Scout",
        _ => "Sign in to Sprite Scout"
    };

    public string Heading => Kind switch
    {
        AuthPageKind.WebsiteLogin => "Your next find starts here.",
        AuthPageKind.Error => "Let's try that again",
        AuthPageKind.SignedIn => "You're signed in",
        AuthPageKind.Consent => "Approve your connection",
        _ => "Welcome to Sprite Scout"
    };
}

public sealed record AuthFormField(string Name, string Value);
public sealed record AuthFormModel(string Action, IReadOnlyList<AuthFormField> Fields, bool Consent);
