using JetBrains.Annotations;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Objects;
using MultiplayerCore.UI;
using SiraUtil.Affinity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace MultiplayerCore.Patchers
{
    internal class BeatmapSelectionViewPatcher : IAffinity
    {
        private MpPlayersDataModel _mpPlayersDataModel;
        private MpBeatmapLevelProvider _mpBeatmapLevelProvider;


        BeatmapSelectionViewPatcher(MpPlayersDataModel mpPlayersDataModel, MpBeatmapLevelProvider mpBeatmapLevelProvider)
        {
            _mpPlayersDataModel = mpPlayersDataModel;
            _mpBeatmapLevelProvider = mpBeatmapLevelProvider;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(EditableBeatmapSelectionView), nameof(EditableBeatmapSelectionView.SetBeatmap))]
        public bool EditableBeatmapSelectionView_SetBeatmap(EditableBeatmapSelectionView ___instance, in BeatmapKey beatmapKey)
        {
            if (!beatmapKey.IsValid()) return true;

            var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
            if (String.IsNullOrWhiteSpace(levelHash)) return true;

            var packet = _mpPlayersDataModel.FindLevelPacket(levelHash!);
            if (packet == null) return true;

            ___instance._clearButton.gameObject.SetActive(___instance.showClearButton);
            ___instance._noLevelText.enabled = false;
            ___instance._levelBar.hide = false;

            // TODO: create a level to provide to the levelbar, on quest the beatmaplevelprovider actually provides game BeatmapLevels
            // var level = _mpBeatmapLevelProvider.GetBeatmapFromPacket(packet)
            // ___instance._levelBar.Setup(level, beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
            return false;
        }
    }
}
