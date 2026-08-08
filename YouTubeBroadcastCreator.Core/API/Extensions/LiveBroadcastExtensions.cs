using Google.Apis.YouTube.v3.Data;
using YouTubeBroadcastCreator.Core.Types.Broadcast;

namespace YouTubeBroadcastCreator.Core.API.Extensions;

public static class LiveBroadcastExtensions
{
    extension(LiveBroadcast broadcast)
    {
        public static LiveBroadcast FromBroadcastMetadata(BroadcastMetadata meta) => meta.ToLiveBroadcast();
    }
}