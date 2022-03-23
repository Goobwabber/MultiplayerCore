using MultiplayerCore.Beatmaps;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine.UI;

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
    }
}
