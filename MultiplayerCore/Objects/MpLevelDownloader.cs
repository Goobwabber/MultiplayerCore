using BeatSaverSharp;
using BeatSaverSharp.Models;
using MultiplayerCore.Helpers;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerCore.Objects
{
    internal class MpLevelDownloader
    {
        public readonly string CustomLevelsFolder = Path.Combine(Application.dataPath, Plugin.CustomLevelsPath);

        private ConcurrentDictionary<string, Task<bool>> _downloads = new();
        private readonly ZipExtractor _zipExtractor = new();
        private readonly BeatSaver _beatsaver;
        private readonly SiraLog _logger;
        
        internal MpLevelDownloader(
            UBinder<Plugin, BeatSaver> beatsaver,
            SiraLog logger)
        {
            _beatsaver = beatsaver.Value;
            _logger = logger;
        }

        /// <summary>
        /// Gets the download task for a level if it is downloading.
        /// </summary>
        /// <param name="levelId">Level to check for</param>
        /// <param name="task">Download task</param>
        /// <returns>Whether level is downloading or not</returns>
        public bool TryGetDownload(string levelId, out Task<bool> task)
            => _downloads.TryGetValue(levelId, out task);

        /// <summary>
        /// Tries to download a level.
        /// </summary>
        /// <param name="levelId">Level to download</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progress">Progress object</param>
        /// <returns>Task that is false when download fails and true when successful</returns>
        public Task<bool> TryDownloadLevel(string levelId, CancellationToken cancellationToken, IProgress<double>? progress = null)
        {
            Task<bool> task;
            if (_downloads.TryGetValue(levelId, out task))
            {
                if (!task.IsCompleted)
                    _logger.Debug($"Download already in progress: {levelId}");
                if (task.IsCompleted && task.Result)
                    _logger.Debug($"Download already finished: {levelId}");
            }
            if (task == null || (task.IsCompleted && !task.Result))
            {
                _logger.Debug($"Starting download: {levelId}");
                task = TryDownloadLevelInternal(levelId, cancellationToken, progress);
                _downloads[levelId] = task;
            }
            return task;
        }

        private async Task<bool> TryDownloadLevelInternal(string levelId, CancellationToken cancellationToken, IProgress<double>? progress = null)
        {
            string levelHash = Utilities.HashForLevelID(levelId);
            if (string.IsNullOrEmpty(levelHash))
            {
                _logger.Error($"Could not parse hash from id {levelId}");
                return false;
            }

            try
            {
                await DownloadLevel(levelHash, cancellationToken, progress);
                _logger.Debug($"Download finished: {levelId}");
                _downloads.TryRemove(levelId, out _);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.Debug($"Download cancelled: {levelId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Download failed: {levelId} {ex.Message}");
                _logger.Debug(ex);
            }
            return false;
        }

        private async Task DownloadLevel(string levelHash, CancellationToken cancellationToken, IProgress<double>? progress = null)
        {
            Beatmap? beatmap = await _beatsaver.BeatmapByHash(levelHash, cancellationToken);
            if (beatmap == null)
                throw new Exception("Not found on BeatSaver.");

            BeatmapVersion? beatmapVersion = beatmap.Versions.FirstOrDefault(x => string.Equals(x.Hash, levelHash, StringComparison.OrdinalIgnoreCase));
            if (beatmapVersion == null!)
                throw new Exception("Not found in versions provided by BeatSaver.");
            byte[]? beatmapBytes = await beatmapVersion.DownloadZIP(cancellationToken, progress);

            string folderPath = GetSongDirectoryName(beatmap.LatestVersion.Key, beatmap.Metadata.SongName, beatmap.Metadata.LevelAuthorName);
            folderPath = Path.Combine(CustomLevelsFolder, folderPath);
            using (MemoryStream memoryStream = new MemoryStream(beatmapBytes))
            {
                var result = await _zipExtractor.ExtractZip(memoryStream, folderPath);
                if (folderPath != result.OutputDirectory)
                    folderPath = result.OutputDirectory ?? throw new Exception("Zip extract failed, no output directory.");
                if (result.Exception != null)
                    throw result.Exception;
            }

            using (var awaiter = new EventAwaiter<SongCore.Loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel>>(cancellationToken))
            {
                try
                {
                    SongCore.Loader.SongsLoadedEvent += awaiter.OnEvent;
                    SongCore.Loader.Instance.RefreshSongs(false);
                    await awaiter.Task;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    SongCore.Loader.SongsLoadedEvent -= awaiter.OnEvent;
                }
            }
        }

        private string GetSongDirectoryName(string? songKey, string songName, string levelAuthorName)
        {
            // BeatSaverDownloader's method of naming the directory.
            string basePath;
            string nameAuthor;
            if (string.IsNullOrEmpty(levelAuthorName))
                nameAuthor = songName;
            else
                nameAuthor = $"{songName} - {levelAuthorName}";
            songKey = songKey?.Trim();
            if (songKey != null && songKey.Length > 0)
                basePath = songKey + " (" + nameAuthor + ")";
            else
                basePath = nameAuthor;
            basePath = string.Concat(basePath.Trim().Split(_invalidPathChars));
            return basePath;
        }

        private readonly char[] _invalidPathChars = new char[]
        {
            '<', '>', ':', '/', '\\', '|', '?', '*', '"',
            '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007',
            '\u0008', '\u0009', '\u000a', '\u000b', '\u000c', '\u000d', '\u000e', '\u000d',
            '\u000f', '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016',
            '\u0017', '\u0018', '\u0019', '\u001a', '\u001b', '\u001c', '\u001d', '\u001f',
        }.Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
    }
}
