using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using SiraUtil.Logging;

namespace MultiplayerCore.Objects
{
    public class MpLevelLoader : MultiplayerLevelLoader, IProgress<double>
    {
        public event Action<double> progressUpdated = null!;

        public ILevelGameplaySetupData? CurrentLoadingData => _gameplaySetupData;

        internal readonly IMultiplayerSessionManager _sessionManager;
		internal readonly MpLevelDownloader _levelDownloader;
		internal readonly MpEntitlementChecker _entitlementChecker;
		internal readonly IMenuRpcManager _rpcManager;
		internal readonly SiraLog _logger;

        internal MpLevelLoader(
            IMultiplayerSessionManager sessionManager,
            MpLevelDownloader levelDownloader,
            NetworkPlayerEntitlementChecker entitlementChecker,
            IMenuRpcManager rpcManager,
            SiraLog logger)
        {
            _sessionManager = sessionManager;
            _levelDownloader = levelDownloader;
            _entitlementChecker = (entitlementChecker as MpEntitlementChecker)!;
            _rpcManager = rpcManager;
            _logger = logger;
        }

        [UsedImplicitly]
        public void LoadLevel_override(string levelId)
        {
			var levelHash = Utilities.HashForLevelID(levelId);
            
            if (levelHash == null)
            {
                _logger.Debug($"Ignoring level (not a custom level hash): {levelId}");
                return;
            }
            
            var downloadNeeded = !SongCore.Collections.songWithHashPresent(levelHash);
            
            _logger.Debug($"Loading level: {levelId} (downloadNeeded={downloadNeeded})");
            
            if (downloadNeeded)
                _getBeatmapLevelResultTask = DownloadBeatmapLevelAsync(levelId, _getBeatmapCancellationTokenSource.Token);
        }

        internal void UnloadLevelIfRequirementsNotMet()
        {
            // Extra: load finished, check if there are extra requirements in place
            // If we fail requirements, unload the level
            
            var beatmapKey = _gameplaySetupData.beatmapKey;
            var levelId = beatmapKey.levelId;
            
            var levelHash = Utilities.HashForLevelID(levelId);
            if (levelHash == null)
                return;
            
            var SongData = SongCore.Collections.GetCustomLevelSongData(levelId);
            if (SongData == null)
                return;

            var difficulty = _gameplaySetupData.beatmapKey.difficulty;
            var characteristicName = _gameplaySetupData.beatmapKey.beatmapCharacteristic.serializedName;

            var difficultyData = SongData._difficulties.FirstOrDefault(x =>
                x._difficulty == difficulty && x._beatmapCharacteristicName == characteristicName);
            if (difficultyData == null)
                return;

            var requirementsMet = true;
            foreach (var requirement in difficultyData.additionalDifficultyData._requirements)
            {
                if (SongCore.Collections.capabilities.Contains(requirement))
                    continue;
                _logger.Warn($"Level requirements not met: {requirement}");
                requirementsMet = false;
            }

            if (requirementsMet)
                return;
            
            _logger.Warn($"Level will be unloaded due to unmet requirements");
            _beatmapLevelData = null!;
        }

        public void Report(double value)
            => progressUpdated?.Invoke(value); 

        /// <summary>
        /// Downloads a custom level, and then loads and returns its data.
        /// </summary>
        private async Task<LoadBeatmapLevelDataResult> DownloadBeatmapLevelAsync(string levelId, CancellationToken cancellationToken)
        {
            // Download from BeatSaver
            var success = await _levelDownloader.TryDownloadLevel(levelId, cancellationToken, this);
            if (!success)
            {
                // If the download fails we go into spectator
                _rpcManager.SetIsEntitledToLevel(levelId, EntitlementsStatus.NotOwned);
                _beatmapLevelData = null;
                return LoadBeatmapLevelDataResult.Error;
            }

            // Reload custom level set
            _logger.Debug("Reloading custom level collection...");
            while (!SongCore.Loader.AreSongsLoaded)
            {
                await Task.Delay(25);
			}
            
			// Load level data
			var method = AccessTools.Method(_beatmapLevelsModel.GetType(), nameof(_beatmapLevelsModel.LoadBeatmapLevelDataAsync));
			LoadBeatmapLevelDataResult loadResult = LoadBeatmapLevelDataResult.Error;
			if (method != null)
			{
				if (method.GetParameters().Length > 2)
					loadResult = await (Task<LoadBeatmapLevelDataResult>)method.Invoke(_beatmapLevelsModel,
						new object[] { levelId, 0, cancellationToken });
                else if (method.GetParameters().Length == 2)
	                loadResult = await (Task<LoadBeatmapLevelDataResult>)method.Invoke(_beatmapLevelsModel,
		                new object[] { levelId, cancellationToken });
                else throw new NotSupportedException("Game version not supported");
			}
            //var loadResult = await _beatmapLevelsModel.LoadBeatmapLevelDataAsync(levelId, BeatmapLevelDataVersion.Original, cancellationToken);
            if (loadResult.isError) 
				_logger.Error($"Custom level data could not be loaded after download: {levelId}");
			return loadResult;
		}
    }
}
