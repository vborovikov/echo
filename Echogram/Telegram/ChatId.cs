namespace Echo.Telegram;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity = Serialization.IntegerOrString<long>;

/// <summary>
/// Represents an unique identifier for a Telegram chat.
/// </summary>
/// <remarks>
/// The <see cref="ChatId"/> type is used to uniquely identify chats within the system.
/// </remarks>
[TypeConverter(typeof(ChatIdTypeConverter)), JsonConverter(typeof(ChatIdJsonConverter))]
public readonly struct ChatId : IEquatable<ChatId>, IComparable, IComparable<ChatId>,
    ISpanFormattable, IUtf8SpanFormattable, ISpanParsable<ChatId>, IUtf8SpanParsable<ChatId>,
    IEqualityOperators<ChatId, ChatId, bool>, IComparisonOperators<ChatId, ChatId, bool>
{
    private readonly Identity identity;

    private ChatId(Identity value)
    {
        this.identity = value;
    }

    /// <inheritdoc />
    public override int GetHashCode() => this.identity.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ChatId id && Equals(id);

    /// <inheritdoc />
    public bool Equals(ChatId other) => this.identity.Equals(other.identity);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;
        if (obj is not ChatId other)
            throw new ArgumentException($"Object must be of type {nameof(ChatId)}.", nameof(obj));

        return CompareTo(other);
    }

    /// <inheritdoc />
    public int CompareTo(ChatId other) => this.identity.CompareTo(other.identity);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var str = this.identity.ToString(format, formatProvider);

        if (!this.identity.IsInteger && !str.StartsWith('@'))
        {
            return '@' + str;
        }

        return str;
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.identity.TryFormat(destination, out charsWritten, format, provider);

    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.identity.TryFormat(utf8Destination, out bytesWritten, format, provider);

    /// <inheritdoc cref="Parse(ReadOnlySpan{char}, IFormatProvider?)" />
    public static ChatId Parse(ReadOnlySpan<char> s) => Parse(s, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out ChatId)" />
    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out ChatId result) => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc cref="Parse(string, IFormatProvider?)" />
    public static ChatId Parse(string s) => Parse(s, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(string, IFormatProvider?, out ChatId)" />
    public static bool TryParse(string s, [MaybeNullWhen(false)] out ChatId result) => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc cref="Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
    public static ChatId Parse(ReadOnlySpan<byte> utf8Text) => Parse(utf8Text, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{byte}, IFormatProvider?, out ChatId)" />
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, [MaybeNullWhen(false)] out ChatId result) => TryParse(utf8Text, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc />
    public static ChatId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out ChatId result)
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
    public static ChatId Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ChatId result) =>
        TryParse(s.AsSpan(), provider, out result);

    /// <inheritdoc />
    public static ChatId Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        TryParse(utf8Text, provider, out var id) ? id : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out ChatId result)
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
    public static bool operator ==(ChatId left, ChatId right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(ChatId left, ChatId right) => !(left == right);

    /// <inheritdoc />
    public static bool operator >(ChatId left, ChatId right) => left.CompareTo(right) > 0;

    /// <inheritdoc />
    public static bool operator <(ChatId left, ChatId right) => left.CompareTo(right) < 0;

    /// <inheritdoc />
    public static bool operator >=(ChatId left, ChatId right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public static bool operator <=(ChatId left, ChatId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Implicitly converts the specified <see cref="long">integer</see> to a <see cref="ChatId"/>
    /// </summary>
    /// <param name="id">The <see cref="long">integer</see> to convert.</param>
    /// <returns>A new instance of the <see cref="ChatId"/> type.</returns>
    public static implicit operator ChatId(long id) => new(id);

    /// <summary>
    /// Explicitly converts the specified <see cref="ChatId"/> to an <see cref="long">integer</see>.
    /// </summary>
    /// <param name="id">The <see cref="ChatId"/> to convert.</param>
    /// <returns>The <see cref="long">integer</see> that was converted.</returns>
    public static explicit operator long(ChatId id) => (long)id.identity;

    /// <summary>
    /// Converts the specified object to a <see cref="ChatId"/>.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <param name="culture">The culture to use in the conversion.</param>
    /// <returns>A <see cref="ChatId"/> that represents the converted object.</returns>
    public static ChatId ConvertFrom(object value, CultureInfo? culture = null) =>
        value switch
        {
            long id => new ChatId(id),
            byte[] span when Identity.TryParse(span, culture, out Identity id) => new ChatId(id),
            char[] span when Identity.TryParse(span, culture, out var id) => new ChatId(id),
            string str when Identity.TryParse(str, culture, out var id) => new ChatId(id),
            Identity id => new ChatId(id),
            not null when Identity.TryParse(value.ToString(), culture, out var id) => new ChatId(id),
            _ => throw new NotSupportedException()
        };

    private sealed class ChatIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(byte[]) || sourceType == typeof(char[]) || sourceType == typeof(long) ||
            sourceType == typeof(string) || sourceType == typeof(Identity);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            ChatId.ConvertFrom(value, culture);
    }

    private sealed class ChatIdJsonConverter : JsonConverter<ChatId>
    {
        public override ChatId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.TokenType == JsonTokenType.Number ? reader.GetInt64() : reader.GetString());

        public override void Write(Utf8JsonWriter writer, ChatId value, JsonSerializerOptions options)
        {
            if (value.identity.IsInteger)
            {
                writer.WriteNumberValue((long)value.identity);
            }
            else
            {
                writer.WriteStringValue((string)value.identity);
            }
        }
    }
}