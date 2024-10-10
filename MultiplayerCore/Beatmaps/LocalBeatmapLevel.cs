using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Serializable;
using UnityEngine;
using static SongCore.Data.ExtraSongData;

namespace MultiplayerCore.Beatmaps
{
    /// <summary>
    /// Beatmap level data that was loaded locally by SongCore.
    /// </summary>
    public class LocalBeatmapLevel : MpBeatmap
    {
        public override string LevelHash { get; protected set; }

        public override string SongName => _localBeatmapLevel.songName;
        public override string SongSubName => _localBeatmapLevel.songSubName;
        public override string SongAuthorName => _localBeatmapLevel.songAuthorName;
        public override string LevelAuthorName => string.Join(", ", _localBeatmapLevel.allMappers);

        public override float BeatsPerMinute => _localBeatmapLevel.beatsPerMinute;
        public override float SongDuration => _localBeatmapLevel.songDuration;
        
        public override Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> Requirements
        {
            get
            {
                Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> reqs = new();
                var difficulties = SongCore.Collections.RetrieveExtraSongData(LevelHash)?._difficulties;
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

        public override Dictionary<string, Dictionary<BeatmapDifficulty, DifficultyColors>> DifficultyColors
        {
            get
            {
                Dictionary<string, Dictionary<BeatmapDifficulty, DifficultyColors>> colors = new();
                var difficulties = SongCore.Collections.RetrieveExtraSongData(LevelHash)?._difficulties;
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

        public override Contributor[] Contributors => SongCore.Collections.RetrieveExtraSongData(LevelHash)?.contributors ?? new Contributor[0];

        private readonly BeatmapLevel _localBeatmapLevel;

        public LocalBeatmapLevel(string hash, BeatmapLevel localBeatmapLevel)
        {
            LevelHash = hash;
            _localBeatmapLevel = localBeatmapLevel;
        }

        public override Task<Sprite> TryGetCoverSpriteAsync(CancellationToken cancellationToken)
            => _localBeatmapLevel.previewMediaData.GetCoverSpriteAsync();
    }
}
