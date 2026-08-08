using YouTubeBroadcastCreator.Core.Types.Stream;

namespace YouTubeBroadcastCreator.Core.API.Extensions;

public static class IngestionTypeExtensions
{
    extension(IngestionType ingestionType)
    {
        public static IngestionType FromApiIngestionType(string? t)
            => Enum.TryParse(t, true, out IngestionType r) ? r : IngestionType.RTMP;
        
        public string ToApiIngestionType() => ingestionType.ToString().ToLowerInvariant();
    }
}