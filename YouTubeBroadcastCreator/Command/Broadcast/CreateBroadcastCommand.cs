using System.Diagnostics;
using System.Runtime.InteropServices;
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
using YouTubeBroadcastCreator.Core;
using YouTubeBroadcastCreator.Core.API;
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

    [CliOption(Name = "existing-stream-key-name", Description = "Rebinds the given stream key (by name) to the new broadcast", Required = false)]
    public string? ExistingStreamKeyName { get; set; } = null;

    private static readonly IContentInspector ContentInspector = new ContentInspectorBuilder()
    {
        Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
    }.Build();
    
    public async Task<int> RunAsync()
    {
        await using FileStream mfs = MetadataFile.OpenRead();
        BroadcastMetadata meta = await JsonSerializer.DeserializeAsync<BroadcastMetadata>(mfs) ??
                                 throw new InvalidOperationException("Broadcast meta is null");
        
        string title = BroadcastTextFormatter.Format(meta.Title, EvaluateFormatterCommands);
        string desc = BroadcastTextFormatter.Format(meta.Description, EvaluateFormatterCommands);

        meta = meta with
        {
            Title = title,
            Description = desc
        };

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
                                    new FileDataStore(Constants.ProgramIdentifier)
                                   );
        
        YouTubeAPIHelperService ytApi = new(creds);

        BroadcastInfo bc = await PublishNewBroadcastAsync(ytApi, meta);
        Console.WriteLine(JsonSerializer.Serialize(bc));//todo can I find alternative way of doing this???

        return 0;
    }

    private async Task<BroadcastInfo> PublishNewBroadcastAsync(YouTubeAPIHelperService ytApi, BroadcastMetadata meta)
    {
        LiveStream stream = await ytApi.GetOrCreateStreamAsync(ExistingStreamKeyName, meta);
        LiveBroadcast broadcast = await ytApi.CreateBroadcastAsync(meta);
        
        if (meta.ThumbnailFile != null)
        {
            await using FileStream fs = meta.ThumbnailFile.OpenRead();

            var mime = ContentInspector.Inspect(fs);
            fs.Position = 0;
            
            await ytApi.SetThumbnail(broadcast, fs, mime.ByMimeType().FirstOrDefault()?.MimeType ?? "application/octet-stream");
        }

        await ytApi.BindStreamAsync(stream, broadcast);

        return new BroadcastInfo(broadcast.Id, stream.Cdn.IngestionInfo.StreamName, stream.Cdn.IngestionInfo.IngestionAddress);
    }
}