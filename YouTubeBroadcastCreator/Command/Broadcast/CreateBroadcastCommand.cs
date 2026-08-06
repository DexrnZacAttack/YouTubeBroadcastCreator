using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using CliWrap.Buffered;
using DotMake.CommandLine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MimeDetective;
using SmartFormat;
using YouTubeBroadcastCreator.Util;

namespace YouTubeBroadcastCreator.Command.Broadcast;

[CliCommand(Name = "create", Description = "Automatically creates a broadcast with the given metadata and outputs the stream key and other metadata into the console.", Parent = typeof(BroadcastCommand), ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public partial class CreateBroadcastCommand
{
    [CliArgument(Name = "identifier", Description = "Unique ID for the channel you're using, used as an identifier to cache login details")]
    public required string Identifier { get; set; }

    [CliOption(Name = "secrets-file", Alias = "-c", Description = "Google Cloud project credentials file path")]
    public FileInfo CredentialsFile { get; set; } = new("secrets.json");
    
    [CliOption(Name = "metadata-file", Alias = "-m", Description = "Stream metadata JSON file path")]
    public FileInfo MetadataFile { get; set; } = new("metadata.json");
    
    [CliOption(Name = "evaluate-formatter-commands", Description = "Runs inline $cmd[] blocks as commands and outputs the result into the string for supported text fields. NOTE: Only enable if you're sure the text does not contain malicious commands.")]
    public bool EvaluateFormatterCommands { get; set; } = false;

    private static readonly IContentInspector ContentInspector = new ContentInspectorBuilder()
    {
        Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
    }.Build();
    
    public async Task<int> RunAsync()
    {
        await using FileStream mfs = MetadataFile.OpenRead();
        BroadcastMetadata meta = await JsonSerializer.DeserializeAsync<BroadcastMetadata>(mfs) ??
                                 throw new InvalidOperationException("Broadcast meta is null");

        if (string.IsNullOrWhiteSpace(meta.Title))
        {
            throw new InvalidOperationException("Stream title cannot be empty.");
        }
        
        await using FileStream fs = CredentialsFile.OpenRead();
        UserCredential creds = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                    (await GoogleClientSecrets.FromStreamAsync(fs)).Secrets,
                                    [YouTubeService.Scope.Youtube],
                                    Identifier,
                                    CancellationToken.None,//TODo
                                    new FileDataStore(Program.ProgramIdentifier)
                                   );

        BroadcastInfo bc = await CreateBroadcastAsync(creds, meta);
        Console.WriteLine(JsonSerializer.Serialize(bc));//todo can I find alternative way of doing this???

        return 0;
    }

    private async Task<BroadcastInfo> CreateBroadcastAsync(UserCredential creds, BroadcastMetadata meta)
    {
        string title = BroadcastTextFormatter.Format(meta.Title, EvaluateFormatterCommands);
        string desc = BroadcastTextFormatter.Format(meta.Description, EvaluateFormatterCommands);
        
        YouTubeService yt = new(new BaseClientService.Initializer()
        {
            HttpClientInitializer = creds,
            ApplicationName = Program.ProgramIdentifier
        });

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
                Title = title,
                Description = desc,
                ScheduledStartTimeDateTimeOffset = meta.Schedule?.StartTime ?? DateTimeOffset.UtcNow,
                ScheduledEndTimeDateTimeOffset = meta.Schedule?.EndTime,
            }
        };

        LiveBroadcastsResource.InsertRequest insertRequest =
            yt.LiveBroadcasts.Insert(broadcastPayload, "snippet,status,contentDetails");
        LiveBroadcast broadcast = await insertRequest.ExecuteAsync();

        //note: video id
        string broadcastId = broadcast.Id;

        if (meta.ThumbnailFile != null)
        {
            await using FileStream fs = meta.ThumbnailFile.OpenRead();
            var m = ContentInspector.Inspect(fs);
            
            ThumbnailsResource.SetMediaUpload setRequest = yt.Thumbnails.Set(broadcastId, fs, m.ByMimeType().FirstOrDefault()?.MimeType ?? "application/octet-stream");
            await setRequest.UploadAsync();
        }
        
        LiveStream streamPayload = new()
        {
            Snippet = new LiveStreamSnippet
            {
                Title = $"(YouTubeBroadcastCreator) {title}"
            },
            Cdn = new CdnSettings
            {
                Resolution = meta.ContentDeliverySettings.Resolution,
                FrameRate = meta.ContentDeliverySettings.FrameRate,
                IngestionType = meta.ContentDeliverySettings.IngestionType.ToString().ToLowerInvariant()
            }
        };

        LiveStreamsResource.InsertRequest streamRequest = yt.LiveStreams.Insert(streamPayload, "snippet,cdn");
        LiveStream stream = await streamRequest.ExecuteAsync();

        string streamId = stream.Id;
        string streamKey = stream.Cdn.IngestionInfo.StreamName;
        string streamAddr = stream.Cdn.IngestionInfo.IngestionAddress;

        LiveBroadcastsResource.BindRequest bindRequest = yt.LiveBroadcasts.Bind(broadcastId, "id,contentDetails");
        bindRequest.StreamId = streamId;

        await bindRequest.ExecuteAsync();

        return new BroadcastInfo(broadcastId, streamKey, streamAddr);
    }
}