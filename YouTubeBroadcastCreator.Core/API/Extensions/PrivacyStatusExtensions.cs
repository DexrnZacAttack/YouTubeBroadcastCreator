using YouTubeBroadcastCreator.Core.Types.Broadcast;

namespace YouTubeBroadcastCreator.Core.API.Extensions;

public static class PrivacyStatusExtensions
{
    extension(PrivacyStatus status)
    {
        public static PrivacyStatus FromApiPrivacyStatus(string? privacyStatus)
        => Enum.TryParse(privacyStatus, true, out PrivacyStatus r) ? r : PrivacyStatus.Public;
        
        public string ToApiPrivacyStatus() => status.ToString().ToLowerInvariant();
    }
}