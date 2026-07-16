using System.Text.Json;

namespace SPINbuster.Application.Internal;

internal static class CanonicalJson
{
  public static string Canonicalize(JsonElement element)
  {
    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream))
    {
      WriteCanonicalElement(writer, element);
    }

    return System.Text.Encoding.UTF8.GetString(stream.ToArray());
  }

  private static void WriteCanonicalElement(Utf8JsonWriter writer, JsonElement element)
  {
    switch (element.ValueKind)
    {
      case JsonValueKind.Object:
        writer.WriteStartObject();
        foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
        {
          writer.WritePropertyName(property.Name);
          WriteCanonicalElement(writer, property.Value);
        }
        writer.WriteEndObject();
        break;

      case JsonValueKind.Array:
        writer.WriteStartArray();
        foreach (var item in element.EnumerateArray())
        {
          WriteCanonicalElement(writer, item);
        }
        writer.WriteEndArray();
        break;

      default:
        element.WriteTo(writer);
        break;
    }
  }
}
