using Mergify.Logic;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SpotifyAPI.Web;

namespace Mergify.Web;

public static class AuthFlow
{
    public static async Task<bool> IsLoggingIn(IJSRuntime js, NavigationManager navigationManager)
    {
        var cookieKeys = (await Cookies.GetCookies(js)).Keys;
        var uri = new Uri(navigationManager.Uri);
        var query = uri.Query.ToDictionary("[=&]");
        return cookieKeys.Contains("verifier") && cookieKeys.Contains("challenge") && query.ContainsKey("code");
    }

    private static async Task PrepareLogin(IJSRuntime js, string verifier, string challenge)
    {
        await Cookies.SetCookie(js, "verifier", verifier);
        await Cookies.SetCookie(js, "challenge", challenge);
    }

    public static async Task RequestLogin(IJSRuntime js, NavigationManager navigationManager, string clientId)
    {
        var generatedCodes = (ValueTuple<string, string>)PKCEUtil.GenerateCodes();
        var verifier = generatedCodes.Item1;
        var challenge = generatedCodes.Item2;

        await PrepareLogin(js, verifier, challenge);
        navigationManager.NavigateTo(AuthUtils.GenerateRequest(clientId, challenge, navigationManager.BaseUri).ToUri()
            .ToString());
    }

    public static async Task ClearLoginCookies(IJSRuntime js)
    {
        await Cookies.DeleteCookie(js, "verifier");
        await Cookies.DeleteCookie(js, "challenge");
    }

    public static async Task HandleLogin(IJSRuntime js)
    {
        //TODO

        await ClearLoginCookies(js);
    }
}
