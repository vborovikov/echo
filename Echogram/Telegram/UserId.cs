namespace Echo.Telegram;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity = long;

/// <summary>
/// Represents an unique identifier for a Telegram user.
/// </summary>
/// <remarks>
/// The <see cref="UserId"/> type is used to uniquely identify users within the system.
/// </remarks>
[TypeConverter(typeof(UserIdTypeConverter)), JsonConverter(typeof(UserIdJsonConverter))]
public readonly struct UserId : IEquatable<UserId>, IComparable, IComparable<UserId>,
    ISpanFormattable, IUtf8SpanFormattable, ISpanParsable<UserId>, IUtf8SpanParsable<UserId>,
    IEqualityOperators<UserId, UserId, bool>, IComparisonOperators<UserId, UserId, bool>
{
    private readonly Identity identity;

    private UserId(Identity value)
    {
        this.identity = value;
    }

    /// <inheritdoc />
    public override int GetHashCode() => this.identity.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => this.identity.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is UserId id && Equals(id);

    /// <inheritdoc />
    public bool Equals(UserId other) => this.identity.Equals(other.identity);

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;
        if (obj is not UserId other)
            throw new ArgumentException($"Object must be of type {nameof(UserId)}.", nameof(obj));

        return CompareTo(other);
    }

    /// <inheritdoc />
    public int CompareTo(UserId other) => this.identity.CompareTo(other.identity);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => this.identity.ToString(format, formatProvider);

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.identity.TryFormat(destination, out charsWritten, format, provider);

    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        this.identity.TryFormat(utf8Destination, out bytesWritten, format, provider);

    /// <inheritdoc cref="Parse(ReadOnlySpan{char}, IFormatProvider?)" />
    public static UserId Parse(ReadOnlySpan<char> s) => Parse(s, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out UserId)" />
    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out UserId result) => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc cref="Parse(string, IFormatProvider?)" />
    public static UserId Parse(string s) => Parse(s, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(string, IFormatProvider?, out UserId)" />
    public static bool TryParse(string s, [MaybeNullWhen(false)] out UserId result) => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc cref="Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
    public static UserId Parse(ReadOnlySpan<byte> utf8Text) => Parse(utf8Text, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="TryParse(ReadOnlySpan{byte}, IFormatProvider?, out UserId)" />
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, [MaybeNullWhen(false)] out UserId result) => TryParse(utf8Text, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc />
    public static UserId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out UserId result)
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
    public static UserId Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out UserId result) =>
        TryParse(s.AsSpan(), provider, out result);

    /// <inheritdoc />
    public static UserId Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        TryParse(utf8Text, provider, out var id) ? id : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out UserId result)
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
    public static bool operator ==(UserId left, UserId right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(UserId left, UserId right) => !(left == right);

    /// <inheritdoc />
    public static bool operator >(UserId left, UserId right) => left.CompareTo(right) > 0;

    /// <inheritdoc />
    public static bool operator <(UserId left, UserId right) => left.CompareTo(right) < 0;

    /// <inheritdoc />
    public static bool operator >=(UserId left, UserId right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public static bool operator <=(UserId left, UserId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Implicitly converts the specified <see cref="Identity"/> to a UserId
    /// </summary>
    /// <param name="id">The <see cref="Identity"/> to convert.</param>
    /// <returns>A new instance of the <see cref="UserId"/> type.</returns>
    public static implicit operator UserId(Identity id) => new(id);

    /// <summary>
    /// Explicitly converts the specified <see cref="UserId"/> to an <see cref="Identity"/>.
    /// </summary>
    /// <param name="id">The <see cref="UserId"/> to convert.</param>
    /// <returns>The <see cref="Identity"/> that was converted.</returns>
    public static explicit operator Identity(UserId id) => id.identity;

    /// <summary>
    /// Explicitly converts the specified <see cref="UserId"/> to an <see cref="ChatId"/>.
    /// </summary>
    /// <param name="id">The <see cref="UserId"/> to convert.</param>
    /// <returns>The <see cref="ChatId"/> that was converted.</returns>
    public static explicit operator ChatId(UserId id) => id.identity;

    /// <summary>
    /// Converts the specified object to a <see cref="UserId"/>.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <param name="culture">The culture to use in the conversion.</param>
    /// <returns>A <see cref="UserId"/> that represents the converted object.</returns>
    public static UserId ConvertFrom(object value, CultureInfo? culture = null) =>
        value switch
        {
            byte[] span when Identity.TryParse(span, culture, out Identity id) => new UserId(id),
            char[] span when Identity.TryParse(span, culture, out var id) => new UserId(id),
            string str when Identity.TryParse(str, culture, out var id) => new UserId(id),
            Identity id => new UserId(id),
            not null when Identity.TryParse(value.ToString(), culture, out var id) => new UserId(id),
            _ => throw new NotSupportedException()
        };

    private sealed class UserIdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            sourceType == typeof(byte[]) || sourceType == typeof(char[]) ||
            sourceType == typeof(string) || sourceType == typeof(Identity);

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            UserId.ConvertFrom(value, culture);
    }

    private sealed class UserIdJsonConverter : JsonConverter<UserId>
    {
        public override UserId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetInt64());

        public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.identity);
    }
}