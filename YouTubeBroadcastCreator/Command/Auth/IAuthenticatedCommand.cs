using DotMake.CommandLine;

namespace YouTubeBroadcastCreator.Command.Auth;

public interface IAuthenticatedCommand
{
    //TODO more flexible auth options, move to onetime auth via --auth param
    public string AuthIdentifier { get; set; }
    
    public FileInfo CredentialsFile { get; set; }
}