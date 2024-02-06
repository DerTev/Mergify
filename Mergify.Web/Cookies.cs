using Microsoft.JSInterop;

namespace Mergify.Web;

public static class Cookies
{
    public static async Task<Dictionary<string, string>> GetCookies(IJSRuntime js)
        => (await js.InvokeAsync<string>("getCookies")).ToDictionary("=|; ");

    public static async Task SetCookie(IJSRuntime js, string key, string value)
        => await js.InvokeVoidAsync("setCookie", key + "=" + value);

    public static async Task DeleteCookie(IJSRuntime js, string key)
        => await SetCookie(js, key, "; expires=Thu, 01 Jan 1970 00:00:01 GMT");
}
