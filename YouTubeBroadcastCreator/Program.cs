using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Schema;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using SmartFormat;

namespace YouTubeBroadcastCreator;

internal class Program
{
    private const string Identifier = "me.dexrn.YouTubeBroadcastCreator";

    private static async Task<int> Main(string[] args)
    {
        Option<string> idOption = new("--identifier")
        {
            Description = "Unique ID for the channel you're using, used as an identifier to cache login details",
            Required = true
        };

        Option<FileInfo> credsOption = new("--secrets-file")
        {
            Description = "Google Cloud project credentials file path",
            DefaultValueFactory = _ => new FileInfo("secrets.json")
        };
        credsOption.AcceptExistingOnly();

        Option<FileInfo> metadataOption = new("--metadata-file")
        {
            Description = "File used for stream metadata",
            DefaultValueFactory = _ => new FileInfo("metadata.json")
        };
        metadataOption.AcceptExistingOnly();

        RootCommand rootCommand = new("""
                                      Automatically creates a broadcast with the given metadata and outputs the stream key and other metadata into the console.

                                      This project is licensed under the MIT license, see https://github.com/DexrnZacAttack/YouTubeBroadcastCreator/tree/master/LICENSE for more info.
                                      NOTE: I am not responsible for any problems that this program may cause, even if your account gets banned.
                                      """);
        rootCommand.Options.Add(idOption);
        rootCommand.Options.Add(credsOption);
        rootCommand.Options.Add(metadataOption);

        rootCommand.SetAction(async res =>
        {
            await using FileStream mfs = res.GetRequiredValue(metadataOption).OpenRead();
            BroadcastMetadata meta = await JsonSerializer.DeserializeAsync<BroadcastMetadata>(mfs) ??
                                     throw new InvalidOperationException("Broadcast meta is null");

            await using FileStream fs = res.GetRequiredValue(credsOption).OpenRead();
            UserCredential creds = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                        (await GoogleClientSecrets.FromStreamAsync(fs)).Secrets,
                                        [YouTubeService.Scope.Youtube],
                                        res.GetRequiredValue(idOption),
                                        CancellationToken.None,//TODo
                                        new FileDataStore(Identifier)
                                       );

            BroadcastInfo bc = await CreateBroadcastAsync(creds, meta);
            Console.WriteLine(JsonSerializer.Serialize(bc));//todo can I find alternative way of doing this???

            return 0;
        });

        Command writeMetadataSchemaCommand =
            new("write-metadata-schema", "Writes the metadata schema to ./metadata.schema.json and exits.");
        writeMetadataSchemaCommand.SetAction(async _ =>
        {
            await File.WriteAllTextAsync("metadata.schema.json",
                                         JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(BroadcastMetadata))
                                                              .ToString());
            return 0;
        });
        
        Command writeOutputMetadataSchemaCommand =
            new("write-output-metadata-schema", "Writes the outputted metadata schema to ./output_metadata.schema.json and exits.");
        writeOutputMetadataSchemaCommand.SetAction(async _ =>
        {
            await File.WriteAllTextAsync("output_metadata.schema.json",
                                         JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(BroadcastInfo))
                                                              .ToString());
            return 0;
        });

        rootCommand.Subcommands.Add(writeMetadataSchemaCommand);
        rootCommand.Subcommands.Add(writeOutputMetadataSchemaCommand);
        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static string FormatDefault(string s)
    {
        return Smart.Format(s, new
        {
            date = DateTime.Now//idk what else to add
        });
    }

    private static async Task<BroadcastInfo> CreateBroadcastAsync(UserCredential creds, BroadcastMetadata meta)
    {
        YouTubeService yt = new(new BaseClientService.Initializer()
        {
            HttpClientInitializer = creds,
            ApplicationName = Identifier
        });

        string title = FormatDefault(meta.Title);
        string desc = FormatDefault(meta.Description);

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
                ScheduledEndTimeDateTimeOffset = meta.Schedule?.EndTime
            }
        };

        LiveBroadcastsResource.InsertRequest insertRequest =
            yt.LiveBroadcasts.Insert(broadcastPayload, "snippet,status,contentDetails");
        LiveBroadcast broadcast = await insertRequest.ExecuteAsync();

        //note: video id
        string broadcastId = broadcast.Id;

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