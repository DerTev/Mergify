using SpotifyAPI.Web;

namespace Mergify.Logic;

public static class SpotifyUtils
{
    public static string GetUri(this IPlayableItem track)
    {
        return track switch
        {
            FullTrack fullTrack => fullTrack.Uri,
            FullEpisode fullEpisode => fullEpisode.Uri,
            _ => ""
        };
    }

    public static async Task<IEnumerable<string>> GetUris(this FullPlaylist playlist, SpotifyClient spotifyClient)
        => playlist.Id == null
            ? (await spotifyClient.PaginateAll(await spotifyClient.Library.GetTracks())).Select(item =>
                item.Track.GetUri())
            : (await spotifyClient.PaginateAll(await spotifyClient.Playlists.GetItems(playlist.Id!)))
            .Select(item => item.Track.GetUri());

    public static async Task<IList<FullPlaylist>> GetCurrentUsersPlaylists(this SpotifyClient spotifyClient)
        => await spotifyClient.PaginateAll(await spotifyClient.Playlists.CurrentUsers());
}
