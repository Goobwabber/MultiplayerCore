using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MultiplayerCore.Beatmaps.Serializable;
using UnityEngine;
using static SongCore.Data.ExtraSongData;

namespace MultiplayerCore.Beatmaps.Abstractions
{
    /// <summary>
    /// Base class for Beatmap data that can be used in multiplayer.
    /// </summary>
    public abstract class MpBeatmap
    {
        /// <summary>
        /// The hash of the level. Should be the same on all clients.
        /// </summary>
        public abstract string LevelHash { get; protected set; }
        /// <summary>
        /// The local ID of the level. Can vary between clients.
        /// </summary>
        public string LevelID => $"custom_level_{LevelHash}";
        public abstract string SongName { get; }
        public abstract string SongSubName { get; }
        public abstract string SongAuthorName { get; }
        public abstract string LevelAuthorName { get; }
        public virtual float BeatsPerMinute { get; protected set; }
        public virtual float SongDuration { get; protected set; }
        public virtual Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> Requirements { get; protected set; } = new();
        public virtual Dictionary<string, Dictionary<BeatmapDifficulty, DifficultyColors>> DifficultyColors { get; protected set; } = new();
        public virtual Contributor[]? Contributors { get; protected set; } = null!;
        
        public virtual Task<Sprite> TryGetCoverSpriteAsync(CancellationToken cancellationToken)
            => Task.FromResult<Sprite>(null!);
    }
}