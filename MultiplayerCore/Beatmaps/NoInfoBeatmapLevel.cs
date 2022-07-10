using MultiplayerCore.Beatmaps.Abstractions;

namespace MultiplayerCore.Beatmaps
{
    public class NoInfoBeatmapLevel : MpBeatmapLevel
    {
        public override string levelHash { get; protected set; }
        public override string songName => string.Empty;
        public override string songSubName => string.Empty;
        public override string songAuthorName => string.Empty;
        public override string levelAuthorName => string.Empty;

        public NoInfoBeatmapLevel(string hash)
        {
            levelHash = hash;
        }
    }
}
