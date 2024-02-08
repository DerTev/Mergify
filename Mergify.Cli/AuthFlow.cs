using System.Diagnostics;
using System.Text;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Utilities;
using Mergify.Cli.WebInterface;
using Mergify.Logic;
using SpotifyAPI.Web;

namespace Mergify.Cli;

public class AuthFlow
{
    private readonly string _clientId;
    private readonly Func<SpotifyClient, Task> _callback;

    private readonly string _verifier;
    private readonly string _challenge;
    private readonly WebServer _webServer;

    private async Task Handler(IHttpContext context)
    {
        if (!context.GetRequestQueryData().ContainsKey("code"))
        {
            var invalidContent = await Renderer.RenderComponent(typeof(Invalid), new Dictionary<string, object?>());
            await context.SendStringAsync(invalidContent, "text/html", Encoding.Default);
            return;
        }

        var spotifyClient = await AuthUtils.ProcessAuth("http://localhost:8080/callback", _clientId, _verifier,
            context.GetRequestQueryData()["code"]!);
        Task.Run(async () => await _callback(spotifyClient));

        var successfulContent = await Renderer.RenderComponent(typeof(Successful),
            new Dictionary<string, object?>
                { { "DisplayName", (await spotifyClient.UserProfile.Current()).DisplayName } });
        context.SendStringAsync(successfulContent, "text/html", Encoding.Default).GetAwaiter().GetResult();
    }

    public void RequestAuth()
    {
        var url = AuthUtils.GenerateRequest(_clientId, _challenge, "http://localhost:8080/callback").ToUri()
            .OriginalString;
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        Console.WriteLine(
            $"Your browser should have been opened now. If not, please open the login via {url} yourself!");
    }

    public AuthFlow(string clientId, Func<SpotifyClient, Task> callback)
    {
        _clientId = clientId;
        _callback = callback;

        (_verifier, _challenge) = PKCEUtil.GenerateCodes();

        _webServer = new WebServer(options => options.WithUrlPrefix("http://localhost:8080/")
                .WithMode(HttpListenerMode.EmbedIO))
            .WithModule(new ActionModule("/callback", HttpVerbs.Get, Handler));
        _webServer.StateChanged += (_, args) =>
        {
            if (args.NewState == WebServerState.Listening) RequestAuth();
        };
        _webServer.OnHttpException += async (context, exception) =>
        {
            if (exception.StatusCode != 404)
            {
                await HttpExceptionHandler.Default(context, exception);
                Environment.Exit(1);
            }
        };

        _webServer.RunAsync().GetAwaiter().GetResult();
    }
}
