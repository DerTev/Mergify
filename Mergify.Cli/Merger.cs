using Sharprompt;
using ShellProgressBar;
using SpotifyAPI.Web;

namespace Mergify.Cli;

public class Merger
{
    private static IEnumerable<FullPlaylist>? _fromPlaylists;
    private static FullPlaylist? _toPlaylist;

    public static SpotifyClient? SpotifyClient;

    public static async Task SelectPlaylists()
    {
        var currentUser = await SpotifyClient!.UserProfile.Current();
        var paginatedAvailablePlaylists = await SpotifyClient.Playlists.CurrentUsers();
        var availablePlaylists = await SpotifyClient.PaginateAll(paginatedAvailablePlaylists);

        _fromPlaylists = Prompt.MultiSelect(new MultiSelectOptions<FullPlaylist>
        {
            Message = "From which playlist do you want to merge?",
            Items = availablePlaylists.Concat(new[] { new FullPlaylist { Name = "Saved Tracks" } }),
            TextSelector = playlist => playlist.Name
        });

        _toPlaylist = Prompt.Select(new SelectOptions<FullPlaylist>
        {
            Message = "Into which playlist do you want to merge?",
            Items = availablePlaylists.Where(playlist => playlist.Owner!.Id == currentUser.Id),
            TextSelector = playlist => playlist.Name
        });
    }

    public static bool ConfirmMerge()
        => Prompt.Confirm(
            "Do you really want to merge all Songs from \""
            + String.Join(", ", _fromPlaylists!.Select(playlist => playlist.Name))
            + $"\" into \"{_toPlaylist!.Name}\"?");

    private static async Task IndexPlaylist(FullPlaylist playlist, List<string> indexedItems)
    {
        indexedItems.AddRange(playlist.Id == null
            ? (await SpotifyClient!.PaginateAll(await SpotifyClient.Library.GetTracks())).Select(item =>
                item.Track.GetUri())
            : (await SpotifyClient!.PaginateAll(await SpotifyClient.Playlists.GetItems(playlist.Id!))).Select(
                item => item.Track.GetUri()));
    }

    public static async Task Merge()
    {
        var indexedItems = new List<string>();
        var urisToAdd = new List<string>();

        foreach (var fromPlaylist in _fromPlaylists!) await IndexPlaylist(fromPlaylist, indexedItems);
        var toPlaylistUris =
            (await SpotifyClient!.PaginateAll(await SpotifyClient.Playlists.GetItems(_toPlaylist!.Id!)))
            .Select(item => item.Track.GetUri()).ToList();

        var progressBar = new ProgressBar(indexedItems.Count + 2, "Indexing tracks...",
            new ProgressBarOptions { ProgressBarOnBottom = true });
        foreach (var track in indexedItems)
        {
            if (!toPlaylistUris.Contains(track)) urisToAdd.Add(track);
            progressBar.Tick();
        }

        progressBar.Tick($"Adding {urisToAdd.Count} tracks...");
        foreach (var uris in urisToAdd.Chunk(100)) await SpotifyClient.Playlists.AddItems(_toPlaylist.Id!, new(uris));
        progressBar.Tick($"Successfully added {urisToAdd.Count} tracks!");
    }
}
