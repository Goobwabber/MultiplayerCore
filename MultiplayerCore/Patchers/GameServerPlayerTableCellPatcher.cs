using SiraUtil.Affinity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerCore.UI;
using MultiplayerCore.Objects;
using Zenject;

namespace MultiplayerCore.Patchers
{
    internal class GameServerPlayerTableCellPatcher : IAffinity
    {
        private MpPlayersDataModel _mpPlayersDataModel;

        GameServerPlayerTableCellPatcher(MpPlayersDataModel mpPlayersDataModel) => _mpPlayersDataModel = mpPlayersDataModel;

        [AffinityPatch(typeof(GameServerPlayerTableCell), nameof(GameServerPlayerTableCell.SetData))]
        void GameServerPlayerTableCell_SetData(GameServerPlayerTableCell ___instance, IConnectedPlayer connectedPlayer, ILobbyPlayerData playerData)
        {
            var beatmapKey = playerData.beatmapKey;
            if (!beatmapKey.IsValid()) return;

            var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
            if (string.IsNullOrEmpty(levelHash)) return;

            var packet = _mpPlayersDataModel.PlayerPackets[connectedPlayer.userId];
            if (packet == null || packet.levelHash != levelHash) return;

            ___instance._suggestedLevelText.text = packet.songName;
        }

    }
}
