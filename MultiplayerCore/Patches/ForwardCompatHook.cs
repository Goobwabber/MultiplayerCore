using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using IPA.Utilities;
using LiteNetLib.Utils;

namespace MultiplayerCore.Patches
{
	/// <summary>
	/// Temporary patch to allow 1.37.0 and 1.37.1 compatibility
	/// </summary>
	[HarmonyPatch]
	internal class ForwardCompatHook
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LevelCompletionResults), nameof(LevelCompletionResults.Serialize))]
		private static void LevelCompletionResultsSerialize(ref NetDataWriter writer)
		{
			Plugin.Logger.Debug("LevelCompletionResultsSerialize called");
			try
			{
				if (UnityGame.GameVersion < new AlmostVersion("1.37.1"))
				{
					Plugin.Logger.Debug("LevelCompletionResultsSerialize GameVersion is 1.37.0 or lower");
					writer.Put(false);
				}
				else Plugin.Logger.Debug("LevelCompletionResultsSerialize GameVersion is 1.37.1 or higher, nothing todo");
			}
			catch (Exception ex)
			{
				Plugin.Logger.Warn($"LevelCompletionResultsSerialize Put failed: {ex.Message}");
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(LevelCompletionResults), nameof(INetImmutableSerializable<LevelCompletionResults>.CreateFromSerializedData))]
		private static void LevelCompletionResultsCreateFromSerializedData(ref NetDataReader reader)
		{
			try
			{
				if (UnityGame.GameVersion < new AlmostVersion("1.37.1"))
				{
					Plugin.Logger.Debug("LevelCompletionResultsCreateFromSerializedData GameVersion is 1.37.0 or lower");
					reader.GetBool();
				}
				else Plugin.Logger.Debug("LevelCompletionResultsCreateFromSerializedData GameVersion is 1.37.1 or higher, nothing todo");
			}
			catch (Exception ex)
			{
				Plugin.Logger.Warn($"LevelCompletionResultsCreateFromSerializedData GetBool failed, sending player on outdated version?: {ex.Message}");
			}
		}
	}
}
