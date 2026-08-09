using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;

namespace YouTubeBroadcastCreator.Util.API;

public class ExceptionCodeReceiver(string message = "") : ICodeReceiver
{
    public string RedirectUri => "";

    public Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken) => throw new InvalidOperationException(message);
}