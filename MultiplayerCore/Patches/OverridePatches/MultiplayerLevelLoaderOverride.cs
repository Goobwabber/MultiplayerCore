using HarmonyLib;
using MultiplayerCore.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IPA.Logging.Logger;
using static MultiplayerLevelLoader;

namespace MultiplayerCore.Patches.OverridePatches
{
	[HarmonyPatch(typeof(MultiplayerLevelLoader))]
	internal class MultiplayerLevelLoaderOverride
	{

		[HarmonyPostfix]
		[HarmonyPatch(nameof(MultiplayerLevelLoader.LoadLevel))]
		private static void LoadLevel_override(MultiplayerLevelLoader __instance, ILevelGameplaySetupData gameplaySetupData, long initialStartTime)
		{
			Plugin.Logger.Debug("Called MultiplayerLevelLoader.LoadLevel Override Patch");
			((MpLevelLoader)__instance).LoadLevel_override(gameplaySetupData.beatmapKey.levelId);
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(MultiplayerLevelLoader.Tick))]
		private static bool Tick_override_Pre(MultiplayerLevelLoader __instance, ref MultiplayerBeatmapLoaderState __state)
		{
			MpLevelLoader instance = (MpLevelLoader)__instance;
			__state = instance._loaderState;
			if (instance._loaderState == MultiplayerBeatmapLoaderState.NotLoading)
			{
				// Loader: not doing anything
				return false;
			}

			var levelId = instance._gameplaySetupData.beatmapKey.levelId;

			if (instance._loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown)
			{
				// Loader: level is loaded locally, waiting for countdown to transition to level
				// Modded behavior: wait until all players are ready before we transition

				if (instance._sessionManager.syncTime < instance._startTime)
					return false;

				// Ready check: player returned OK entitlement (load finished) OR already transitioned to gameplay
				var allPlayersReady = instance._sessionManager.connectedPlayers.All(p =>
					instance._entitlementChecker.GetKnownEntitlement(p.userId, levelId) == EntitlementsStatus.Ok // level loaded
					|| p.HasState("in_gameplay") // already playing
					|| p.HasState("backgrounded") // not actively in game
					|| !p.HasState("wants_to_play_next_level") // doesn't want to play (spectator)
				);

				if (!allPlayersReady)
					return false;

				instance._logger.Debug($"All players finished loading");
			}

			// Loader main: pending load
			return true;
		}


		[HarmonyPostfix]
		[HarmonyPatch(nameof(MultiplayerLevelLoader.Tick))]
		private static void Tick_override_Post(MultiplayerLevelLoader __instance, MultiplayerBeatmapLoaderState __state)
		{
			MpLevelLoader instance = (MpLevelLoader)__instance;

			bool loadJustFinished = __state == MultiplayerBeatmapLoaderState.LoadingBeatmap && instance._loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown;
			if (!loadJustFinished) return;

			// Loader main: pending load
			var levelId = instance._gameplaySetupData.beatmapKey.levelId;
			instance._rpcManager.SetIsEntitledToLevel(levelId, EntitlementsStatus.Ok);
			instance._logger.Debug($"Loaded level: {levelId}");

			instance.UnloadLevelIfRequirementsNotMet();
		}
	}
}
