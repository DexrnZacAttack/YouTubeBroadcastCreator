using DotMake.CommandLine;

namespace YouTubeBroadcastCreator.Command.Auth;

public interface IAuthenticatedCommand
{
    //TODO more flexible auth options
    public string AuthIdentifier { get; set; }
    
    public FileInfo CredentialsFile { get; set; }
}