using Mergify.Logic;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SpotifyAPI.Web;

namespace Mergify.Web;

public static class AuthFlow
{
    private static IDictionary<string, string> GetQueryParams(NavigationManager navigationManager)
        => new Uri(navigationManager.Uri).Query.Replace("?", "").ToDictionary("[=&]");

    public static async Task<bool> IsLoggingIn(IJSRuntime js, NavigationManager navigationManager)
    {
        var cookieKeys = (await Cookies.GetCookies(js)).Keys;
        var uri = new Uri(navigationManager.Uri);
        var query = uri.Query.ToDictionary("[=&]");
        return cookieKeys.Contains("verifier") && GetQueryParams(navigationManager).ContainsKey("code");
    }

    private static async Task PrepareLogin(IJSRuntime js, string clientId, string verifier, string challenge)
    {
        await Cookies.SetCookie(js, "clientId", clientId);
        await Cookies.SetCookie(js, "verifier", verifier);
    }

    public static async Task RequestLogin(IJSRuntime js, NavigationManager navigationManager, string clientId)
    {
        var generatedCodes = (ValueTuple<string, string>)PKCEUtil.GenerateCodes();
        var verifier = generatedCodes.Item1;
        var challenge = generatedCodes.Item2;

        await PrepareLogin(js, clientId, verifier, challenge);
        navigationManager.NavigateTo(AuthUtils.GenerateRequest(clientId, challenge, navigationManager.BaseUri).ToUri()
            .ToString());
    }

    public static async Task ClearLoginCookies(IJSRuntime js)
    {
        await Cookies.DeleteCookie(js, "clientId");
        await Cookies.DeleteCookie(js, "verifier");
    }

    public static async Task<SpotifyClient> HandleLogin(IJSRuntime js, NavigationManager navigationManager)
    {
        var cookies = await Cookies.GetCookies(js);
        await ClearLoginCookies(js);

        return await AuthUtils.ProcessAuth(navigationManager.BaseUri, cookies["clientId"], cookies["verifier"],
            GetQueryParams(navigationManager)["code"]);
    }
}
