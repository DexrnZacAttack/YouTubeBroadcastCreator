using System.Text.Json;
using System.Text.Json.Serialization;

namespace YouTubeBroadcastCreator.Core.Util.Serialization;

public class FileInfoJsonConverter : JsonConverter<FileInfo>
{
    public override FileInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? path = reader.GetString();
        if (path == null)
            return null;
        
        return new FileInfo(path);
    }

    public override void Write(Utf8JsonWriter writer, FileInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.FullName);
    }
    
}