using System.ComponentModel;
using System.Text.Json.Serialization;
using YouTubeBroadcastCreator.Util.Serialization;
using YouTubeBroadcastCreator.Util.Serialization.Schema;

namespace YouTubeBroadcastCreator;

public enum PrivacyStatus
{
    /// <summary>
    /// Publicly viewable
    /// </summary>
    Public,
    /// <summary>
    /// Viewable by anyone who has the link
    /// </summary>
    Unlisted,
    /// <summary>
    /// Only viewable by specific people
    /// </summary>
    Private
}

public enum StreamLatency
{
    Normal,
    Low,
    UltraLow
}

public enum IngestionType
{
    RTMP,
    HLS
}

public record BroadcastSchedule(
    [property: JsonPropertyName("start_time"), Description("Expected stream start time. Default: DateTimeOffset.UtcNow when the broadcast is created")] DateTimeOffset? StartTime,
    [property: JsonPropertyName("end_time"), Description("Stream end time")] DateTimeOffset? EndTime
);

public record BroadcastSettings(
    [property: JsonPropertyName("auto_start_enabled"), Description("Whether the stream should start and publish automatically when data is received from the encoder. true recommended for automation."), JsonRequired] bool UseAutoStart,
    [property: JsonPropertyName("auto_stop_enabled"), Description("Whether the stream should stop and publish a VOD (if possible) automatically when the encoder has stopped streaming. true recommended for automation."), JsonRequired] bool UseAutoStop,
    [property: JsonPropertyName("dvr_enabled"), Description("Whether the stream should publish a VOD when stopped. Maximum VOD time of 12 hours"), JsonRequired] bool UseDvr,
    [property: JsonPropertyName("allow_embedding"), Description("Whether the stream is allowed to be embedded on external sites."), JsonRequired] bool AllowEmbedding,
    [property: JsonPropertyName("record_from_start"), Description("Whether YouTube will automatically start recording the broadcast after the event's status changes to live.  This property's default value is true, and it can only be set to false if the broadcasting channel is allowed to disable recordings for live broadcasts. https://developers.google.com/youtube/v3/live/docs/liveBroadcasts#contentDetails.recordFromStart"), JsonRequired] bool RecordFromStart,
    [property: JsonPropertyName("latency"), JsonRequired, Description("Desired stream latency. Note that using ultraLow disables higher quality streams and closed captioning."), JsonConverter(typeof(JsonStringEnumConverter<StreamLatency>))] StreamLatency StreamLatency
);

public record ContentDeliverySettings(
    [property: JsonPropertyName("ingestion"), JsonRequired, Description("Ingestion type, such as RTSP, HLS"), JsonConverter(typeof(JsonStringEnumConverter<IngestionType>))] IngestionType IngestionType,
    [property: JsonPropertyName("resolution"), Description("Expected stream resolution, use 'variable' to auto detect. Default: \"variable\"")] string Resolution = "variable",
    [property: JsonPropertyName("frame_rate"), Description("Expected stream FPS, use 'variable' to auto detect. Default: \"variable\"")] string FrameRate = "variable"
);

public record BroadcastMetadata(
    [property: JsonPropertyName("title"), Description("The stream title, must not be empty."), JsonRequired] string Title,
    [property: JsonPropertyName("description"), Description("The stream description"), JsonRequired] string Description,
    [property: JsonPropertyName("thumbnail_path"), Description("Path to a thumbnail file if desired"), JsonConverter(typeof(FileInfoJsonConverter)), SchemaType("string, null")] FileInfo? ThumbnailFile,
    [property: JsonPropertyName("settings"), Description("Broadcast settings"), JsonRequired] BroadcastSettings Settings,
    [property: JsonPropertyName("privacy"), Description("Video privacy"), JsonRequired, JsonConverter(typeof(JsonStringEnumConverter))] PrivacyStatus PrivacyStatus,
    [property: JsonPropertyName("made_for_kids"), Description("Whether the stream is marked as 'Made for kids'"), JsonRequired] bool MadeForKids,
    [property: JsonPropertyName("schedule"), Description("The stream schedule")] BroadcastSchedule? Schedule,
    [property: JsonPropertyName("cdn"), Description("CDN settings")] ContentDeliverySettings ContentDeliverySettings
);