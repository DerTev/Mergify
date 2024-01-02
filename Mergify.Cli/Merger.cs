using Sharprompt;
using ShellProgressBar;
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
            $"Do you really want to merge all Songs from \"{_fromPlaylist!.Name}\" into \"{_toPlaylist!.Name}\"?");

    public static async Task Merge()
    {
        var toPlaylistItems =
            await SpotifyClient!.PaginateAll(await SpotifyClient.Playlists.GetItems(_toPlaylist!.Id!));
        var toPlaylistUris = toPlaylistItems.Select(item => item.Track.GetUri()).ToList();
        var urisToAdd = new List<string>();

        var fromTracks = _fromPlaylist!.Id == null
            ? (await SpotifyClient.PaginateAll(await SpotifyClient.Library.GetTracks())).Select(item => item.Track)
            : (await SpotifyClient.PaginateAll(await SpotifyClient.Playlists.GetItems(_fromPlaylist.Id!))).Select(
                item => item.Track);
        var progressBar = new ProgressBar(fromTracks.Count() + 2, "Indexing tracks...",
            new ProgressBarOptions { ProgressBarOnBottom = true });
        foreach (var track in fromTracks)
        {
            var uri = track.GetUri();
            if (!toPlaylistUris.Contains(uri)) urisToAdd.Add(uri);
            progressBar.Tick();
        }

        progressBar.Tick($"Adding {urisToAdd.Count} tracks...");
        foreach (var uris in urisToAdd.Chunk(100)) await SpotifyClient.Playlists.AddItems(_toPlaylist.Id!, new(uris));
        progressBar.Tick($"Successfully added {urisToAdd.Count} tracks!");
    }
}
