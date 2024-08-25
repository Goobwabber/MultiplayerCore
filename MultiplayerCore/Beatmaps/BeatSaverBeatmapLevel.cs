using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp.Models;
using MultiplayerCore.Beatmaps.Abstractions;
using UnityEngine;
using static BeatSaverSharp.Models.BeatmapDifficulty;
using static SongCore.Data.ExtraSongData;

namespace MultiplayerCore.Beatmaps
{
    /// <summary>
    /// Beatmap level data that was loaded remotely from the BeatSaver API.
    /// </summary>
    public class BeatSaverBeatmapLevel : MpBeatmap
    {
        public override string LevelHash { get; protected set; }

        public override string SongName => _beatmap.Metadata.SongName;
        public override string SongSubName => _beatmap.Metadata.SongSubName;
        public override string SongAuthorName => _beatmap.Metadata.SongAuthorName;
        public override string LevelAuthorName => _beatmap.Metadata.LevelAuthorName;
        public override float BeatsPerMinute => _beatmap.Metadata.BPM;
        public override float SongDuration => _beatmap.Metadata.Duration;

        public override Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> Requirements
        {
            get
            {
                Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> reqs = new();
                var difficulties = _beatmap.LatestVersion.Difficulties;
                foreach (var difficulty in difficulties)
                {
                    var characteristic = difficulty.Characteristic.ToString();
                    var difficultyKey = difficulty.Difficulty switch
                    {
                        BeatSaverBeatmapDifficulty.Easy => BeatmapDifficulty.Easy,
                        BeatSaverBeatmapDifficulty.Normal => BeatmapDifficulty.Normal,
                        BeatSaverBeatmapDifficulty.Hard => BeatmapDifficulty.Hard,
                        BeatSaverBeatmapDifficulty.Expert => BeatmapDifficulty.Expert,
                        BeatSaverBeatmapDifficulty.ExpertPlus => BeatmapDifficulty.ExpertPlus,
                        _ => throw new ArgumentOutOfRangeException(nameof(difficulty.Difficulty), $"Unexpected difficulty value: {difficulty.Difficulty}")
                    };
                    if (!reqs.ContainsKey(characteristic))
                        reqs.Add(characteristic, new());
                    string[] diffReqs = new string[0];
                    //if (difficulty.Chroma)
                    //    diffReqs.Append("Chroma");
                    if (difficulty.NoodleExtensions)
                        diffReqs.Append("Noodle Extensions");
                    if (difficulty.MappingExtensions)
                        diffReqs.Append("Mapping Extensions");
                    reqs[characteristic][difficultyKey] = diffReqs;
                }
                return reqs;
            }
        }

        public override Contributor[] Contributors => new Contributor[] { new Contributor
        {
            _role = "Uploader",
            _name = _beatmap.Uploader.Name,
            _iconPath = ""
        }};

        private readonly Beatmap _beatmap;

        public BeatSaverBeatmapLevel(string hash, Beatmap beatmap)
        {
            LevelHash = hash;
            _beatmap = beatmap;
        }

        public override async Task<Sprite> TryGetCoverSpriteAsync(CancellationToken cancellationToken) 
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
