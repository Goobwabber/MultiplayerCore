using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaverSharp;
using BeatSaverSharp.Models;

namespace MultiplayerCore.Beatmaps
{
    public class BeatSaverPreviewMediaData : IPreviewMediaData
    {

        public string LevelHash { get; private set; }
        public BeatSaver BeatSaverClient { get; private set; }
        public Sprite? CoverImagesprite { get; private set; }

        public BeatSaverPreviewMediaData(string levelHash) : this(Plugin._beatsaver, levelHash) {}

        public BeatSaverPreviewMediaData(BeatSaver beatsaver, string levelHash) 
        {
            BeatSaverClient = beatsaver;
            LevelHash = levelHash;
        }

        private Beatmap? _beatmap = null;
        private async Task<Beatmap> GetBeatsaverBeatmap()
        {
            if (_beatmap != null) return _beatmap;
            _beatmap = await BeatSaverClient.BeatmapByHash(LevelHash);
            return _beatmap;
        }

        public async Task<Sprite> GetCoverSpriteAsync(CancellationToken cancellationToken)
        {
            if (CoverImagesprite != null) return CoverImagesprite;

            var bm = await GetBeatsaverBeatmap();
            if (bm == null) return null!;

            byte[]? coverBytes = await bm.LatestVersion.DownloadCoverImage(cancellationToken);
            if (coverBytes == null || coverBytes.Length == 0) return null!;

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(coverBytes);
            CoverImagesprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), 100.0f);
            return CoverImagesprite;
        }

        public async Task<AudioClip> GetPreviewAudioClip(CancellationToken cancellationToken)
        {
            // TODO: something with preview url
            // var bm = await GetBeatsaverBeatmap();
            // bm.LatestVersion.PreviewURL
            return null;
        }

        public void UnloadPreviewAudioClip() {}
    }
}
