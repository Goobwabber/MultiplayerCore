using HarmonyLib;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MultiplayerCore.Patchers
{
    public class OutroAnimationPatcher : IAffinity
    {
        private readonly SiraLog _logger;

        internal OutroAnimationPatcher(
            SiraLog logger)
        {
            _logger = logger;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerOutroAnimationController), nameof(MultiplayerOutroAnimationController.BindRingsAndAudio))]
        private void BindRingsAndAudio(ref GameObject[] rings)
        {
            rings = rings.Take(5).ToArray();
        }

        private readonly MethodInfo _getActivePlayersMethod = AccessTools.PropertyGetter(typeof(MultiplayerPlayersManager), nameof(MultiplayerPlayersManager.allActiveAtGameStartPlayers));

        [AffinityTranspiler]
        [AffinityPatch(typeof(MultiplayerOutroAnimationController), nameof(MultiplayerOutroAnimationController.BindOutroTimeline))]
        private IEnumerable<CodeInstruction> PlayIntroPlayerCount(IEnumerable<CodeInstruction> instructions)
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

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerResultsPyramidView), nameof(MultiplayerResultsPyramidView.SetupResults))]
        private void SetupResultsPyramid(ref IReadOnlyList<MultiplayerPlayerResultsData> resultsData)
        {
            resultsData = resultsData.Take(5).ToList();
        }
    }
}
