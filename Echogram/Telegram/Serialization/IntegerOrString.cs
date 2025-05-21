namespace Echo.Telegram.Serialization;

using System;
using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Unicode;

readonly struct IntegerOrString<TInteger>
    where TInteger : struct, INumber<TInteger>
{
    private readonly TInteger num;
    private readonly string? str;

    public IntegerOrString(TInteger value)
    {
        this.num = value;
        this.str = null;
    }

    public IntegerOrString(string? value)
    {
        this.num = default;
        this.str = value ?? string.Empty;
    }

    public bool IsInteger => this.str is null;

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            TInteger otherNum => this.IsInteger && this.num.Equals(otherNum),
            string otherStr => this.str?.Equals(otherStr, StringComparison.OrdinalIgnoreCase) == true,
            IntegerOrString<TInteger> other => CompareTo(other) == 0,
            _ => false,
        };
    }

    public override int GetHashCode() => HashCode.Combine(this.num, this.str);

    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? provider) => this.str ?? this.num.ToString(format, provider);

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (this.str is null)
            return this.num.TryFormat(destination, out charsWritten, format, provider);

        charsWritten = this.str.Length;
        return this.str.TryCopyTo(destination);
    }

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (this.str is null)
            return this.num.TryFormat(utf8Destination, out bytesWritten, format, provider);

        return Utf8.FromUtf16(this.str, utf8Destination, out _, out bytesWritten) == OperationStatus.Done;
    }

    public static bool TryParse(ReadOnlySpan<char> span, IFormatProvider? provider, out IntegerOrString<TInteger> value)
    {
        if (TInteger.TryParse(span, NumberStyles.Integer, provider, out TInteger result))
        {
            value = new(result);
            return true;
        }
        else if (span.Length > 0)
        {
            value = new(span.ToString());
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryParse(ReadOnlySpan<byte> utf8, IFormatProvider? provider, out IntegerOrString<TInteger> value)
    {
        if (TInteger.TryParse(utf8, NumberStyles.Integer, provider, out TInteger result))
        {
            value = new(result);
            return true;
        }
        else if (utf8.Length > 0)
        {
            value = new(Encoding.UTF8.GetString(utf8));
            return true;
        }

        value = default;
        return false;
    }

    public int CompareTo(IntegerOrString<TInteger> other)
    {
        if (this.IsInteger && other.IsInteger)
        {
            return this.num.CompareTo(other.num);
        }
        else if (this.IsInteger)
        {
            return 1; // integer is greater than string
        }
        else if (other.IsInteger)
        {
            return -1; // string is less than integer
        }
        else
        {
            return string.Compare(this.str, other.str, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static implicit operator IntegerOrString<TInteger>(TInteger num) => new(num);
    public static implicit operator IntegerOrString<TInteger>(string? str) => new(str);
    public static explicit operator TInteger(IntegerOrString<TInteger> value) => value.num;
    public static explicit operator string(IntegerOrString<TInteger> value) => value.str ?? string.Empty;
}
