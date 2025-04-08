namespace Echo.Telegram;

using System;
using System.Buffers.Text;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a unique identifier for a user.
/// </summary>
/// <remarks>
/// The <see cref="UserId"/> type is used to uniquely identify objects within the system.
/// </remarks>
[TypeConverter(typeof(UserIdTypeConverter)), JsonConverter(typeof(UserIdJsonConverter))]
public readonly struct UserId : IEquatable<UserId>, IComparable, IComparable<UserId>,
    ISpanFormattable, IUtf8SpanFormattable, ISpanParsable<UserId>, IUtf8SpanParsable<UserId>
{
    private readonly long value;

    private UserId(long value)
    {
        this.value = value;
    }

    public override int GetHashCode() => this.value.GetHashCode();

    public override string ToString() => this.value.ToString();

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is UserId id && Equals(id);

    public bool Equals(UserId other) => this.value.Equals(other.value);

    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;
        if (obj is not UserId other)
            throw new ArgumentException($"Object must be of type {nameof(UserId)}.", nameof(obj));

        return CompareTo(other);
    }

    public int CompareTo(UserId other) => this.value.CompareTo(other.value);

    public string ToString(string? format, IFormatProvider? formatProvider) => this.value.ToString(format, formatProvider);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.value.TryFormat(destination, out charsWritten, format);

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.value.TryFormat(utf8Destination, out bytesWritten, format);

    public static UserId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException();

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out UserId result)
    {
        if (long.TryParse(s, provider, out var value))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public static UserId Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out UserId result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static UserId Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        TryParse(utf8Text, provider, out var id) ? id : throw new FormatException();

    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out UserId result)
    {
        if (Utf8Parser.TryParse(utf8Text, out long value, out _))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public static bool operator ==(UserId left, UserId right) => left.Equals(right);
    public static bool operator !=(UserId left, UserId right) => !(left == right);
    public static bool operator >(UserId left, UserId right) => left.CompareTo(right) > 0;
    public static bool operator <(UserId left, UserId right) => left.CompareTo(right) < 0;
    public static bool operator >=(UserId left, UserId right) => left.CompareTo(right) >= 0;
    public static bool operator <=(UserId left, UserId right) => left.CompareTo(right) <= 0;
    public static implicit operator UserId(long guid) => new(guid);
    public static explicit operator long(UserId id) => id.value;
    public static explicit operator ChatId(UserId id) => id.value;

    public static UserId ConvertFrom(object value, CultureInfo? culture = null) =>
        value switch
        {
            byte[] span when Utf8Parser.TryParse(span, out long guid, out _) => new UserId(guid),
            char[] span when long.TryParse(span, culture, out var guid) => new UserId(guid),
            string str when long.TryParse(str, culture, out var guid) => new UserId(guid),
            long guid => new UserId(guid),
            not null when long.TryParse(value.ToString(), culture, out var guid) => new UserId(guid),
            _ => throw new NotSupportedException()
        };

    private sealed class UserIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(byte[]) || sourceType == typeof(char[]) ||
            sourceType == typeof(string) || sourceType == typeof(long);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            UserId.ConvertFrom(value, culture);
    }

    private sealed class UserIdJsonConverter : JsonConverter<UserId>
    {
        public override UserId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetInt64());

        public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.value);
    }
}