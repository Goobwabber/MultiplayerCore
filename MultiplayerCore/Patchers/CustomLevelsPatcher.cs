using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine.UI;

namespace MultiplayerCore.Patchers
{
    public class CustomLevelsPatcher : IAffinity
    {
        private readonly NetworkConfigPatcher _networkConfigPatcher;
        private readonly INetworkConfig _networkConfig;
        private readonly SiraLog _logger;

        internal CustomLevelsPatcher(
            NetworkConfigPatcher networkConfigPatcher,
            INetworkConfig networkConfig,
            SiraLog logger)
        {
            _networkConfigPatcher = networkConfigPatcher;
            _networkConfig = networkConfig;
            _logger = logger;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerLevelSelectionFlowCoordinator), "enableCustomLevels", AffinityMethodType.Getter)]
        private bool CustomLevelsEnabled(ref bool __result, SongPackMask ____songPackMask)
        {
            __result = 
                (_networkConfigPatcher.MasterServerEndPoint != null || (_networkConfig is CustomNetworkConfig))
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
    }
}
