using System.Text.Json;
using System.Text.Json.Serialization;

namespace FortniteSpriteTracker.Shared.Profiles;

[JsonConverter(typeof(ThemePreferenceJsonConverter))]
public enum ThemePreference
{
    System,
    Light,
    Dark
}

public static class ThemePreferenceExtensions
{
    public static string ToStorageValue(this ThemePreference preference) => preference switch
    {
        ThemePreference.System => "system",
        ThemePreference.Light => "light",
        ThemePreference.Dark => "dark",
        _ => throw new ArgumentOutOfRangeException(nameof(preference), preference, null)
    };

    public static bool TryParse(string? value, out ThemePreference preference)
    {
        preference = value?.Trim().ToLowerInvariant() switch
        {
            "system" => ThemePreference.System,
            "light" => ThemePreference.Light,
            "dark" => ThemePreference.Dark,
            _ => ThemePreference.System
        };

        return value?.Trim().ToLowerInvariant() is "system" or "light" or "dark";
    }

    public static ThemePreference Parse(string value) =>
        TryParse(value, out var preference)
            ? preference
            : throw new ArgumentException("Theme preference must be system, light, or dark.", nameof(value));
}

public sealed class ThemePreferenceJsonConverter : JsonConverter<ThemePreference>
{
    public override ThemePreference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && ThemePreferenceExtensions.TryParse(reader.GetString(), out var preference))
        {
            return preference;
        }

        throw new JsonException("Theme preference must be system, light, or dark.");
    }

    public override void Write(Utf8JsonWriter writer, ThemePreference value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToStorageValue());
}
