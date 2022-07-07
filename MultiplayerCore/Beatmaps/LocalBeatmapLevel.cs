using MultiplayerCore.Beatmaps.Abstractions;
using SongCore.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static SongCore.Data.ExtraSongData;

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
        public override IReadOnlyList<PreviewDifficultyBeatmapSet>? previewDifficultyBeatmapSets => _preview.previewDifficultyBeatmapSets;

        public override Dictionary<BeatmapDifficulty, string[]> requirements => SongCore.Collections.RetrieveExtraSongData(levelHash)?._difficulties.ToDictionary(x => x._difficulty, x => x.additionalDifficultyData._requirements) ?? new Dictionary<BeatmapDifficulty, string[]>();
        public override Contributor[] contributors => SongCore.Collections.RetrieveExtraSongData(levelHash)?.contributors ?? new Contributor[0];
        public override Dictionary<BeatmapDifficulty, DifficultyColors> difficultyColors => SongCore.Collections.RetrieveExtraSongData(levelHash)?._difficulties.ToDictionary(x => x._difficulty, x
            => new DifficultyColors(x._colorLeft, x._colorRight, x._envColorLeft, x._envColorRight, x._envColorLeftBoost, x._envColorRightBoost, x._obstacleColor)) ?? new Dictionary<BeatmapDifficulty, DifficultyColors>();

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
