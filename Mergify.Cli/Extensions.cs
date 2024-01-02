using SpotifyAPI.Web;

namespace Mergify.Cli;

public static class Extensions
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
}
