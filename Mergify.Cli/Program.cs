using Mergify.Cli;
using Mergify.Logic;
using Sharprompt;
using ShellProgressBar;
using SpotifyAPI.Web;

new AuthFlow(Prompt.Input<string>("Whats your Client-Id?"), async spotifyClient =>
{
    Console.Clear();
    Console.WriteLine("Successfully logged in.");

    var fromPlaylist = Prompt.MultiSelect(new MultiSelectOptions<FullPlaylist>
    {
        Message = "From which playlist do you want to merge?",
        Items = await Merger.GetAvailableFromPlaylists(spotifyClient),
        TextSelector = playlist => playlist.Name
    }).ToList();

    var toPlaylist = Prompt.Select(new SelectOptions<FullPlaylist>
    {
        Message = "Into which playlist do you want to merge?",
        Items = await Merger.GetAvailableToPlaylists(spotifyClient),
        TextSelector = playlist => playlist.Name
    });
    
    if (!Prompt.Confirm(
            "Do you really want to merge all Songs from \""
            + String.Join(", ", fromPlaylist.Select(playlist => playlist.Name))
            + $"\" into \"{toPlaylist.Name}\"?")) Environment.Exit(0);

    var progressBar = new ProgressBar(3, "Starting", new ProgressBarOptions { ProgressBarOnBottom = true });
    Merger.Merge(spotifyClient, fromPlaylist, toPlaylist, state => progressBar.Tick(state.ToString()))
        .GetAwaiter().GetResult();
    
    Environment.Exit(0);
});
