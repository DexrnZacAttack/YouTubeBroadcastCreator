using System.ComponentModel;
using System.Text.Json.Serialization;
using Google.Apis.YouTube.v3.Data;

namespace YouTubeBroadcastCreator.Core.Types.Broadcast;

public record BroadcastSchedule
{
    [JsonPropertyName("start_time"),
     Description("Expected stream start time.")]
    public DateTimeOffset? StartTime { get; init; } = null;

    [JsonPropertyName("end_time"), Description("Stream end time")]
    public DateTimeOffset? EndTime { get; init; } = null;

    public static BroadcastSchedule FromLiveBroadcast(LiveBroadcast broadcast) => new()
    {
        StartTime = broadcast.Snippet?.ScheduledStartTimeDateTimeOffset ?? DateTimeOffset.UtcNow,
        EndTime = broadcast.Snippet?.ScheduledEndTimeDateTimeOffset
    };
}