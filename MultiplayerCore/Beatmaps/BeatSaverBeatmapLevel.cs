using BeatSaverSharp.Models;
using MultiplayerCore.Beatmaps.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static BeatSaverSharp.Models.BeatmapDifficulty;
using static SongCore.Data.ExtraSongData;

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

        public override Dictionary<BeatmapDifficulty, string[]> requirements => _beatmap.LatestVersion.Difficulties.ToDictionary(x => x.Difficulty switch
        {
            BeatSaverBeatmapDifficulty.Easy => BeatmapDifficulty.Easy,
            BeatSaverBeatmapDifficulty.Normal => BeatmapDifficulty.Normal,
            BeatSaverBeatmapDifficulty.Hard => BeatmapDifficulty.Hard,
            BeatSaverBeatmapDifficulty.Expert => BeatmapDifficulty.Expert,
            BeatSaverBeatmapDifficulty.ExpertPlus => BeatmapDifficulty.ExpertPlus,
            _ => throw new ArgumentOutOfRangeException(nameof(x.Difficulty), $"Unexpected difficulty value: {x.Difficulty}")
        }, x =>
        {
            string[] requirements = new string[0];
            if (x.Chroma)
                requirements.Append("Chroma");
            if (x.NoodleExtensions)
                requirements.Append("Noodle Extensions");
            if (x.MappingExtensions)
                requirements.Append("Mapping Extensions");
            return requirements;
        });

        public override Contributor[] contributors 
        { 
            get
            {
                var contributors = new Contributor[]
                {
                    new Contributor
                    {
                        _role = "Uploader",
                        _name = _beatmap.Uploader.Name,
                        _iconPath = ""
                    }
                };

                if (_beatmap.BeatmapCurator != null)
                    contributors.Append(new Contributor
                    {
                        _role = "Curator",
                        _name = _beatmap.BeatmapCurator.Name,
                        _iconPath = ""
                    });

                return contributors;
            }
        }

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
