using DotMake.CommandLine;
using YouTubeBroadcastCreator.Command.Auth;

namespace YouTubeBroadcastCreator.Command.Broadcast.Management;

public abstract class BaseBroadcastManagementCommand : IAuthenticatedCommand
{
    [CliOption(Name = "auth-identifier", Description = "Unique ID for the channel you're using, used as an identifier to cache login details", Required = true)]
    public required string AuthIdentifier { get; set; }
    
    [CliOption(Name = "secrets-file", Alias = "-c", Description = "Google Cloud project credentials file path")]
    public FileInfo CredentialsFile { get; set; } = new("secrets.json");
    
    [CliOption(Name = "identifier", Description = "The existing broadcast's (video) ID", Required = true)]
    public required string Identifier { get; set; }
}