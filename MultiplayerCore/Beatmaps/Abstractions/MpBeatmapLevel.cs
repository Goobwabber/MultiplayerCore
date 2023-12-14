﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static SongCore.Data.ExtraSongData;

namespace MultiplayerCore.Beatmaps.Abstractions
{
    public abstract class MpBeatmapLevel : IPreviewBeatmapLevel
    {
        /// <summary>
        /// The local ID of the level. Can vary between clients.
        /// </summary>
        public virtual string levelID => $"custom_level_{levelHash}";

        /// <summary>
        /// The hash of the level. Should be the same on all clients.
        /// </summary>
        public abstract string levelHash { get; protected set; }

        public abstract string songName { get; }
        public abstract string songSubName { get; }
        public abstract string songAuthorName { get; }
        public abstract string levelAuthorName { get; }

        public virtual float beatsPerMinute { get; protected set; }
        public virtual float songDuration { get; protected set; }
        public virtual float previewStartTime { get; protected set; }
        public virtual float previewDuration { get; protected set; }
        public virtual EnvironmentInfoSO[] environmentInfos { get; private set; }
        public virtual IReadOnlyList<PreviewDifficultyBeatmapSet>? previewDifficultyBeatmapSets { get; protected set; }
        public PlayerSensitivityFlag contentRating { get; }  // Not needed

        public virtual float songTimeOffset { get; protected set; } // Not needed
        public float shuffle { get; private set; } // Not needed
        public float shufflePeriod { get; private set; } // Not needed
        public EnvironmentInfoSO? environmentInfo => null; // Not needed, used for level load
        public EnvironmentInfoSO? allDirectionsEnvironmentInfo => null; // Not needed, used for level load
        public EnvironmentInfoSO[]? environmentInfos => null; // Not needed, used for level load

        // SongCore stuff
        public virtual Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> requirements { get; protected set; } = new();
        public virtual Dictionary<string, Dictionary<BeatmapDifficulty, DifficultyColors>> difficultyColors { get; protected set; } = new();
        public virtual Contributor[]? contributors { get; protected set; } = null!;

        public virtual Task<Sprite> GetCoverImageAsync(CancellationToken cancellationToken)
            => Task.FromResult<Sprite>(null!);
    }
}
