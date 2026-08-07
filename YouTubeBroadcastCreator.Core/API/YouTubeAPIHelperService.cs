using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace YouTubeBroadcastCreator.Core.API;

public class YouTubeAPIHelperService(UserCredential credentials)
{
    private readonly YouTubeService _ytService = new(new BaseClientService.Initializer()
    {
        HttpClientInitializer = credentials,
        ApplicationName = Constants.ProgramIdentifier
    });
    
    public async Task<LiveBroadcast> CreateBroadcastAsync(BroadcastMetadata meta)
    {
        LiveBroadcast broadcastPayload = new()
        {
            Status = new LiveBroadcastStatus
            {
                PrivacyStatus = meta.PrivacyStatus.ToString().ToLowerInvariant(),
                SelfDeclaredMadeForKids = meta.MadeForKids
            },
            ContentDetails = new LiveBroadcastContentDetails
            {
                EnableAutoStart = meta.Settings.UseAutoStart,
                EnableAutoStop = meta.Settings.UseAutoStop,
                EnableDvr = meta.Settings.UseDvr,
                RecordFromStart = meta.Settings.RecordFromStart,
                EnableEmbed = meta.Settings.AllowEmbedding,
                LatencyPreference = meta.Settings.StreamLatency switch
                {
                    StreamLatency.Normal   => "normal",
                    StreamLatency.Low      => "low",
                    StreamLatency.UltraLow => "ultraLow",
                    _                      => throw new InvalidOperationException("Invalid latency value")
                },
                EnableLowLatency = meta.Settings.StreamLatency switch
                {
                    StreamLatency.Normal   => false,
                    StreamLatency.Low      => true,
                    StreamLatency.UltraLow => null,
                    _                      => throw new InvalidOperationException("Invalid latency value")
                }
            },
            Snippet = new LiveBroadcastSnippet
            {
                Title = meta.Title,
                Description = meta.Description,
                ScheduledStartTimeDateTimeOffset = meta.Schedule?.StartTime ?? DateTimeOffset.UtcNow,
                ScheduledEndTimeDateTimeOffset = meta.Schedule?.EndTime,
            }
        };

        LiveBroadcastsResource.InsertRequest insertRequest =
            _ytService.LiveBroadcasts.Insert(broadcastPayload, "snippet,status,contentDetails");
        return await insertRequest.ExecuteAsync();
    }

    public async Task<LiveStream> GetOrCreateStreamAsync(string? existingStreamTitle, BroadcastMetadata meta)
    {
        if (!string.IsNullOrWhiteSpace(existingStreamTitle))
        {
            LiveStream? strm = await GetExistingReusableStreamAsync(existingStreamTitle);
            if (strm != null) return strm;
        }
        
        return await CreateStreamAsync(meta);
    }
    
    public async Task<LiveStream> CreateStreamAsync(BroadcastMetadata meta)
    {
        LiveStream streamPayload = new()
        {
            Snippet = new LiveStreamSnippet
            {
                Title = $"(YouTubeBroadcastCreator:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}) {meta.Title}",
                Description = $"Created by YouTubeBroadcastCreator on {DateTime.Now:F}"
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