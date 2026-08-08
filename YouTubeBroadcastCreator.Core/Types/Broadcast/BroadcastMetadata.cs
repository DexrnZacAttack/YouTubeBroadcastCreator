using System.ComponentModel;
using System.Text.Json.Serialization;
using Google.Apis.YouTube.v3.Data;
using YouTubeBroadcastCreator.Core.API.Extensions;
using YouTubeBroadcastCreator.Core.Util.Serialization;
using YouTubeBroadcastCreator.Core.Util.Serialization.Schema;

namespace YouTubeBroadcastCreator.Core.Types.Broadcast;

public record BroadcastMetadata
{
    [JsonPropertyName("title")]
    [Description("The broadcast title, must not be empty.")]
    [JsonRequired]
    public string Title { get; init; } = "YouTubeBroadcastCreator Template Broadcast Title";

    [JsonPropertyName("description")]
    [Description("The broadcast description")]
    [JsonRequired]
    public string Description { get; init; } = "Created using YouTubeBroadcastCreator";

    [JsonPropertyName("thumbnail_path")]
    [Description("Path to a thumbnail file if desired")]
    [JsonConverter(typeof(FileInfoJsonConverter))]
    [SchemaType("string, null")]
    public FileInfo? ThumbnailFile { get; init; }

    [JsonPropertyName("settings")]
    [Description("Broadcast settings")]
    [JsonRequired]
    public BroadcastSettings Settings { get; init; } = new();

    [JsonPropertyName("privacy")]
    [Description("Video privacy")]
    [JsonRequired]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PrivacyStatus PrivacyStatus { get; init; } = PrivacyStatus.Unlisted;

    [JsonPropertyName("made_for_kids")]
    [Description("Whether the broadcast is marked as 'Made for kids'")]
    [JsonRequired]
    public bool MadeForKids { get; init; }

    [JsonPropertyName("schedule")]
    [Description("The broadcast schedule")]
    public BroadcastSchedule Schedule { get; init; } = new BroadcastSchedule();
    
    public LiveBroadcast ToLiveBroadcast() => new()
    {
        Status = new LiveBroadcastStatus
        {
            PrivacyStatus = PrivacyStatus.ToApiPrivacyStatus(),
            SelfDeclaredMadeForKids = MadeForKids
        },
        ContentDetails = new LiveBroadcastContentDetails
        {
            EnableAutoStart = Settings.UseAutoStart,
            EnableAutoStop = Settings.UseAutoStop,
            EnableDvr = Settings.UseDvr,
            RecordFromStart = Settings.RecordFromStart,
            EnableEmbed = Settings.AllowEmbedding,
            LatencyPreference = Settings.BroadcastLatency.ToApiLatencyPreferenceString(),
            EnableLowLatency = Settings.BroadcastLatency.ToApiLowLatencySetting()
        },
        Snippet = new LiveBroadcastSnippet
        {
            Title = Title,
            Description = Description,
            ScheduledStartTimeDateTimeOffset = Schedule?.StartTime ?? DateTimeOffset.UtcNow,
            ScheduledEndTimeDateTimeOffset = Schedule?.EndTime,
        }
    };

    public static BroadcastMetadata FromLiveBroadcast(LiveBroadcast broadcast) => new() {
         Title = broadcast.Snippet?.Title             ?? "Default Title",
         Description = broadcast.Snippet?.Description ?? string.Empty,
         ThumbnailFile = null,
         Settings = BroadcastSettings.FromLiveBroadcast(broadcast),
         PrivacyStatus = PrivacyStatus.FromApiPrivacyStatus(broadcast.Status?.PrivacyStatus),
         Schedule = BroadcastSchedule.FromLiveBroadcast(broadcast),
         MadeForKids = broadcast.Status?.SelfDeclaredMadeForKids ?? false
    };
}