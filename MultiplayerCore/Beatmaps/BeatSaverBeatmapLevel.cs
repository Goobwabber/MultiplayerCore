using BeatSaverSharp.Models;
using MultiplayerCore.Beatmaps.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerCore.Beatmaps
{
    /// <summary>
    /// An <see cref="IPreviewBeatmapLevel"/> created from data from BeatSaver.
    /// </summary>
    public class BeatSaverBeatmapLevel : MpBeatmapLevel
    {
        public override string levelHash { get; protected set; }

        public override string songName => _beatmap.Metadata.SongName;
        public override string songSubName => _beatmap.Metadata.SongSubName;
        public override string songAuthorName => _beatmap.Metadata.SongAuthorName;
        public override string levelAuthorName => _beatmap.Metadata.LevelAuthorName;
        public override float beatsPerMinute => _beatmap.Metadata.BPM;
        public override float songDuration => _beatmap.Metadata.Duration;

        private readonly Beatmap _beatmap;

        public BeatSaverBeatmapLevel(string hash, Beatmap beatmap)
        {
            levelHash = hash;
            _beatmap = beatmap;
        }

        public override async Task<Sprite> GetCoverImageAsync(CancellationToken cancellationToken) 
        {
            byte[]? coverBytes = await _beatmap.LatestVersion.DownloadCoverImage(cancellationToken);
            if (coverBytes == null || coverBytes.Length == 0)
                return null!;
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(coverBytes);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), 100.0f);
        }
    }
}
