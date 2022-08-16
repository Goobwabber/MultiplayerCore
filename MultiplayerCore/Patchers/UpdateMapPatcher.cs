using BeatSaverSharp;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using IPA.Utilities.Async;
using MultiplayerCore.Beatmaps.Abstractions;
using Polyglot;
using SiraUtil.Affinity;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplayerCore.Patchers
{
    [HarmonyPatch]
    internal class UpdateMapPatcher : IAffinity
    {
        public const string PlayersMissingLevelTextKey = "LABEL_PLAYERS_MISSING_ENTITLEMENT";

        private CancellationTokenSource? _beatmapCts;

        private LobbySetupViewController _lobbySetupViewController;
        private readonly ILobbyPlayersDataModel _playersDataModel;
        private readonly BeatSaver _beatsaver;
        
        public UpdateMapPatcher(
            LobbySetupViewController lobbySetupViewController,
            ILobbyPlayersDataModel playersDataModel,
            BeatSaver beatsaver)
        {
            _lobbySetupViewController = lobbySetupViewController;
            _playersDataModel = playersDataModel;
            _beatsaver = beatsaver;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(GameServerLobbyFlowCoordinator), nameof(GameServerLobbyFlowCoordinator.HandleMenuRpcManagerSetPlayersMissingEntitlementsToLevel))]
        private bool SetPlayersMissingEntitlementsToLevel(PlayersMissingEntitlementsNetSerializable playersMissingEntitlements)
        {
            var levelId = _playersDataModel[_playersDataModel.localUserId].beatmapLevel?.beatmapLevel.levelID;
            var levelHash = Utilities.HashForLevelID(levelId);
            if (levelId is null || levelHash is null)
                return true;
            _beatmapCts?.Cancel();
            _beatmapCts = new CancellationTokenSource();
            _ = Task.Run(() => FetchAndShowError(levelHash, playersMissingEntitlements, _beatmapCts.Token), _beatmapCts.Token);
            return false;
        }

        private async Task FetchAndShowError(string levelHash, PlayersMissingEntitlementsNetSerializable playersMissingEntitlements, CancellationToken cancellationToken)
        {
            var beatmap = await _beatsaver.BeatmapByHash(levelHash, cancellationToken);

            string errorText = PlayersMissingLevelTextKey;
            if (beatmap?.LatestVersion.Hash != levelHash)
                errorText = "Click here to update this song. These players cannot download the older version";
            if (_playersDataModel[_playersDataModel.localUserId].beatmapLevel?.beatmapLevel is MpBeatmapLevel beatmapLevel && beatmapLevel.requirements.Any())
                errorText = "This map has mod requirements that these players may not have";

            await UnityMainThreadTaskScheduler.Factory.StartNew(() => SetPlayersMissingLevelText(playersMissingEntitlements, errorText));
        }

        private static readonly FieldInfo _errorField = AccessTools.Field(typeof(UpdateMapPatcher), nameof(_errorText));

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(GameServerLobbyFlowCoordinator), nameof(GameServerLobbyFlowCoordinator.HandleMenuRpcManagerSetPlayersMissingEntitlementsToLevel))]
        private static void SetPlayersMissingLevelText(PlayersMissingEntitlementsNetSerializable playersMissingEntitlements, string errorText)
        {
            _errorText = errorText;
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
                new CodeMatcher(instructions)
                    .Start()
                    .RemoveInstructions(2)
                    .Set(OpCodes.Ldc_I4_1, null)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldstr && i.OperandIs(PlayersMissingLevelTextKey)))
                    .Set(OpCodes.Ldsfld, _errorField)
                    .InstructionEnumeration();
            _ = Transpiler(null!);
        }

        private static string? _errorText = null;
    }
}