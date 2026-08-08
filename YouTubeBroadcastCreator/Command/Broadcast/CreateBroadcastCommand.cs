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
using YouTubeBroadcastCreator.Command.Auth;
using YouTubeBroadcastCreator.Core;
using YouTubeBroadcastCreator.Core.API;
using YouTubeBroadcastCreator.Types;
using YouTubeBroadcastCreator.Util;

namespace YouTubeBroadcastCreator.Command.Broadcast;

[CliCommand(Name = "create", Description = "Automatically creates a broadcast with the given metadata and outputs the stream key and other metadata into the console.", Parent = typeof(BroadcastCommand), ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public partial class CreateBroadcastCommand : IAuthenticatedCommand
{
    [CliOption(Name = "auth-identifier", Description = "Unique ID for the channel you're using, used as an identifier to cache login details", Required = true)]
    public required string AuthIdentifier { get; set; }

    [CliOption(Name = "secrets-file", Alias = "-c", Description = "Google Cloud project credentials file path")]
    public FileInfo CredentialsFile { get; set; } = new("secrets.json");
    
    [CliOption(Name = "profile-file", Alias = "-p", Description = "Broadcast profile JSON file path")]
    public FileInfo ProfileFile { get; set; } = new("profile.json");
    
    [CliOption(Name = "evaluate-formatter-commands", Description = "Runs inline $cmd[] blocks as commands and outputs the result into the string for supported text fields. NOTE: Only enable if you're sure the text does not contain malicious commands.")]
    public bool EvaluateFormatterCommands { get; set; } = false;

    private static readonly IContentInspector ContentInspector = new ContentInspectorBuilder()
    {
        Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
    }.Build();
    
    public async Task<int> RunAsync()
    {
        await using FileStream mfs = ProfileFile.OpenRead();
        LiveStreamBroadcastProfile profile = await JsonSerializer.DeserializeAsync<LiveStreamBroadcastProfile>(mfs) ??
                                 throw new InvalidOperationException("Broadcast meta is null");

        profile = profile with
        {
            BroadcastMetadata = profile.BroadcastMetadata with
            {
                Title = BroadcastTextFormatter.Format(profile.BroadcastMetadata.Title, EvaluateFormatterCommands),
                Description = BroadcastTextFormatter.Format(profile.BroadcastMetadata.Description, EvaluateFormatterCommands)
            },
            StreamMetadata = profile.StreamMetadata with
            {
                Title = BroadcastTextFormatter.Format(profile.StreamMetadata.Title, EvaluateFormatterCommands),
                Description = BroadcastTextFormatter.Format(profile.StreamMetadata.Description, EvaluateFormatterCommands)
            }
        };

        if (string.IsNullOrWhiteSpace(profile.BroadcastMetadata.Title))
            throw new InvalidOperationException("Broadcast title cannot be empty.");
        
        if (string.IsNullOrWhiteSpace(profile.StreamMetadata.Title))
            throw new InvalidOperationException("Stream title cannot be empty.");
        
        await using FileStream fs = CredentialsFile.OpenRead();
        UserCredential creds = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                    (await GoogleClientSecrets.FromStreamAsync(fs)).Secrets,
                                    [YouTubeService.Scope.Youtube],
                                    AuthIdentifier,
                                    CancellationToken.None,//TODo
                                    new FileDataStore(Constants.ProgramIdentifier)
                                   );
        
        YouTubeApiHelperService ytApi = new(creds);

        BroadcastInfo bc = await PublishNewBroadcastAsync(ytApi, profile);
        Console.WriteLine(JsonSerializer.Serialize(bc));//todo can I find alternative way of doing this???

        return 0;
    }

    private async Task<BroadcastInfo> PublishNewBroadcastAsync(YouTubeApiHelperService ytApi, LiveStreamBroadcastProfile profile)
    {
        LiveStream stream = await ytApi.GetOrCreateStreamAsync(profile.StreamMetadata);
        LiveBroadcast broadcast = await ytApi.CreateBroadcastAsync(profile.BroadcastMetadata);
        
        if (profile.BroadcastMetadata.ThumbnailFile != null)
        {
            await using FileStream fs = profile.BroadcastMetadata.ThumbnailFile.OpenRead();

            var mime = ContentInspector.Inspect(fs);
            fs.Position = 0;
            
            await ytApi.SetThumbnail(broadcast, fs, mime.ByMimeType().FirstOrDefault()?.MimeType ?? "application/octet-stream");
        }

        await ytApi.BindStreamAsync(stream, broadcast);

        return new BroadcastInfo(broadcast.Id, stream.Cdn.IngestionInfo.StreamName, stream.Cdn.IngestionInfo.IngestionAddress);
    }
}