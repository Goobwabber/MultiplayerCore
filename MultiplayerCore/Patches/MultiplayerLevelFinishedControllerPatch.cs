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
			float maxMultipliedScore = ScoreModel.ComputeQuickInaccurateMaxMultipliedScoreForBeatmap(__instance._beatmapBasicData) * 1.21f;
			if (!results.hasAnyResults)
				Plugin.Logger.Info($"Score Received from user with id '{userId}' contains no results");
			// Skip score validation if notesCount is 0, since custom songs always have notesCount 0 in BeatmapBasicData
			// TODO: Change this to only be a single if (__instance._beatmapBasicData.notesCount <= 0 && results.hasAnyResults) if check
			else if (__instance._beatmapBasicData.notesCount <= 0)
			{
				Plugin.Logger.Info($"BeatmapData noteCount is 0, skipping validation");
				__instance._otherPlayersCompletionResults[userId] = results;
				return false;
			}
			else if (results.levelCompletionResults.modifiedScore <= maxMultipliedScore && results.levelCompletionResults.modifiedScore >= 0)
				Plugin.Logger.Info($"Score Received from user with id '{userId}' contains results and is valid, modifiedScore: '{results.levelCompletionResults.modifiedScore}'");
			else
				Plugin.Logger.Info($"Score Received from user with id '{userId}' failed validation, maxMultipliedScore: '{maxMultipliedScore}', modifiedScore: '{results.levelCompletionResults.modifiedScore}'");
			return true;
		}
	}

}
