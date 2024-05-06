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
		private static bool Tick_override_Pre(MultiplayerLevelLoader __instance/*, ValueTuple<bool, string> __state*/)
		{
			MpLevelLoader instance = (MpLevelLoader)__instance;
			if (instance._loaderState == MultiplayerBeatmapLoaderState.NotLoading)
			{
				// Loader: not doing anything
				return false;
			}

			var levelId = instance._gameplaySetupData.beatmapKey.levelId;
			//__state = (false, levelId);

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
				//base.Tick(); // calling Tick() now will cause base level loader to transition to gameplay
				//return true;
			}

			// Loader main: pending load
			//__state.Item1 = true;
			return true;
			//base.Tick();
			//var loadDidFinish = (_loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown);
			//if (!loadDidFinish)
			//	return false;

			//_rpcManager.SetIsEntitledToLevel(levelId, EntitlementsStatus.Ok);
			//_logger.Debug($"Loaded level: {levelId}");

			//UnloadLevelIfRequirementsNotMet();
		}


		[HarmonyPostfix]
		[HarmonyPatch(nameof(MultiplayerLevelLoader.Tick))]
		private static void Tick_override_Post(MultiplayerLevelLoader __instance/*, ValueTuple<bool, string> __state*/)
		{
			MpLevelLoader instance = (MpLevelLoader)__instance;

			//if (!__state.Item1)
			if (instance._loaderState == MultiplayerBeatmapLoaderState.NotLoading)
			{
				// Loader: not doing anything
				return;
			}

			// Loader main: pending load
			var levelId = instance._gameplaySetupData.beatmapKey.levelId;
			var loadDidFinish = (instance._loaderState == MultiplayerBeatmapLoaderState.WaitingForCountdown);
			if (!loadDidFinish)
				return;

			instance._rpcManager.SetIsEntitledToLevel(levelId, EntitlementsStatus.Ok);
			instance._logger.Debug($"Loaded level: {levelId}");

			instance.UnloadLevelIfRequirementsNotMet();

		}
	}
}
