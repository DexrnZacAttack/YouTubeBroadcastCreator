namespace YouTubeBroadcastCreator.Core.Types.Broadcast;

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