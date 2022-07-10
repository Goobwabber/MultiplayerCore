using HarmonyLib;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MultiplayerCore.Patches
{
    public class IntroAnimationPatches
    {
        private static PlayableDirector? _originalDirector;
        private static int _iteration = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerIntroAnimationController), nameof(MultiplayerIntroAnimationController.PlayIntroAnimation))]
        private static void BeginPlayIntroAnimation(ref Action onCompleted, Action ____onCompleted, ref bool ____bindingFinished, ref PlayableDirector ____introPlayableDirector)
        {
            Plugin.Logger.Debug($"Creating intro PlayableDirector for iteration '{_iteration}'.");

            _originalDirector = ____introPlayableDirector;

            // Create new gameobject to play the animation after first
            if (_iteration != 0)
            {
                GameObject newPlayableGameObject = new GameObject();
                ____introPlayableDirector = newPlayableGameObject.AddComponent<PlayableDirector>();
                ____introPlayableDirector.playableAsset = _originalDirector.playableAsset;

                // Cleanup gameobject
                onCompleted = () => {
                    GameObject.Destroy(newPlayableGameObject);

                    // Make sure old action happens by calling it
                    ____onCompleted.Invoke();
                };
            }

            // Mute audio if animation is not first animation, so audio only plays once
            foreach (TrackAsset track in ((TimelineAsset)____introPlayableDirector.playableAsset).GetOutputTracks())
            {
                track.muted = track is AudioTrack && _iteration != 0;
            }

            // Makes animator rebind to new playable
            ____bindingFinished = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MultiplayerIntroAnimationController), nameof(MultiplayerIntroAnimationController.PlayIntroAnimation))]
        private static void EndPlayIntroAnimation(ref MultiplayerIntroAnimationController __instance, float maxDesiredIntroAnimationDuration, Action onCompleted, ref PlayableDirector ____introPlayableDirector, MultiplayerPlayersManager ____multiplayerPlayersManager)
        {
            _iteration++;
            ____introPlayableDirector = _originalDirector!;
            IEnumerable<IConnectedPlayer> players = ____multiplayerPlayersManager.allActiveAtGameStartPlayers.Where(p => !p.isMe);
            if (_iteration < ((players.Count() + 3) / 4))
                __instance.PlayIntroAnimation(maxDesiredIntroAnimationDuration, onCompleted);
            else
                _iteration = 0; // Reset
        }

        private static readonly MethodInfo _getActivePlayersMethod = AccessTools.PropertyGetter(typeof(MultiplayerPlayersManager), nameof(MultiplayerPlayersManager.allActiveAtGameStartPlayers));

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MultiplayerIntroAnimationController), nameof(MultiplayerIntroAnimationController.BindTimeline))]
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
            IEnumerable<IConnectedPlayer> players = contract.allActiveAtGameStartPlayers.Where(p => !p.isMe);
            players = players.Skip(_iteration * 4).Take(4);
            if (_iteration == 0 && contract.allActiveAtGameStartPlayers.Any(p => p.isMe))
                players.Append(contract.allActiveAtGameStartPlayers.First(p => p.isMe));
            return players.ToList();
        }
    }
}
