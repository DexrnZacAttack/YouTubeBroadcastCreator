using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouTubeBroadcastCreator.Core.API.Extensions;
using YouTubeBroadcastCreator.Core.Types.Broadcast;
using YouTubeBroadcastCreator.Core.Types.Stream;

namespace YouTubeBroadcastCreator.Core.API;

public class YouTubeApiHelperService(UserCredential credentials)
{
    private readonly YouTubeService _ytService = new(new BaseClientService.Initializer()
    {
        HttpClientInitializer = credentials,
        ApplicationName = Constants.ProgramIdentifier
    });
    
    public async Task<LiveBroadcast> CreateBroadcastAsync(BroadcastMetadata meta)
    {
        LiveBroadcast broadcastPayload = LiveBroadcast.FromBroadcastMetadata(meta);

        LiveBroadcastsResource.InsertRequest insertRequest =
            _ytService.LiveBroadcasts.Insert(broadcastPayload, "snippet,status,contentDetails");
        return await insertRequest.ExecuteAsync();
    }

    public async Task<LiveStream> GetOrCreateStreamAsync(StreamMetadata meta)
    {
        LiveStream? strm = await GetExistingReusableStreamAsync(meta.Title);
        if (strm != null) return strm;
        
        return await CreateStreamAsync(meta);
    }
    
    public async Task<LiveStream> CreateStreamAsync(StreamMetadata meta)
    {
        LiveStream streamPayload = new()
        {
            Snippet = new LiveStreamSnippet
            {
                Title = meta.Title,
                Description = meta.Description
            },
            Cdn = new CdnSettings
            {
                Resolution = meta.ContentDeliverySettings.Resolution,
                FrameRate = meta.ContentDeliverySettings.FrameRate,
                IngestionType = meta.ContentDeliverySettings.IngestionType.ToString().ToLowerInvariant()
            }
        };

        LiveStreamsResource.InsertRequest streamRequest = _ytService.LiveStreams.Insert(streamPayload, "snippet,cdn");
        return await streamRequest.ExecuteAsync();
    }

    public async Task<LiveStream?> GetExistingReusableStreamAsync(string title)
    {
        LiveStreamsResource.ListRequest streamListRequest = _ytService.LiveStreams.List("id,snippet,cdn,status");
        streamListRequest.Mine = true;
        streamListRequest.MaxResults = 50;

        LiveStreamListResponse streams = await streamListRequest.ExecuteAsync();
        
        foreach (LiveStream strm in streams.Items)
        {
            if (strm.Snippet?.Title == null || !strm.Snippet.Title.Equals(title, StringComparison.Ordinal)) continue;
            
            if (strm.Status?.HealthStatus?.ConfigurationIssues?.Any() ?? false)
            {
                await Console.Error.WriteLineAsync("There are configuration issues with the provided existing live stream:");
                foreach (LiveStreamConfigurationIssue configIssue in strm.Status.HealthStatus.ConfigurationIssues)
                {
                    await Console.Error.WriteLineAsync($"   - {configIssue.Reason}");
                    await Console.Error.WriteLineAsync($"     {configIssue.Description}");
                }

                return null;
            }
                
            return strm;
        }

        return null;
    }

    public async Task SetThumbnail(LiveBroadcast broadcast, FileStream fs, string type) =>
        await SetThumbnail(broadcast.Id, fs, type);
    
    public async Task SetThumbnail(string broadcastId, FileStream fs, string type)
    {
        ThumbnailsResource.SetMediaUpload setRequest = _ytService.Thumbnails.Set(broadcastId, fs, type);
        await setRequest.UploadAsync();
    }

    public async Task BindStreamAsync(LiveStream stream, LiveBroadcast broadcast) =>
        await BindStreamAsync(stream.Id, broadcast.Id);
    
    public async Task BindStreamAsync(string streamId, string broadcastId)
    {
        LiveBroadcastsResource.BindRequest bindRequest = _ytService.LiveBroadcasts.Bind(broadcastId, "id,contentDetails");
        bindRequest.StreamId = streamId;

        await bindRequest.ExecuteAsync();
    }
}