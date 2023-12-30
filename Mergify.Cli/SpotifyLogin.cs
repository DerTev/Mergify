using System.Diagnostics;
using System.Text;
using EmbedIO.Actions;
using EmbedIO.Utilities;
using Mergify.Cli.WebInterface;
using SpotifyAPI.Web;

namespace Mergify.Cli;

using EmbedIO;

public class SpotifyLogin
{
    private readonly Func<SpotifyClient, Task> _callback;
    private readonly string _clientId;
    private readonly string _verifier;
    private readonly string _challenge;
    private readonly WebServer _webServer;

    private LoginRequest GenerateRequest()
    {
        return new LoginRequest(
            new Uri("http://localhost:8080/callback"),
            _clientId,
            LoginRequest.ResponseType.Code
        )
        {
            CodeChallengeMethod = "S256",
            CodeChallenge = _challenge,
            Scope = new[]
            {
                Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.PlaylistReadPrivate,
                Scopes.PlaylistReadCollaborative, Scopes.UserLibraryRead
            }
        };
    }

    private void RequestLogin()
    {
        var url = GenerateRequest().ToUri().OriginalString;
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        Console.WriteLine(
            $"Your browser should have been opened now. If not, please open the login via {url} yourself!");
    }

    private async Task HandleLogin(IHttpContext context)
    {
        if (!context.GetRequestQueryData().ContainsKey("code"))
        {
            var invalidContent = await Renderer.RenderComponent(typeof(Invalid), new Dictionary<string, object?>());
            await context.SendStringAsync(invalidContent, "text/html", Encoding.Default);
            return;
        }

        var initialResponse =
            await new OAuthClient().RequestToken(new PKCETokenRequest(_clientId, context.GetRequestQueryData()["code"]!,
                new Uri("http://localhost:8080/callback"), _verifier));
        var spotifyClient = new SpotifyClient(initialResponse.AccessToken);
        var currenUser = await spotifyClient.UserProfile.Current();
        var successfulContent = await Renderer.RenderComponent(typeof(Successful),
            new Dictionary<string, object?> { { "DisplayName", currenUser.DisplayName } });
        context.SendStringAsync(successfulContent, "text/html", Encoding.Default).GetAwaiter().GetResult();
        _callback(spotifyClient);
    }

    private WebServer BuildWebServer()
        => new WebServer(options => options.WithUrlPrefix("http://localhost:8080/")
                .WithMode(HttpListenerMode.EmbedIO))
            .WithModule(new ActionModule("/callback", HttpVerbs.Get, HandleLogin));

    public SpotifyLogin(string clientId, Func<SpotifyClient, Task> callback)
    {
        _callback = callback;
        _clientId = clientId;

        var (verifier, challenge) = PKCEUtil.GenerateCodes();
        _verifier = verifier;
        _challenge = challenge;

        _webServer = BuildWebServer();
        _webServer.StateChanged += (_, args) =>
        {
            if (args.NewState == WebServerState.Listening) RequestLogin();
        };
        _webServer.RunAsync().GetAwaiter().GetResult();
    }
}
