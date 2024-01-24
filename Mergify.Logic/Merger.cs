using SpotifyAPI.Web;

namespace Mergify.Logic;

public static class Merger
{
    private static async Task<IList<FullPlaylist>> GetSavedPlaylists(SpotifyClient spotifyClient)
        => await spotifyClient.GetCurrentUsersPlaylists();

    public static async Task<List<FullPlaylist>> GetAvailableFromPlaylists(SpotifyClient spotifyClient)
        => (await GetSavedPlaylists(spotifyClient))
            .Concat(new[] { new FullPlaylist { Name = "Saved Tracks" } }).ToList();

    public static async Task<List<FullPlaylist>> GetAvailableToPlaylists(SpotifyClient spotifyClient)
        => (await GetSavedPlaylists(spotifyClient))
            .Where(playlist => playlist.Owner!.Id == spotifyClient.UserProfile.Current().GetAwaiter().GetResult().Id)
            .ToList();

    public static async Task Merge(SpotifyClient spotifyClient, List<FullPlaylist> fromPlaylists,
        FullPlaylist toPlaylist, Action<MergeState> stateListener)
    {
        stateListener(MergeState.Indexing);
        var indexedItems = new List<string>();
        fromPlaylists.ForEach(fromPlaylist =>
            indexedItems.AddRange(fromPlaylist.GetUris(spotifyClient).GetAwaiter().GetResult()));
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
