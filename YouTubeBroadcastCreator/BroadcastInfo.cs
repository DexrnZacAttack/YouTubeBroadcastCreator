using System.Text.Json.Serialization;

namespace YouTubeBroadcastCreator;

public record BroadcastInfo(
    [property: JsonPropertyName("video_id")] string BroadcastId,
    [property: JsonPropertyName("stream_key")] string StreamKey,
    [property: JsonPropertyName("stream_addr")] string StreamAddress
);