using System.ComponentModel;
using System.Text.Json.Serialization;
using YouTubeBroadcastCreator.Core.Types.Broadcast;

namespace YouTubeBroadcastCreator.Core.Types.Stream;

public record StreamMetadata
{
    [JsonPropertyName("title"), Description("The stream title, must not be empty.")]
    public string Title { get; init; } = @"YouTubeBroadcastCreator - {date:yyyy-MM-dd HH\:mm\:ss}";

    [JsonPropertyName("description"), Description("The stream description"), JsonRequired]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("cdn"), Description("CDN settings")]
    public ContentDeliverySettings ContentDeliverySettings { get; init; } = new();
}