using System.Text.Json;
using System.Text.Json.Serialization;

namespace Premag.API.Json;

public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (TimeOnly.TryParse(raw, out var hora))
            return hora;
        throw new JsonException("Horário inválido.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm"));
}

public sealed class NullableTimeOnlyJsonConverter : JsonConverter<TimeOnly?>
{
    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (TimeOnly.TryParse(raw, out var hora))
            return hora;
        throw new JsonException("Horário inválido.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value.ToString("HH:mm"));
    }
}
