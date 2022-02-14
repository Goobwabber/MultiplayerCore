using MultiplayerCore.Beatmaps.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerCore.Beatmaps
{
    /// <summary>
    /// An <see cref="IPreviewBeatmapLevel"/> created from a local preview.
    /// </summary>
    public class LocalBeatmapLevel : MpBeatmapLevel
    {
        public override string levelHash { get; protected set; }

        public override string songName => _preview.songName;
        public override string songSubName => _preview.songSubName;
        public override string songAuthorName => _preview.songAuthorName;
        public override string levelAuthorName => _preview.levelAuthorName;

        public override float beatsPerMinute => _preview.beatsPerMinute;
        public override float songDuration => _preview.songDuration;
        public override float previewStartTime => _preview.previewStartTime;
        public override float previewDuration => _preview.previewDuration;
        public override PreviewDifficultyBeatmapSet[]? previewDifficultyBeatmapSets => _preview.previewDifficultyBeatmapSets;

        private readonly IPreviewBeatmapLevel _preview;

        public LocalBeatmapLevel(string hash, IPreviewBeatmapLevel preview)
        {
            levelHash = hash;
            _preview = preview;
        }

        public override Task<Sprite> GetCoverImageAsync(CancellationToken cancellationToken)
            => _preview.GetCoverImageAsync(cancellationToken);
    }
}
