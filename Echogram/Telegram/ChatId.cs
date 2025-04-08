namespace Echo.Telegram;

using System;
using System.Buffers.Text;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a unique identifier for a chat.
/// </summary>
/// <remarks>
/// The <see cref="ChatId"/> type is used to uniquely identify objects within the system.
/// </remarks>
[TypeConverter(typeof(ChatIdTypeConverter)), JsonConverter(typeof(ChatIdJsonConverter))]
public readonly struct ChatId : IEquatable<ChatId>, IComparable, IComparable<ChatId>,
    ISpanFormattable, IUtf8SpanFormattable, ISpanParsable<ChatId>, IUtf8SpanParsable<ChatId>
{
    private readonly long value;

    private ChatId(long value)
    {
        this.value = value;
    }

    public override int GetHashCode() => this.value.GetHashCode();

    public override string ToString() => this.value.ToString();

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ChatId id && Equals(id);

    public bool Equals(ChatId other) => this.value.Equals(other.value);

    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;
        if (obj is not ChatId other)
            throw new ArgumentException($"Object must be of type {nameof(ChatId)}.", nameof(obj));

        return CompareTo(other);
    }

    public int CompareTo(ChatId other) => this.value.CompareTo(other.value);

    public string ToString(string? format, IFormatProvider? formatProvider) => this.value.ToString(format, formatProvider);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.value.TryFormat(destination, out charsWritten, format);

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.value.TryFormat(utf8Destination, out bytesWritten, format);

    public static ChatId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException();

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out ChatId result)
    {
        if (long.TryParse(s, provider, out var value))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public static ChatId Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ChatId result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static ChatId Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        TryParse(utf8Text, provider, out var id) ? id : throw new FormatException();

    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out ChatId result)
    {
        if (Utf8Parser.TryParse(utf8Text, out long value, out _))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public static bool operator ==(ChatId left, ChatId right) => left.Equals(right);
    public static bool operator !=(ChatId left, ChatId right) => !(left == right);
    public static bool operator >(ChatId left, ChatId right) => left.CompareTo(right) > 0;
    public static bool operator <(ChatId left, ChatId right) => left.CompareTo(right) < 0;
    public static bool operator >=(ChatId left, ChatId right) => left.CompareTo(right) >= 0;
    public static bool operator <=(ChatId left, ChatId right) => left.CompareTo(right) <= 0;
    public static implicit operator ChatId(long guid) => new(guid);
    public static explicit operator long(ChatId id) => id.value;

    public static ChatId ConvertFrom(object value, CultureInfo? culture = null) =>
        value switch
        {
            byte[] span when Utf8Parser.TryParse(span, out long guid, out _) => new ChatId(guid),
            char[] span when long.TryParse(span, culture, out var guid) => new ChatId(guid),
            string str when long.TryParse(str, culture, out var guid) => new ChatId(guid),
            long guid => new ChatId(guid),
            not null when long.TryParse(value.ToString(), culture, out var guid) => new ChatId(guid),
            _ => throw new NotSupportedException()
        };

    private sealed class ChatIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(byte[]) || sourceType == typeof(char[]) ||
            sourceType == typeof(string) || sourceType == typeof(Guid);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            ChatId.ConvertFrom(value, culture);
    }

    private sealed class ChatIdJsonConverter : JsonConverter<ChatId>
    {
        public override ChatId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetInt64());

        public override void Write(Utf8JsonWriter writer, ChatId value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.value);
    }
}