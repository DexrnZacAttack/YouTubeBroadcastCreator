using System.Diagnostics;
using System.Numerics;
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
using Microsoft.Extensions.Logging;
using MimeDetective;
using Serilog;
using SmartFormat;
using YouTubeBroadcastCreator.Command.Auth;
using YouTubeBroadcastCreator.Core;
using YouTubeBroadcastCreator.Core.API;
using YouTubeBroadcastCreator.Types;
using YouTubeBroadcastCreator.Util;
using YouTubeBroadcastCreator.Util.API;

namespace YouTubeBroadcastCreator.Command.Broadcast;

[CliCommand(Name = "create",
            Description =
                "Automatically creates a broadcast with the given metadata and outputs the stream key and other metadata into the console.",
            Parent = typeof(BroadcastCommand),
            ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives |
                                    CliNameAutoGenerate.Options)]
public partial class CreateBroadcastCommand(ILoggerFactory loggerFactory) : IAuthenticatedCommand
{
    //TODO can I somehow create like param groups and instead choose a param group based on enum value?
    //maybe via some computed property
    [CliOption(Name = "auth-identifier",
               Description = "Unique ID for the channel you're using, used as an identifier to cache login details",
               Required = true)]
    public required string AuthIdentifier { get; set; }

    [CliOption(Name = "secrets-file", Alias = "-c", Description = "Google Cloud project credentials file path")]
    public FileInfo CredentialsFile { get; set; } = new("secrets.json");

    [CliOption(Name = "profile-file", Alias = "-p", Description = "Broadcast profile JSON file path")]
    public FileInfo ProfileFile { get; set; } = new("profile.json");

    [CliOption(Name = "evaluate-formatter-commands",
               Description =
                   "Runs inline $cmd[] blocks as commands and outputs the result into the string for supported text fields. NOTE: Only enable if you're sure the text does not contain malicious commands.")]
    public bool EvaluateFormatterCommands { get; set; } = false;
    
    [CliOption(Name = "output", Alias = "o",
               Description =
                   "Outputs to a file with the given path", Required = false)]
    public FileInfo? OutputFile { get; set; }

    private static readonly IContentInspector ContentInspector = new ContentInspectorBuilder()
    {
        Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
    }.Build();

    public async Task<int> RunAsync()
    {
        if (OutputFile != null && !(OutputFile.Directory?.Exists ?? false))
        {
            throw new
                DirectoryNotFoundException($"Directory for output file not found. Chosen file path: '{OutputFile.FullName}'");
        }
        
        await using FileStream mfs = ProfileFile.OpenRead();
        LiveStreamBroadcastProfile profile = await JsonSerializer.DeserializeAsync<LiveStreamBroadcastProfile>(mfs) ??
                                             throw new InvalidOperationException("Broadcast meta is null");

        profile = new LiveStreamBroadcastProfile(
                                                 BroadcastMetadata: profile.BroadcastMetadata with
                                                 {
                                                     Title =
                                                     BroadcastTextFormatter.Format(profile.BroadcastMetadata.Title,
                                                         EvaluateFormatterCommands),
                                                     Description =
                                                     BroadcastTextFormatter.Format(profile.BroadcastMetadata
                                                        .Description, EvaluateFormatterCommands)
                                                 },
                                                 StreamMetadata: profile.StreamMetadata with
                                                 {
                                                     Title = BroadcastTextFormatter.Format(profile.StreamMetadata.Title,
                                                         EvaluateFormatterCommands),
                                                     Description =
                                                     BroadcastTextFormatter.Format(profile.StreamMetadata.Description,
                                                         EvaluateFormatterCommands)
                                                 });

        if (string.IsNullOrWhiteSpace(profile.BroadcastMetadata.Title))
            throw new InvalidOperationException("Broadcast title cannot be empty.");

        if (string.IsNullOrWhiteSpace(profile.StreamMetadata.Title))
            throw new InvalidOperationException("Stream title cannot be empty.");

        Log.Information("Authenticating");
        
        await using FileStream fs = CredentialsFile.OpenRead();
        UserCredential creds = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                    (await GoogleClientSecrets.FromStreamAsync(fs)).Secrets,
                                    [YouTubeService.Scope.Youtube],
                                    AuthIdentifier,
                                    CancellationToken.None, //TODo
                                    new FileDataStore(Constants.ProgramIdentifier),
                                    //we do this so that the failure reason is obvious instead of the entire program stalling because it tried to open a browser
                                    //which may break anything that wants to read the output and/or confuse the user.
                                    new
                                        ExceptionCodeReceiver($"Not authenticated, please see '{Constants.ProgramName} auth --help' for info on how to authenticate.")
                                   );

        Log.Information("Creating {Name}", nameof(YouTubeApiHelperService));
        YouTubeApiHelperService ytApi = new(creds, loggerFactory.CreateLogger<YouTubeApiHelperService>());

        Log.Information("Publishing new broadcast");
        BroadcastInfo bc = await PublishNewBroadcastAsync(ytApi, profile);
        
        Log.Information("Broadcast '{Name}' has been published.\nVideo link: https://youtu.be/{Id}\nBroadcast info:", profile.BroadcastMetadata.Title, bc.BroadcastId);
        string serialized = JsonSerializer.Serialize(bc);
        Console.WriteLine(serialized); //todo can I find alternative way of doing this???

        if (OutputFile != null)
        {
            Log.Information("Writing to file {Path}", OutputFile.FullName);
            await using FileStream o = OutputFile.Open(FileMode.Create, FileAccess.Write);
            await using StreamWriter sw = new(o);
        
            await sw.WriteAsync(serialized);
        }
        
        return 0;
    }

    private async Task<BroadcastInfo> PublishNewBroadcastAsync(YouTubeApiHelperService ytApi,
                                                               LiveStreamBroadcastProfile profile)
    {
        Log.Logger.Information("Getting stream");
        LiveStream stream = await ytApi.GetOrCreateStreamAsync(profile.StreamMetadata);
        Log.Logger.Information("Got stream '{Name}'", stream.Snippet.Title);

        Log.Logger.Information("Creating broadcast");
        LiveBroadcast broadcast = await ytApi.CreateBroadcastAsync(profile.BroadcastMetadata);
        Log.Logger.Information("Created broadcast '{Name}'", broadcast.Snippet.Title);

        if (profile.BroadcastMetadata.ThumbnailFile != null)
        {
            await using FileStream fs = profile.BroadcastMetadata.ThumbnailFile.OpenRead();

            Log.Logger.Information("Uploading thumbnail '{Path}'", profile.BroadcastMetadata.ThumbnailFile.FullName);
            var mime = ContentInspector.Inspect(fs);
            fs.Position = 0;

            await ytApi.SetThumbnail(broadcast, fs,
                                     mime.ByMimeType().FirstOrDefault()?.MimeType ?? "application/octet-stream");
        }

        Log.Logger.Information("Binding stream to broadcast");
        await ytApi.BindStreamAsync(stream, broadcast);

        return new BroadcastInfo(broadcast.Id, stream.Cdn.IngestionInfo.StreamName,
                                 stream.Cdn.IngestionInfo.IngestionAddress);
    }
}