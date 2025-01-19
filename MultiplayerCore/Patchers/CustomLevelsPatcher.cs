using System;
using System.Threading.Tasks;
using HarmonyLib;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine.UI;

namespace MultiplayerCore.Patchers
{
    [HarmonyPatch]
    internal class CustomLevelsPatcher : IAffinity
    {
        private readonly NetworkConfigPatcher _networkConfig;
        private readonly SiraLog _logger;
        private bool? originalIncludeAllDifficulties = null;
        private string? _lastStatusUrl;

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
                _networkConfig.IsOverridingApi 
                && ____songPackMask.Contains(new SongPackMask("custom_levelpack_CustomLevels"));
			_logger.Trace($"Custom levels enabled check songpackmask: '{____songPackMask.ToShortString()}' enables custom levels '{__result}'");
			return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LobbySetupViewController), nameof(LobbySetupViewController.SetPlayersMissingLevelText))]
        private static void SetPlayersMissingLevelText(LobbySetupViewController __instance, string playersMissingLevelText, ref Button ____startGameReadyButton)
        {
            if (!string.IsNullOrEmpty(playersMissingLevelText) && ____startGameReadyButton.interactable && __instance._isPartyOwner)
                __instance.SetStartGameEnabled(CannotStartGameReason.DoNotOwnSong);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameServerLobbyFlowCoordinator),
	        nameof(GameServerLobbyFlowCoordinator.HandleMenuRpcManagerSetPlayersMissingEntitlementsToLevel))]
        private static bool HandleMenuRpcManagerSetPlayersMissingEntitlementsToLevel(
	        GameServerLobbyFlowCoordinator __instance,
	        PlayersMissingEntitlementsNetSerializable playersMissingEntitlements)
        {
	        //if (__instance.isQuickStartServer)
	        //{
		        __instance._playerIdsWithoutEntitlements.Clear();
		        __instance._playerIdsWithoutEntitlements.AddRange(playersMissingEntitlements.playersWithoutEntitlements);
		        __instance.SetPlayersMissingLevelText();
		        return false;
	        //}
            //return true;
		}

		[AffinityPrefix]
        [AffinityPatch(typeof(JoinQuickPlayViewController), nameof(JoinQuickPlayViewController.Setup))]
        private void SetupPre(JoinQuickPlayViewController __instance, ref BeatmapDifficultyDropdown ____beatmapDifficultyDropdown, QuickPlaySongPacksDropdown ____songPacksDropdown, QuickPlaySetupData quickPlaySetupData)
        {
            _logger.Trace("JoinQuickPlayViewController.Setup called");
            _logger.Trace($"Check QPSD override: {quickPlaySetupData.hasOverride}");
			// Ensure quickplay selection options are updated
			if (!originalIncludeAllDifficulties.HasValue) originalIncludeAllDifficulties =
		____beatmapDifficultyDropdown.includeAllDifficulties;
			if (_networkConfig.IsOverridingApi) ____beatmapDifficultyDropdown.includeAllDifficulties = true;
			else ____beatmapDifficultyDropdown.includeAllDifficulties = originalIncludeAllDifficulties.Value;
			if (_lastStatusUrl != _networkConfig.MasterServerStatusUrl || _lastStatusUrl == null)
            {
                // Refresh difficulty dropdown
                ____beatmapDifficultyDropdown._beatmapDifficultyData = null;
                ____beatmapDifficultyDropdown.OnDestroy();
                ____beatmapDifficultyDropdown.Start();

                // Refresh song packs dropdown
                ____songPacksDropdown.SetOverrideSongPacks(quickPlaySetupData.quickPlayAvailablePacksOverride); // Ensure it's always set even when null
                ____songPacksDropdown._initialized = false;

                _lastStatusUrl = _networkConfig.MasterServerStatusUrl;
            }
		}

		[HarmonyPostfix]
        [HarmonyPatch(typeof(BeatmapDifficultyDropdown), nameof(BeatmapDifficultyDropdown.GetIdxForBeatmapDifficultyMask))]
        private static void GetIdxForBeatmapDifficultyMask(BeatmapDifficultyDropdown __instance, ref int __result)
        {
            if (__instance.includeAllDifficulties) __result = 0;
        }

		[AffinityPostfix]
		[AffinityPatch(typeof(QuickPlaySetupModel), nameof(QuickPlaySetupModel.IsQuickPlaySetupTaskValid))]
		private void IsQuickPlaySetupTaskValid(QuickPlaySetupModel __instance, ref bool __result, Task<QuickPlaySetupData> ____request, DateTime ____lastRequestTime)
		{
			if (_networkConfig.IsOverridingApi) __result = false;
		}
	}
}
