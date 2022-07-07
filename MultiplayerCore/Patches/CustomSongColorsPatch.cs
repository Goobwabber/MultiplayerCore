using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch(typeof(MultiplayerLevelScenesTransitionSetupDataSO), "Init", MethodType.Normal)]
    internal class CustomSongColorsPatch
    {
        private static void Prefix(ref IDifficultyBeatmap difficultyBeatmap, ref ColorScheme? overrideColorScheme)
        {
            object sConfiguration = typeof(SongCore.Plugin).GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var customSongColors = (bool)typeof(SongCore.Plugin).Assembly.GetType("SongCore.SConfiguration").GetProperty("CustomSongColors").GetValue(sConfiguration);
            if (!customSongColors)
                return;
            var songData = SongCore.Collections.RetrieveDifficultyData(difficultyBeatmap);
            if (songData == null)
                return;
            if (songData._colorLeft == null && songData._colorRight == null && songData._envColorLeft == null && songData._envColorRight == null && songData._obstacleColor == null && songData._envColorLeftBoost == null && songData._envColorRightBoost == null)
                return;

            var environmentInfoSO = difficultyBeatmap.GetEnvironmentInfo();
            var fallbackScheme = overrideColorScheme ?? new ColorScheme(environmentInfoSO.colorScheme);

            var saberLeft = songData._colorLeft == null ? fallbackScheme.saberAColor : ColorFromMapColor(songData._colorLeft);
            var saberRight = songData._colorRight == null ? fallbackScheme.saberBColor : ColorFromMapColor(songData._colorRight);
            var envLeft = songData._envColorLeft == null
                ? songData._colorLeft == null ? fallbackScheme.environmentColor0 : ColorFromMapColor(songData._colorLeft)
                : ColorFromMapColor(songData._envColorLeft);
            var envRight = songData._envColorRight == null
                ? songData._colorRight == null ? fallbackScheme.environmentColor1 : ColorFromMapColor(songData._colorRight)
                : ColorFromMapColor(songData._envColorRight);
            var envLeftBoost = songData._envColorLeftBoost == null ? envLeft : ColorFromMapColor(songData._envColorLeftBoost);
            var envRightBoost = songData._envColorRightBoost == null ? envRight : ColorFromMapColor(songData._envColorRightBoost);
            var obstacle = songData._obstacleColor == null ? fallbackScheme.obstaclesColor : ColorFromMapColor(songData._obstacleColor);
            overrideColorScheme = new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, saberLeft, saberRight, envLeft,
                envRight, true, envLeftBoost, envRightBoost, obstacle);
        }

        private static Color ColorFromMapColor(SongCore.Data.ExtraSongData.MapColor mapColor) =>
            new Color(mapColor.r, mapColor.g, mapColor.b);
    }
}
