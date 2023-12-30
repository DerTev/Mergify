using SpotifyAPI.Web;

namespace Mergify.Cli;

public static class Extensions
{
    public static string GetUri(this PlaylistTrack<IPlayableItem> track)
    {
        if (track.Track is FullTrack fullTrack) return fullTrack.Uri;
        if (track.Track is FullEpisode fullEpisode) return fullEpisode.Uri;
        return "";
    }
}
