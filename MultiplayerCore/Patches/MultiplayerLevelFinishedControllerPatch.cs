using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerCore.Patches
{
	[HarmonyPatch]
	internal class MultiplayerLevelFinishedControllerPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MultiplayerLevelFinishedController), nameof(MultiplayerLevelFinishedController.HandleRpcLevelFinished))]
		static bool HandleRpcLevelFinished(MultiplayerLevelFinishedController __instance, string userId, MultiplayerLevelCompletionResults results)
		{
			// Possibly get notesCount from BeatSaver or by parsing the beatmapdata ourselves
			// Skip score validation if notesCount is 0, since custom songs always have notesCount 0 in BeatmapBasicData
			if (__instance._beatmapBasicData.notesCount <= 0 && results.hasAnyResults)
			{
				Plugin.Logger.Info($"BeatmapData noteCount is 0, skipping validation");
				__instance._otherPlayersCompletionResults[userId] = results;
				return false;
			}
			return true;
		}
	}

}
