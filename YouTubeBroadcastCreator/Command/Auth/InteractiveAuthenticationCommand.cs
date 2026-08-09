using DotMake.CommandLine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Serilog;
using YouTubeBroadcastCreator.Core;

namespace YouTubeBroadcastCreator.Command.Auth;

[CliCommand(Name = "interactive", Description = "Authenticates via a login flow in the local web browser",
            Parent = typeof(BaseAuthenticationCommand),
            ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives |
                                    CliNameAutoGenerate.Options)]
public class InteractiveAuthenticationCommand : IAuthenticatedCommand, ICliRunAsyncWithReturn
{
    [CliOption(Name = "auth-identifier",
               Description = "Unique ID for the channel you're using, used as an identifier to cache login details",
               Required = true)]
    public required string AuthIdentifier { get; set; }

    [CliOption(Name = "secrets-file", Alias = "-c", Description = "Google Cloud project credentials file path")]
    public FileInfo CredentialsFile { get; set; } = new("secrets.json");
    
    [CliOption(Name = "force", Alias = "-f", Description = "Reauthenticates even if the given auth-identifier is already present and not stale", Required = false)]
    public bool ForceAuth { get; set; } = false;

    public async Task<int> RunAsync()
    {
        FileDataStore dat = new(Constants.ProgramIdentifier);
        TokenResponse token = await dat.GetAsync<TokenResponse>(AuthIdentifier);
        if (token != null)
        {
            if (!ForceAuth && !token.IsStale)
            {
                Log.Error("Auth Identifier '{Identifier}' has already been authenticated", AuthIdentifier);
                return -1;
            }

            if (token.IsStale)
            {
                Log.Information("Re-authenticating");
            }
            
            await dat.DeleteAsync<TokenResponse>(AuthIdentifier);
        }
        
        await using FileStream fs = CredentialsFile.OpenRead();
        await GoogleWebAuthorizationBroker.AuthorizeAsync(
             (await GoogleClientSecrets.FromStreamAsync(fs)).Secrets,
             [YouTubeService.Scope.Youtube],
             AuthIdentifier,
             CancellationToken.None, //TODo
             dat,
             new LocalServerCodeReceiver()
        );

        return 0;
    }
}