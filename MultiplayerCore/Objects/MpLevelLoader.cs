using HarmonyLib;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplayerCore.Objects
{
    public class MpLevelLoader : MultiplayerLevelLoader, IAffinity, IProgress<double>
    {
        public event Action<double> progressUpdated = null!;

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

        public override void LoadLevel(BeatmapIdentifierNetSerializable beatmapId, GameplayModifiers gameplayModifiers, float initialStartTime)
        {
            string levelHash = SongCore.Collections.hashForLevelID(beatmapId.levelID);
            _logger.Debug($"Loading level {beatmapId.levelID}");
            base.LoadLevel(beatmapId, gameplayModifiers, initialStartTime);
            if (levelHash != null && !SongCore.Collections.songWithHashPresent(levelHash))
                _getBeatmapLevelResultTask = DownloadBeatmapLevelAsync(beatmapId.levelID, _getBeatmapCancellationTokenSource.Token);

            // Possible race condition here
        }

        private readonly FieldInfo _previewBeatmapLevelField = AccessTools.Field(typeof(MultiplayerLevelLoader), "_previewBeatmapLevel");
        private readonly FieldInfo _beatmapCharacteristicField = AccessTools.Field(typeof(MultiplayerLevelLoader), "_beatmapCharacteristic");

        [AffinityTranspiler]
        [AffinityPatch(typeof(MultiplayerLevelLoader), nameof(MultiplayerLevelLoader.LoadLevel))]
        private IEnumerable<CodeInstruction> LoadLevelPatch(IEnumerable<CodeInstruction> instructions) =>
            new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Stfld, _previewBeatmapLevelField))
                .Advance(-6)
                .RemoveInstructions(7)
                .MatchForward(false, new CodeMatch(OpCodes.Stfld, _beatmapCharacteristicField))
                .Advance(-9)
                .RemoveInstructions(10)
                .InstructionEnumeration()
                .ToList();

        public override void Tick()
        {
            if (_loaderState == MultiplayerBeatmapLoaderState.LoadingBeatmap)
            {
                base.Tick();
                if (_loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown)
                {
                    _previewBeatmapLevel = _beatmapLevelsModel.GetLevelPreviewForLevelId(_beatmapId.levelID);
                    _beatmapCharacteristic = _previewBeatmapLevel.previewDifficultyBeatmapSets.First((PreviewDifficultyBeatmapSet set) => set.beatmapCharacteristic.serializedName == _beatmapId.beatmapCharacteristicSerializedName).beatmapCharacteristic;
                    _rpcManager.SetIsEntitledToLevel(_beatmapId.levelID, EntitlementsStatus.Ok);
                    _logger.Debug($"Loaded level {_beatmapId.levelID}");
                }
            }
            else if (_loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown)
            {
                if (_sessionManager.connectedPlayers.All(p => _entitlementChecker.GetUserEntitlementStatusWithoutRequest(p.userId, _beatmapId.levelID) == EntitlementsStatus.Ok))
                {
                    _logger.Debug($"All players finished loading");
                    base.Tick();
                }
            }
            else
                base.Tick();
        }

        public void Report(double value)
            => progressUpdated?.Invoke(value); 

        /// <summary>
        /// Downloads a level and then loads it.
        /// </summary>
        /// <param name="levelId">Level to download</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Level load results</returns>
        public async Task<BeatmapLevelsModel.GetBeatmapLevelResult> DownloadBeatmapLevelAsync(string levelId, CancellationToken cancellationToken)
        {
            _ = await _levelDownloader.TryDownloadLevel(levelId, cancellationToken, this); // Handle?
            return await _beatmapLevelsModel.GetBeatmapLevelAsync(levelId, cancellationToken);
        }
    }
}
