using SpotifyAPI.Web;

namespace Mergify.Logic;

public static class Merger
{
    private static async Task<IList<FullPlaylist>> GetSavedPlaylists(SpotifyClient spotifyClient)
        => await spotifyClient.GetCurrentUsersPlaylists();

    public static async Task<List<FullPlaylist>> GetAvailableFromPlaylists(SpotifyClient spotifyClient)
        => (await GetSavedPlaylists(spotifyClient))
            .Concat(new[] { new FullPlaylist { Name = "Saved Tracks", Uri = "mergify:custom:saved-tracks" } }).ToList();

    public static async Task<List<FullPlaylist>> GetAvailableToPlaylists(SpotifyClient spotifyClient)
    {
        var savedPlaylists = await GetSavedPlaylists(spotifyClient);
        var result = new List<FullPlaylist>();

        foreach (var savedPlaylist in savedPlaylists)
        {
            if (savedPlaylist.Owner!.Id == (await spotifyClient.UserProfile.Current()).Id)
                result.Add(savedPlaylist);
        }

        return result;
    }

    public static async Task Merge(SpotifyClient spotifyClient, List<FullPlaylist> fromPlaylists,
        FullPlaylist toPlaylist, Action<MergeState> stateListener)
    {
        stateListener(MergeState.Indexing);
        var indexedItems = new List<string>();
        foreach (var fromPlaylist in fromPlaylists)
            indexedItems.AddRange(await fromPlaylist.GetUris(spotifyClient));
        var urisToAdd = new List<string>();
        var toPlaylistUris =
            (await spotifyClient.PaginateAll(await spotifyClient.Playlists.GetItems(toPlaylist.Id!)))
            .Select(item => item.Track.GetUri()).ToList();
        foreach (var track in indexedItems)
        {
            if (!toPlaylistUris.Contains(track) && !urisToAdd.Contains(track)) urisToAdd.Add(track);
        }

        stateListener(MergeState.Adding);
        foreach (var uris in urisToAdd.Chunk(100)) await spotifyClient.Playlists.AddItems(toPlaylist.Id!, new(uris));
        stateListener(MergeState.Finished);
    }
}
