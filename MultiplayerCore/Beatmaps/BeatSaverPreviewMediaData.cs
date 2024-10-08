﻿using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
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
        private async Task<Beatmap?> GetBeatsaverBeatmap()
        {
            if (_beatmap != null) return _beatmap;
            _beatmap = await BeatSaverClient.BeatmapByHash(LevelHash);
            return _beatmap;
        }

        public async Task<Sprite> GetCoverSpriteAsync()
        {
            if (CoverImagesprite != null) return CoverImagesprite;

            var bm = await GetBeatsaverBeatmap();
            if (bm == null) return null!;

            byte[]? coverBytes = await bm.LatestVersion.DownloadCoverImage();
            if (coverBytes == null || coverBytes.Length == 0) return null!;

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(coverBytes);
            CoverImagesprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), 100.0f);
            return CoverImagesprite;
        }

        public void UnloadCoverSprite()
        {
            if (CoverImagesprite == null) return;
            Object.Destroy(CoverImagesprite.texture);
            Object.Destroy(CoverImagesprite);
            CoverImagesprite = null;
        }

        public Task<AudioClip> GetPreviewAudioClip()
        {
            // TODO: something with preview url
            //var bm = await GetBeatsaverBeatmap();
            // bm.LatestVersion.PreviewURL
            return null!;
        }

        public void UnloadPreviewAudioClip() {}
    }
}
