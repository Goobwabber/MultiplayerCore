using SiraUtil.Affinity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerCore.UI;
using MultiplayerCore.Objects;
using Zenject;
using System.Diagnostics.Eventing.Reader;
using BGLib.Polyglot;

namespace MultiplayerCore.Patchers
{
    internal class GameServerPlayerTableCellPatcher : IAffinity
    {
        private MpPlayersDataModel _mpPlayersDataModel;

        GameServerPlayerTableCellPatcher(ILobbyPlayersDataModel playersDataModel) => _mpPlayersDataModel = playersDataModel as MpPlayersDataModel;

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
            bool validKey = key.IsValid();
            bool displayLevelText = validKey;
            if (validKey)
            {
                var level = __instance._beatmapLevelsModel.GetBeatmapLevel(key.levelId);
                var levelHash = Utilities.HashForLevelID(key.levelId);
                __instance._suggestedLevelText.text = level?.songName;
                displayLevelText = level != null;

                if (level == null && _mpPlayersDataModel != null && !string.IsNullOrEmpty(levelHash)) // we didn't have the level, but we can attempt to get the packet
                {
                    var packet = _mpPlayersDataModel.FindLevelPacket(levelHash);
                    __instance._suggestedLevelText.text = packet?.songName;
                    displayLevelText = packet != null;
                }

                __instance._suggestedCharacteristicIcon.sprite = key.beatmapCharacteristic.icon;
                __instance._suggestedDifficultyText.text = key.difficulty.ShortName();
            }
            SetLevelFoundValues(__instance, displayLevelText);
            bool anyModifiers = !(playerData?.gameplayModifiers?.IsWithoutModifiers() ?? true);
            __instance._suggestedModifiersList.gameObject.SetActive(anyModifiers);
            __instance._emptySuggestedModifiersText.gameObject.SetActive(!anyModifiers);

            if (anyModifiers)
            {
                var modifiers = __instance._gameplayModifiers.CreateModifierParamsList(playerData.gameplayModifiers);
                __instance._emptySuggestedModifiersText.gameObject.SetActive(modifiers.Count == 0);
                if (modifiers.Count > 0)
                {
                    __instance._suggestedModifiersList.SetData(modifiers.Count, (int id, GameplayModifierInfoListItem listItem) => listItem.SetModifier(modifiers[id], false));
                }
            }

            __instance._useModifiersButton.interactable = !connectedPlayer.isMe && anyModifiers && allowSelection;
            __instance._kickPlayerButton.interactable = !connectedPlayer.isMe && hasKickPermissions && allowSelection;
            __instance._mutePlayerButton.gameObject.SetActive(false);
            if (getLevelEntitlementTask != null && !connectedPlayer.isMe)
            {
                __instance._useBeatmapButtonHoverHint.text = Localization.Get("LABEL_CANT_START_GAME_DO_NOT_OWN_SONG");
                __instance.SetBeatmapUseButtonEnabledAsync(getLevelEntitlementTask);
                return false;
            }

            __instance._useBeatmapButton.interactable = false;
            __instance._useBeatmapButtonHoverHint.enabled = false;

            return false;
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
