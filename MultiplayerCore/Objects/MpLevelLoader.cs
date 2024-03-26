using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SiraUtil.Logging;

namespace MultiplayerCore.Objects
{
    public class MpLevelLoader : MultiplayerLevelLoader, IProgress<double>
    {
        public event Action<double> progressUpdated = null!;

        public ILevelGameplaySetupData? CurrentLoadingData => _gameplaySetupData;

        private readonly IMultiplayerSessionManager _sessionManager;
        private readonly MpLevelDownloader _levelDownloader;
        private readonly MpEntitlementChecker _entitlementChecker;
        private readonly IMenuRpcManager _rpcManager;
        private readonly SiraLog _logger;

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
        public new void LoadLevel(ILevelGameplaySetupData gameplaySetupData, long initialStartTime)
        {
            var levelId = gameplaySetupData.beatmapKey.levelId;
            var levelHash = Utilities.HashForLevelID(levelId);
            
            base.LoadLevel(gameplaySetupData, initialStartTime);

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

        [UsedImplicitly]
        public new void Tick()
        {
            if (_loaderState == MultiplayerBeatmapLoaderState.NotLoading)
            {
                // Loader: not doing anything
                return;
            }

            var levelId = _gameplaySetupData.beatmapKey.levelId;

            if (_loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown)
            {
                // Loader: level is loaded locally, waiting for countdown to transition to level
                // Modded behavior: wait until all players are ready before we transition

                if (_startTime <= _sessionManager.syncTime)
                    return;

                // Ready check: player returned OK entitlement (load finished) OR already transitioned to gameplay
                var allPlayersReady = _sessionManager.connectedPlayers.All(p =>
                    _entitlementChecker.GetKnownEntitlement(p.userId, levelId) == EntitlementsStatus.Ok ||
                    p.HasState("in_gameplay"));

                if (!allPlayersReady)
                    return;
                
                _logger.Debug($"All players finished loading");
                base.Tick(); // calling Tick() now will cause base level loader to transition to gameplay
                return;
            }
            
            // Loader main: pending load
            base.Tick();

            var loadDidFinish = (_loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown);
            if (!loadDidFinish)
                return;
            
            _rpcManager.SetIsEntitledToLevel(levelId, EntitlementsStatus.Ok);
            _logger.Debug($"Loaded level: {levelId}");
            
            UnloadLevelIfRequirementsNotMet();
        }

        private void UnloadLevelIfRequirementsNotMet()
        {
            // Extra: load finished, check if there are extra requirements in place
            // If we fail requirements, unload the level
            
            var beatmapKey = _gameplaySetupData.beatmapKey;
            var levelId = beatmapKey.levelId;
            
            var levelHash = Utilities.HashForLevelID(levelId);
            if (levelHash == null)
                return;
            
            var extraSongData = SongCore.Collections.RetrieveExtraSongData(levelHash);
            if (extraSongData == null)
                return;

            var difficulty = _gameplaySetupData.beatmapKey.difficulty;
            var characteristicName = _gameplaySetupData.beatmapKey.beatmapCharacteristic.serializedName;

            var difficultyData = extraSongData._difficulties.FirstOrDefault(x =>
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
                throw new Exception($"Failed to download level: {levelId}");

            // Reload custom level set
            _logger.Debug("Reloading custom level collection...");
            await _beatmapLevelsModel.ReloadCustomLevelPackCollectionAsync(cancellationToken);
            
            // Load level data
            var loadResult = await _beatmapLevelsModel.LoadBeatmapLevelDataAsync(levelId, cancellationToken);
            if (loadResult.isError)
                _logger.Error($"Custom level data could not be loaded after download: {levelId}");
            return loadResult;
        }
    }
}
