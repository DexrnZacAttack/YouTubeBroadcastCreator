using System.ComponentModel;
using System.Text.Json.Serialization;
using YouTubeBroadcastCreator.Core.Types.Broadcast;
using YouTubeBroadcastCreator.Core.Types.Stream;

namespace YouTubeBroadcastCreator.Types;

public record LiveStreamBroadcastProfile(
    [property: JsonPropertyName("broadcast"), Description("Broadcast metadata")]
    BroadcastMetadata BroadcastMetadata,

    [property: JsonPropertyName("stream"), Description("Live stream metadata")]
    StreamMetadata StreamMetadata
)
{
    public LiveStreamBroadcastProfile() : this(new BroadcastMetadata(), new StreamMetadata())
    {
    }
}