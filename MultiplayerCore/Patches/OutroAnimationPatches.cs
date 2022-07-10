using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MultiplayerCore.Patches
{
    public class OutroAnimationPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerOutroAnimationController), nameof(MultiplayerOutroAnimationController.BindRingsAndAudio))]
        private static void BindRingsAndAudio(ref GameObject[] rings)
        {
            rings = rings.Take(5).ToArray();
        }

        private static readonly MethodInfo _getActivePlayersMethod = AccessTools.PropertyGetter(typeof(MultiplayerPlayersManager), nameof(MultiplayerPlayersManager.allActiveAtGameStartPlayers));

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MultiplayerOutroAnimationController), nameof(MultiplayerOutroAnimationController.BindOutroTimeline))]
        private static IEnumerable<CodeInstruction> PlayIntroPlayerCount(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(_getActivePlayersMethod))
                {
                    codes[i] = new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => GetActivePlayersAttacher(null!)));
                }
            }
            return codes.AsEnumerable();
        }

        private static IReadOnlyList<IConnectedPlayer> GetActivePlayersAttacher(MultiplayerPlayersManager contract)
        {
            return contract.allActiveAtGameStartPlayers.Take(4).ToList();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerResultsPyramidView), nameof(MultiplayerResultsPyramidView.SetupResults))]
        private static void SetupResultsPyramid(ref IReadOnlyList<MultiplayerPlayerResultsData> resultsData)
        {
            resultsData = resultsData.Take(5).ToList();
        }
    }
}
