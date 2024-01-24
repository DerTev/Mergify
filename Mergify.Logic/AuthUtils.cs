using SpotifyAPI.Web;

namespace Mergify.Logic;

public static class AuthUtils
{
    public static LoginRequest GenerateRequest(string clientId, string codeChallenge, string redirectUri)
    {
        return new LoginRequest(new Uri(redirectUri), clientId, LoginRequest.ResponseType.Code)
        {
            CodeChallengeMethod = "S256",
            CodeChallenge = codeChallenge,
            Scope = new[]
            {
                Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.PlaylistReadPrivate,
                Scopes.PlaylistReadCollaborative, Scopes.UserLibraryRead
            }
        };
    }

    public static async Task<SpotifyClient> ProcessAuth(string clientId, string verifier, string code)
    {
        var initialResponse = await new OAuthClient().RequestToken(new PKCETokenRequest(clientId, code,
            new Uri("http://localhost:8080/callback"), verifier));
        return new SpotifyClient(initialResponse.AccessToken);
    }
}
