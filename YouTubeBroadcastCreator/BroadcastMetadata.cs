using System.Text.Json.Serialization;

namespace YouTubeBroadcastCreator;

public enum PrivacyStatus
{
    Public,
    Unlisted,
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
    [property: JsonPropertyName("start_time")] DateTimeOffset? StartTime,
    [property: JsonPropertyName("end_time")] DateTimeOffset? EndTime
);

public record BroadcastSettings(
    [property: JsonPropertyName("auto_start_enabled"), JsonRequired] bool UseAutoStart,
    [property: JsonPropertyName("auto_stop_enabled"), JsonRequired] bool UseAutoStop,
    [property: JsonPropertyName("dvr_enabled"), JsonRequired] bool UseDvr,
    [property: JsonPropertyName("allow_embedding"), JsonRequired] bool AllowEmbedding,
    [property: JsonPropertyName("record_from_start"), JsonRequired] bool RecordFromStart,
    [property: JsonPropertyName("latency"), JsonRequired, JsonConverter(typeof(JsonStringEnumConverter<StreamLatency>))] StreamLatency StreamLatency
);

public record ContentDeliverySetting(
    [property: JsonPropertyName("ingestion"), JsonRequired, JsonConverter(typeof(JsonStringEnumConverter<IngestionType>))] IngestionType IngestionType,
    [property: JsonPropertyName("resolution")] string Resolution = "variable",
    [property: JsonPropertyName("frame_rate")] string FrameRate = "variable"
);

public record BroadcastMetadata(
    [property: JsonPropertyName("title"), JsonRequired] string Title,
    [property: JsonPropertyName("description"), JsonRequired] string Description,
    [property: JsonPropertyName("settings"), JsonRequired] BroadcastSettings Settings,
    [property: JsonPropertyName("privacy"), JsonRequired, JsonConverter(typeof(JsonStringEnumConverter<PrivacyStatus>))] PrivacyStatus PrivacyStatus,
    [property: JsonPropertyName("made_for_kids"), JsonRequired] bool MadeForKids,
    [property: JsonPropertyName("schedule")] BroadcastSchedule? Schedule,
    [property: JsonPropertyName("cdn")] ContentDeliverySetting ContentDeliverySettings
);