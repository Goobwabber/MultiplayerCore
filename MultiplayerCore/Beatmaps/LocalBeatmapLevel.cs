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

        public override Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> requirements
        {
            get
            {
                Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> reqs = new();
                var difficulties = SongCore.Collections.RetrieveExtraSongData(levelHash)?._difficulties;
                if (difficulties == null)
                    return new();
                foreach (var difficulty in difficulties)
                {
                    if (!reqs.ContainsKey(difficulty._beatmapCharacteristicName))
                        reqs.Add(difficulty._beatmapCharacteristicName, new());
                    reqs[difficulty._beatmapCharacteristicName][difficulty._difficulty] = difficulty.additionalDifficultyData._requirements;
                }
                return reqs;
            }
        }

        public override Dictionary<string, Dictionary<BeatmapDifficulty, DifficultyColors>> difficultyColors
        {
            get
            {
                Dictionary<string, Dictionary<BeatmapDifficulty, DifficultyColors>> colors = new();
                var difficulties = SongCore.Collections.RetrieveExtraSongData(levelHash)?._difficulties;
                if (difficulties == null)
                    return new();
                foreach (var difficulty in difficulties)
                {
                    if (!colors.ContainsKey(difficulty._beatmapCharacteristicName))
                        colors.Add(difficulty._beatmapCharacteristicName, new());
                    colors[difficulty._beatmapCharacteristicName][difficulty._difficulty]
                        = new DifficultyColors(difficulty._colorLeft, difficulty._colorRight, difficulty._envColorLeft, difficulty._envColorRight, difficulty._envColorLeftBoost, difficulty._envColorRightBoost, difficulty._obstacleColor);
                }
                return colors;
            }
        }

        public override Contributor[] contributors => SongCore.Collections.RetrieveExtraSongData(levelHash)?.contributors ?? new Contributor[0];

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
