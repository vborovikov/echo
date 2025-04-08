namespace Echo.Telegram.Serialization;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Converter to convert enums to and from string arrays.
/// </summary>
/// <remarks>
/// Reading is case insensitive, writing can be customized via a <see cref="JsonNamingPolicy" />.
/// </remarks>
public class JsonStringArrayEnumConverter : JsonConverterFactory
{
    private readonly JsonNamingPolicy? namingPolicy;

    public JsonStringArrayEnumConverter() : this(namingPolicy: null) { }

    public JsonStringArrayEnumConverter(JsonNamingPolicy? namingPolicy = null)
    {
        this.namingPolicy = namingPolicy;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsEnum && typeToConvert.IsDefined(typeof(FlagsAttribute), false);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(typeToConvert.IsEnum, true);

        var converterType = typeof(JsonStringArrayEnumConverter<>).MakeGenericType(typeToConvert);
        if (Activator.CreateInstance(converterType, [this.namingPolicy]) is JsonConverter converter)
        {
            return converter;
        }

        throw new NotSupportedException();
    }
}

/// <summary>
/// Lame and slow converter to convert enums to and from string arrays.
/// </summary>
/// <typeparam name="TEnum">The type of the enum.</typeparam>
sealed class JsonStringArrayEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly JsonNamingPolicy? namingPolicy;

    public JsonStringArrayEnumConverter(JsonNamingPolicy? namingPolicy = null)
    {
        this.namingPolicy = namingPolicy;
    }

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return default;

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var enumNames = new StringBuilder();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
                    return (TEnum)Enum.Parse(underlyingType ?? typeToConvert, enumNames.ToString());
                }
                else if (reader.TokenType == JsonTokenType.String && reader.GetString() is string enumValue)
                {
                    if (enumNames.Length > 0) enumNames.Append(',');
                    enumNames.Append(JsonNamingPolicy.CamelCase.ConvertName(enumValue));
                }
                else
                {
                    throw new JsonException();
                }
            }
        }

        throw new JsonException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        if (!value.Equals(default(TEnum)))
        {
            foreach (var enumName in value.ToString().Split(','))
            {
                writer.WriteStringValue(this.namingPolicy?.ConvertName(enumName) ?? enumName);
            }
        }
        writer.WriteEndArray();
    }
}