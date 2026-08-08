using System.ComponentModel;
using System.Text.Json.Serialization;
using Google.Apis.YouTube.v3.Data;
using YouTubeBroadcastCreator.Core.API.Extensions;
using YouTubeBroadcastCreator.Core.Types.Stream;

namespace YouTubeBroadcastCreator.Core.Types.Broadcast;

public record ContentDeliverySettings
{
    public const string Variable = "variable";

    [JsonPropertyName("ingestion"), JsonRequired, Description("Ingestion type, such as RTSP, HLS"),
     JsonConverter(typeof(JsonStringEnumConverter<IngestionType>))]
    public IngestionType IngestionType { get; init; } = IngestionType.RTMP;

    [JsonPropertyName("resolution"),
     Description($"Expected stream resolution, use 'variable' to auto detect. Default: \"{Variable}\"")]
    public string Resolution { get; init; } = Variable;

    [JsonPropertyName("frame_rate"),
     Description($"Expected stream FPS, use 'variable' to auto detect. Default: \"{Variable}\"")]

    public string FrameRate { get; init; } = Variable;

    public static ContentDeliverySettings FromLiveStream(LiveStream stream) => new ContentDeliverySettings
    {
        IngestionType = IngestionType.FromApiIngestionType(stream.Cdn?.IngestionType),
        Resolution = stream.Cdn?.Resolution ?? Variable,
        FrameRate = stream.Cdn?.FrameRate   ?? Variable
    };
}