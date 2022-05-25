using MultiplayerCore.Beatmaps;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

namespace MultiplayerCore.Patchers
{
    public class CustomLevelsPatcher : IAffinity
    {
        private readonly NetworkConfigPatcher _networkConfig;
        private readonly SiraLog _logger;
        internal CustomLevelsPatcher(
            NetworkConfigPatcher networkConfig,
            SiraLog logger)
        {
            _networkConfig = networkConfig;
            _logger = logger;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerLevelSelectionFlowCoordinator), "enableCustomLevels", AffinityMethodType.Getter)]
        private bool CustomLevelsEnabled(ref bool __result, SongPackMask ____songPackMask)
        {
            __result = 
                _networkConfig.MasterServerEndPoint != null 
                && ____songPackMask.Contains(new SongPackMask("custom_levelpack_CustomLevels"));
            return false;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(LobbySetupViewController), nameof(LobbySetupViewController.SetPlayersMissingLevelText))]
        private void SetPlayersMissingLevelText(LobbySetupViewController __instance, string playersMissingLevelText, ref Button ____startGameReadyButton)
        {
            if (!string.IsNullOrEmpty(playersMissingLevelText) && ____startGameReadyButton.interactable)
                __instance.SetStartGameEnabled(CannotStartGameReason.DoNotOwnSong);
        }

        [AffinityPatch(typeof(BeatmapIdentifierNetSerializableHelper), nameof(BeatmapIdentifierNetSerializableHelper.ToPreviewDifficultyBeatmap))]
        private void BeatmapIdentifierToPreviewDifficultyBeatmap(BeatmapIdentifierNetSerializable beatmapId, ref PreviewDifficultyBeatmap __result)
        {
            if (__result.beatmapLevel == null)
                __result.beatmapLevel = new NoInfoBeatmapLevel(Utilities.HashForLevelID(beatmapId.levelID));
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(JoinQuickPlayViewController), nameof(JoinQuickPlayViewController.Setup))]
        private void SetupPre(JoinQuickPlayViewController __instance, ref BeatmapDifficultyDropdown ____beatmapDifficultyDropdown)
        {
            if (_networkConfig.MasterServerEndPoint != null) ____beatmapDifficultyDropdown.includeAllDifficulties = true;
        }

        [AffinityPostfix]
        [AffinityPatch(typeof(BeatmapDifficultyDropdown), nameof(BeatmapDifficultyDropdown.GetIdxForBeatmapDifficultyMask))]
        private void GetIdxForBeatmapDifficultyMask(BeatmapDifficultyDropdown __instance, ref int __result)
        {
            if (__instance.includeAllDifficulties) __result = 0;
            _logger.Debug($"GetIdxForBeatmapDifficultyMask {__result}"); // TODO: Remove this line for release
        }

        [AffinityPostfix]
        [AffinityPatch(typeof(QuickPlaySetupModel), nameof(QuickPlaySetupModel.IsQuickPlaySetupTaskValid))]
        private void IsQuickPlaySetupTaskValid(QuickPlaySetupModel __instance, ref bool __result, Task<QuickPlaySetupData> ____request, DateTime ____lastRequestTime)
        {
            if (_networkConfig.MasterServerEndPoint != null) __result = false;
            _logger.Debug($"IsQuickPlaySetupTaskValid {__result}"); // TODO: Remove this line for release
        }

        //[AffinityPostfix]
        //[AffinityPatch(typeof(MultiplayerModeSelectionFlowCoordinator), nameof(MultiplayerModeSelectionFlowCoordinator.HandleJoinQuickPlayViewControllerDidFinish))]
        //private void HandleJoinQuickPlayViewControllerDidFinish(MultiplayerModeSelectionFlowCoordinator __instance, ref bool __result, Task<QuickPlaySetupData> ____request, DateTime ____lastRequestTime)
        //{
        //    // TODO: Possibly add warning screen.
        //}

    }
}
