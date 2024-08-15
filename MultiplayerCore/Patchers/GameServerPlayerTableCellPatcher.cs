using SiraUtil.Affinity;
using System.Collections;
using System.Threading.Tasks;
using MultiplayerCore.Objects;
using BGLib.Polyglot;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Providers;

namespace MultiplayerCore.Patchers
{
    internal class GameServerPlayerTableCellPatcher : IAffinity
    {
        private MpPlayersDataModel? _mpPlayersDataModel;
        private MpBeatmapLevelProvider _mpBeatmapLevelProvider;
        private ICoroutineStarter _sharedCoroutineStarter;

        GameServerPlayerTableCellPatcher(ILobbyPlayersDataModel playersDataModel, MpBeatmapLevelProvider mpBeatmapLevelProvider, ICoroutineStarter sharedCoroutineStarter)
        {
			_mpPlayersDataModel = playersDataModel as MpPlayersDataModel;
			_mpBeatmapLevelProvider = mpBeatmapLevelProvider;
			_sharedCoroutineStarter = sharedCoroutineStarter;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(GameServerPlayerTableCell), nameof(GameServerPlayerTableCell.SetData))]
        bool GameServerPlayerTableCell_SetData(ref GameServerPlayerTableCell __instance, IConnectedPlayer connectedPlayer, ILobbyPlayerData? playerData, bool hasKickPermissions, bool allowSelection, Task<EntitlementStatus>? getLevelEntitlementTask)
        {
            __instance._playerNameText.text = connectedPlayer.userName;
            __instance._localPlayerBackgroundImage.enabled = connectedPlayer.isMe;
            if (!playerData.isReady && playerData.isActive && !playerData.isPartyOwner)
            {
                __instance._statusImageView.enabled = false;
            } 
            else
            {
                __instance._statusImageView.enabled = true;

                var statusView = __instance._statusImageView;
                if (playerData.isPartyOwner) statusView.sprite = __instance._hostIcon;
                else if (playerData.isActive) statusView.sprite = __instance._readyIcon;
                else statusView.sprite = __instance._spectatingIcon;
            }

			var key = playerData.beatmapKey;
			//if (!key.IsValid()) return true;
			_sharedCoroutineStarter.StartCoroutine(SetDataCoroutine(__instance, connectedPlayer, playerData, key, hasKickPermissions,
				allowSelection, getLevelEntitlementTask));
			return false;
		}
		IEnumerator SetDataCoroutine(GameServerPlayerTableCell instance, IConnectedPlayer connectedPlayer, ILobbyPlayerData playerData, BeatmapKey key, bool hasKickPermissions, bool allowSelection, Task<EntitlementStatus>? getLevelEntitlementTask)
        {
			Plugin.Logger.Debug($"Start SetDataCoroutine with key {key.levelId} diff {key.difficulty.Name()}");
	        bool displayLevelText = key.IsValid();
			if (displayLevelText)
			{
				Plugin.Logger.Debug("displayLevelText if check start");
				var level = instance._beatmapLevelsModel.GetBeatmapLevel(key.levelId);
				var levelHash = Utilities.HashForLevelID(key.levelId);
				instance._suggestedLevelText.text = level?.songName;
				displayLevelText = level != null;

				if (level == null && _mpPlayersDataModel != null && !string.IsNullOrEmpty(levelHash)) // we didn't have the level, but we can attempt to get the packet
				{
					Plugin.Logger.Debug("FindLevelPacket running");
					var packet = _mpPlayersDataModel.FindLevelPacket(levelHash);
					instance._suggestedLevelText.text = packet?.songName;

					Task<MpBeatmap?>? mpLevelTask = null;
					if (packet == null)
					{
						Plugin.Logger.Debug("Could not find packet, trying beatsaver");
						mpLevelTask = _mpBeatmapLevelProvider.GetBeatmap(levelHash);
						yield return IPA.Utilities.Async.Coroutines.WaitForTask(mpLevelTask);
						Plugin.Logger.Debug($"Task finished SongName={mpLevelTask.Result?.SongName}");
						instance._suggestedLevelText.text = mpLevelTask.Result?.SongName;
					}

					displayLevelText = packet != null || mpLevelTask?.Result != null;
					Plugin.Logger.Debug($"Will display level text? {displayLevelText}");
				}

				instance._suggestedCharacteristicIcon.sprite = key.beatmapCharacteristic.icon;
				instance._suggestedDifficultyText.text = key.difficulty.ShortName();
			} else Plugin.Logger.Debug("Player key was invalid");
			SetLevelFoundValues(instance, displayLevelText);
			bool anyModifiers = !(playerData?.gameplayModifiers?.IsWithoutModifiers() ?? true);
			instance._suggestedModifiersList.gameObject.SetActive(anyModifiers);
			instance._emptySuggestedModifiersText.gameObject.SetActive(!anyModifiers);

			if (anyModifiers)
			{
				var modifiers = instance._gameplayModifiers.CreateModifierParamsList(playerData.gameplayModifiers);
				instance._emptySuggestedModifiersText.gameObject.SetActive(modifiers.Count == 0);
				if (modifiers.Count > 0)
				{
					instance._suggestedModifiersList.SetData(modifiers.Count, (int id, GameplayModifierInfoListItem listItem) => listItem.SetModifier(modifiers[id], false));
				}
			}

			instance._useModifiersButton.interactable = !connectedPlayer.isMe && anyModifiers && allowSelection;
			instance._kickPlayerButton.interactable = !connectedPlayer.isMe && hasKickPermissions && allowSelection;
			instance._mutePlayerButton.gameObject.SetActive(false);
			if (getLevelEntitlementTask != null && !connectedPlayer.isMe)
			{
				instance._useBeatmapButtonHoverHint.text = Localization.Get("LABEL_CANT_START_GAME_DO_NOT_OWN_SONG");
				instance.SetBeatmapUseButtonEnabledAsync(getLevelEntitlementTask);
				yield break;
			}

			instance._useBeatmapButton.interactable = false;
			instance._useBeatmapButtonHoverHint.enabled = false;

		}

		void SetLevelFoundValues(GameServerPlayerTableCell __instance, bool displayLevelText)
        {
            __instance._suggestedLevelText.gameObject.SetActive(displayLevelText);
            __instance._suggestedCharacteristicIcon.gameObject.SetActive(displayLevelText);
            __instance._suggestedDifficultyText.gameObject.SetActive(displayLevelText);
            __instance._emptySuggestedLevelText.gameObject.SetActive(!displayLevelText);
        }
    }
}
