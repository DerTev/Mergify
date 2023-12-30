using Mergify.Cli;
using Sharprompt;

new SpotifyLogin(Prompt.Input<string>("Whats your Client-Id?"), async client =>
{
    Console.Clear();
    Console.WriteLine("Successfully logged in.");
    
    Merger.SpotifyClient = client;
    await Merger.SelectPlaylists();
    if (!Merger.ConfirmMerge()) Environment.Exit(0);
    Console.WriteLine("Successfully merged " + await Merger.Merge() + " songs!");
    
    Environment.Exit(0);
});
