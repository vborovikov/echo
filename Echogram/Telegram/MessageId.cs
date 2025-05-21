namespace Echo.Telegram;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity = int;

/// <summary>
/// Represents an unique identifier for a Telegram message.
/// </summary>
/// <remarks>
/// The <see cref="MessageId"/> type is used to uniquely identify messages within the system.
/// </remarks>
[TypeConverter(typeof(MessageIdTypeConverter)), JsonConverter(typeof(MessageIdJsonConverter))]
public readonly struct MessageId : IEquatable<MessageId>, IComparable, IComparable<MessageId>,
    ISpanFormattable, IUtf8SpanFormattable, ISpanParsable<MessageId>, IUtf8SpanParsable<MessageId>,
    IEqualityOperators<MessageId, MessageId, bool>, IComparisonOperators<MessageId, MessageId, bool>
{
    private readonly Identity identity;

    private MessageId(Identity value)
    {
        this.identity = value;
    }

    /// <inheritdoc />
    public override int GetHashCode() => this.identity.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => this.identity.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is MessageId id && Equals(id);

    /// <inheritdoc />
    public bool Equals(MessageId other) => this.identity.Equals(other.identity);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;
        if (obj is not MessageId other)
            throw new ArgumentException($"Object must be of type {nameof(MessageId)}.", nameof(obj));

        return CompareTo(other);
    }

    /// <inheritdoc />
    public int CompareTo(MessageId other) => this.identity.CompareTo(other.identity);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => this.identity.ToString(format, formatProvider);

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.identity.TryFormat(destination, out charsWritten, format, provider);

    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.identity.TryFormat(utf8Destination, out bytesWritten, format, provider);

    /// <inheritdoc cref="Parse(ReadOnlySpan{char}, IFormatProvider?)" />
    public static MessageId Parse(ReadOnlySpan<char> s) => Parse(s, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out MessageId)" />
    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out MessageId result) => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc cref="Parse(string, IFormatProvider?)" />
    public static MessageId Parse(string s) => Parse(s, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(string, IFormatProvider?, out MessageId)" />
    public static bool TryParse(string s, [MaybeNullWhen(false)] out MessageId result) => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc cref="Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
    public static MessageId Parse(ReadOnlySpan<byte> utf8Text) => Parse(utf8Text, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{byte}, IFormatProvider?, out MessageId)" />
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, [MaybeNullWhen(false)] out MessageId result) => TryParse(utf8Text, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc />
    public static MessageId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out MessageId result)
    {
        if (Identity.TryParse(s, provider, out var value))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public static MessageId Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out MessageId result) =>
        TryParse(s.AsSpan(), provider, out result);

    /// <inheritdoc />
    public static MessageId Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        TryParse(utf8Text, provider, out var id) ? id : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out MessageId result)
    {
        if (Identity.TryParse(utf8Text, provider, out Identity value))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(MessageId left, MessageId right) => !(left == right);

    /// <inheritdoc />
    public static bool operator >(MessageId left, MessageId right) => left.CompareTo(right) > 0;

    /// <inheritdoc />
    public static bool operator <(MessageId left, MessageId right) => left.CompareTo(right) < 0;

    /// <inheritdoc />
    public static bool operator >=(MessageId left, MessageId right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public static bool operator <=(MessageId left, MessageId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Implicitly converts the specified <see cref="Identity"/> to a MessageId
    /// </summary>
    /// <param name="id">The <see cref="Identity"/> to convert.</param>
    /// <returns>A new instance of the <see cref="MessageId"/> type.</returns>
    public static implicit operator MessageId(Identity id) => new(id);

    /// <summary>
    /// Explicitly converts the specified <see cref="MessageId"/> to an <see cref="Identity"/>.
    /// </summary>
    /// <param name="id">The <see cref="MessageId"/> to convert.</param>
    /// <returns>The <see cref="Identity"/> that was converted.</returns>
    public static explicit operator Identity(MessageId id) => id.identity;

    /// <summary>
    /// Converts the specified object to a <see cref="MessageId"/>.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <param name="culture">The culture to use in the conversion.</param>
    /// <returns>A <see cref="MessageId"/> that represents the converted object.</returns>
    public static MessageId ConvertFrom(object value, CultureInfo? culture = null) =>
        value switch
        {
            byte[] span when Identity.TryParse(span, culture, out Identity id) => new MessageId(id),
            char[] span when Identity.TryParse(span, culture, out var id) => new MessageId(id),
            string str when Identity.TryParse(str, culture, out var id) => new MessageId(id),
            Identity id => new MessageId(id),
            not null when Identity.TryParse(value.ToString(), culture, out var id) => new MessageId(id),
            _ => throw new NotSupportedException()
        };

    private sealed class MessageIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(byte[]) || sourceType == typeof(char[]) ||
            sourceType == typeof(string) || sourceType == typeof(Identity);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            MessageId.ConvertFrom(value, culture);
    }

    private sealed class MessageIdJsonConverter : JsonConverter<MessageId>
    {
        public override MessageId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetInt32());

        public override void Write(Utf8JsonWriter writer, MessageId value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.identity);
    }
}