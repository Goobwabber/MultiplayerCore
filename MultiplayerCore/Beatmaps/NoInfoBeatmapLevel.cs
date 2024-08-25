using MultiplayerCore.Beatmaps.Abstractions;

namespace MultiplayerCore.Beatmaps
{
    /// <summary>
    /// Beatmap level data placeholder, used when no information is available.
    /// </summary>
    public class NoInfoBeatmapLevel : MpBeatmap
    {
        public override string LevelHash { get; protected set; }
        public override string SongName => string.Empty;
        public override string SongSubName => string.Empty;
        public override string SongAuthorName => string.Empty;
        public override string LevelAuthorName => string.Empty;

        public NoInfoBeatmapLevel(string hash)
        {
            LevelHash = hash;
        }
    }
}
