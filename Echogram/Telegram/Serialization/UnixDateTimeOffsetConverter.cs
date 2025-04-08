namespace Echo.Telegram.Serialization;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Converts Unix time to nullable DateTimeOffset.
/// </summary>
public class UnixDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// Minimum Unix time in seconds.
    /// </summary>
    private static readonly long s_unixMinSeconds = DateTimeOffset.MinValue.ToUnixTimeSeconds();

    /// <summary>
    /// Maximum Unix time in seconds.
    /// </summary>
    private static readonly long s_unixMaxSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    /// <summary>
    /// Determines if the time should be formatted as seconds. False if resolved as milliseconds.
    /// </summary>
    public bool? FormatAsSeconds { get; init; } = true;

    /// <inheritdoc/>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return default;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var time))
        {
            // If FormatAsSeconds is not specified, the correct type is derived depending on whether
            //    the value can be represented as seconds within the .NET DateTimeOffset min/max range 0001-1-1 to 9999-12-31.

            // Since this is a 64-bit value, the Unixtime in seconds may exceed
            //    the 32-bit min/max restrictions 1/1/1970-1-1 to 1/19/2038-1-19.
            if (this.FormatAsSeconds == true || (time > s_unixMinSeconds && time < s_unixMaxSeconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(time);
            }

            return DateTimeOffset.FromUnixTimeMilliseconds(time);
        }

        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(this.FormatAsSeconds == true ? value.ToUnixTimeSeconds() : value.ToUnixTimeMilliseconds());
    }
}