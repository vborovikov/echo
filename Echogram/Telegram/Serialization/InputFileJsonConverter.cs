namespace Echo.Telegram.Serialization;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Converts <see cref="InputFile"/> to and from a string.
/// </summary>
/// <remarks>
/// A file can be identified by an existing file identifier or an external URL.
/// A file with content to upload can't be represented as a string.
/// </remarks>
public class InputFileJsonConverter : JsonConverter<InputFile>
{
    /// <inheritdoc/>
    public override InputFile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new JsonException($"Expected a string value for {nameof(InputFile)}.");
        return IsUrl(value)
            ? new InputFile { ExternalUrl = value }
            : new InputFile { FileId = value };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, InputFile value, JsonSerializerOptions options)
    {
        if (value.Content is not null)
        {
            throw new NotSupportedException($"Can't serialize an {nameof(InputFile)} with content to a string.");
        }

        writer.WriteStringValue(value.FileId ?? value.ExternalUrl);
    }

    private static bool IsUrl(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
