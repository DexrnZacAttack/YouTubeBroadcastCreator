using System.ComponentModel;
using System.Text.Json.Serialization;
using Google.Apis.YouTube.v3.Data;
using YouTubeBroadcastCreator.Core.API.Extensions;

namespace YouTubeBroadcastCreator.Core.Types.Broadcast;

public record BroadcastSettings
{
    [JsonPropertyName("auto_start_enabled"),
     Description("Whether the stream should start and publish automatically when data is received from the encoder. true recommended for automation."),
     JsonRequired]
    public bool UseAutoStart { get; init; } = true;

    [JsonPropertyName("auto_stop_enabled"),
     Description("Whether the stream should stop and publish a VOD (if possible) automatically when the encoder has stopped streaming. true recommended for automation."),
     JsonRequired]
    public bool UseAutoStop { get; init; } = true;

    [JsonPropertyName("dvr_enabled"),
     Description("Whether the stream should publish a VOD when stopped. Maximum VOD time of 12 hours"),
     JsonRequired]
    public bool UseDvr { get; init; } = true;

    [JsonPropertyName("allow_embedding"),
     Description("Whether the stream is allowed to be embedded on external sites."), JsonRequired]
    public bool AllowEmbedding { get; init; } = true;

    [JsonPropertyName("record_from_start"),
     Description("Whether YouTube will automatically start recording the broadcast after the event's status changes to live.  This property's default value is true, and it can only be set to false if the broadcasting channel is allowed to disable recordings for live broadcasts. https://developers.google.com/youtube/v3/live/docs/liveBroadcasts#contentDetails.recordFromStart"),
     JsonRequired]
    public bool RecordFromStart { get; init; } = true;

    [JsonPropertyName("latency"), JsonRequired,
     Description("Desired stream latency. Note that using ultraLow disables higher quality streams and closed captioning."),
     JsonConverter(typeof(JsonStringEnumConverter<BroadcastLatency>))]
    public BroadcastLatency BroadcastLatency { get; init; } = BroadcastLatency.Normal;

    public static BroadcastSettings FromLiveBroadcast(LiveBroadcast broadcast) => new()
    {
        UseAutoStart = broadcast.ContentDetails?.EnableAutoStart    ?? false,
        UseAutoStop = broadcast.ContentDetails?.EnableAutoStop      ?? false,
        UseDvr = broadcast.ContentDetails?.EnableDvr                ?? true,
        RecordFromStart = broadcast.ContentDetails?.RecordFromStart ?? true,
        AllowEmbedding = broadcast.ContentDetails?.EnableEmbed      ?? true,
        BroadcastLatency = BroadcastLatency.FromApiLatencyPreferenceString(broadcast.ContentDetails?.LatencyPreference)
    };
}