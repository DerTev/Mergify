using Sharprompt;
using SpotifyAPI.Web;

namespace Mergify.Cli;

public class Merger
{
    private static FullPlaylist? _fromPlaylist;
    private static FullPlaylist? _toPlaylist;

    public static SpotifyClient? SpotifyClient;

    public static async Task SelectPlaylists()
    {
        var currentUser = await SpotifyClient!.UserProfile.Current();
        var paginatedAvailablePlaylists = await SpotifyClient.Playlists.CurrentUsers();
        var availablePlaylists = await SpotifyClient.PaginateAll(paginatedAvailablePlaylists);

        _fromPlaylist = Prompt.Select(new SelectOptions<FullPlaylist>
        {
            Message = "From which playlist do you want to copy?",
            Items = availablePlaylists, //TODO Support starred songs
            TextSelector = playlist => playlist.Name
        });

        _toPlaylist = Prompt.Select(new SelectOptions<FullPlaylist>
        {
            Message = "To which playlist do you want to copy?",
            Items = availablePlaylists.Where(playlist => playlist.Owner!.Id == currentUser.Id),
            TextSelector = playlist => playlist.Name
        });
    }

    public static bool ConfirmMerge()
        => Prompt.Confirm(
            $"Do you really want to merge all Songs from \"{_fromPlaylist!.Name}\" to \"{_toPlaylist!.Name}\"?");

    public static async Task<int> Merge()
    {
        var toPlaylistItems =
            await SpotifyClient!.PaginateAll(await SpotifyClient.Playlists.GetItems(_toPlaylist!.Id!));
        var toPlaylistUris = toPlaylistItems.Select(item => item.GetUri()).ToList();
        var urisToAdd = new List<string>();

        foreach (var track in await SpotifyClient.PaginateAll(
                     await SpotifyClient.Playlists.GetItems(_fromPlaylist!.Id!)))
        {
            var uri = track.GetUri();
            if (!toPlaylistUris.Contains(uri)) urisToAdd.Add(uri);
        }

        await SpotifyClient.Playlists.AddItems(_toPlaylist.Id!, new(urisToAdd));
        return urisToAdd.Count;
    }
}
