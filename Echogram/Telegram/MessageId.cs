namespace Echo.Telegram;

using System;
using System.Buffers.Text;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a unique identifier for a message.
/// </summary>
/// <remarks>
/// The <see cref="MessageId"/> type is used to uniquely identify objects within the system.
/// </remarks>
[TypeConverter(typeof(MessageIdTypeConverter)), JsonConverter(typeof(MessageIdJsonConverter))]
public readonly struct MessageId : IEquatable<MessageId>, IComparable, IComparable<MessageId>,
    ISpanFormattable, IUtf8SpanFormattable, ISpanParsable<MessageId>, IUtf8SpanParsable<MessageId>
{
    private readonly int value;

    private MessageId(int value)
    {
        this.value = value;
    }

    public override int GetHashCode() => this.value.GetHashCode();

    public override string ToString() => this.value.ToString();

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is MessageId id && Equals(id);

    public bool Equals(MessageId other) => this.value.Equals(other.value);

    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;
        if (obj is not MessageId other)
            throw new ArgumentException($"Object must be of type {nameof(MessageId)}.", nameof(obj));

        return CompareTo(other);
    }

    public int CompareTo(MessageId other) => this.value.CompareTo(other.value);

    public string ToString(string? format, IFormatProvider? formatProvider) => this.value.ToString(format, formatProvider);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.value.TryFormat(destination, out charsWritten, format);

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.value.TryFormat(utf8Destination, out bytesWritten, format);

    public static MessageId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException();

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out MessageId result)
    {
        if (int.TryParse(s, provider, out var value))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public static MessageId Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out MessageId result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static MessageId Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        TryParse(utf8Text, provider, out var id) ? id : throw new FormatException();

    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out MessageId result)
    {
        if (Utf8Parser.TryParse(utf8Text, out int value, out _))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);
    public static bool operator !=(MessageId left, MessageId right) => !(left == right);
    public static bool operator >(MessageId left, MessageId right) => left.CompareTo(right) > 0;
    public static bool operator <(MessageId left, MessageId right) => left.CompareTo(right) < 0;
    public static bool operator >=(MessageId left, MessageId right) => left.CompareTo(right) >= 0;
    public static bool operator <=(MessageId left, MessageId right) => left.CompareTo(right) <= 0;
    public static implicit operator MessageId(int guid) => new(guid);
    public static explicit operator int(MessageId id) => id.value;

    public static MessageId ConvertFrom(object value, CultureInfo? culture = null) =>
        value switch
        {
            byte[] span when Utf8Parser.TryParse(span, out int guid, out _) => new MessageId(guid),
            char[] span when int.TryParse(span, culture, out var guid) => new MessageId(guid),
            string str when int.TryParse(str, culture, out var guid) => new MessageId(guid),
            int guid => new MessageId(guid),
            not null when int.TryParse(value.ToString(), culture, out var guid) => new MessageId(guid),
            _ => throw new NotSupportedException()
        };

    private sealed class MessageIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(byte[]) || sourceType == typeof(char[]) ||
            sourceType == typeof(string) || sourceType == typeof(int);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            MessageId.ConvertFrom(value, culture);
    }

    private sealed class MessageIdJsonConverter : JsonConverter<MessageId>
    {
        public override MessageId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetInt32());

        public override void Write(Utf8JsonWriter writer, MessageId value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.value);
    }
}