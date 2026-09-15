using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceBooking.Api.Converters;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
  private const string Format = "HH:mm";

  public override TimeOnly Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options)
  {
    var value = reader.GetString();

    if (string.IsNullOrWhiteSpace(value))
    {
      throw new JsonException("Time value is required.");
    }

    if (TimeOnly.TryParseExact(
      value,
      Format,
      CultureInfo.InvariantCulture,
      DateTimeStyles.None,
      out var result))
    {
      return result;
    }

    throw new JsonException(
        $"Invalid time format '{value}'. Expected HH:mm.");
  }

  public override void Write(
      Utf8JsonWriter writer,
      TimeOnly value,
      JsonSerializerOptions options)
  {
    writer.WriteStringValue(
        value.ToString(Format, CultureInfo.InvariantCulture));
  }
}