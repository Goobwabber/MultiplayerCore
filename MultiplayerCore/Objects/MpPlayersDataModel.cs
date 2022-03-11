using MultiplayerCore.Beatmaps;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Networking;
using SiraUtil.Logging;
using System;

namespace MultiplayerCore.Objects
{
    public class MpPlayersDataModel : LobbyPlayersDataModel, ILobbyPlayersDataModel, IDisposable
    {
        private readonly MpPacketSerializer _packetSerializer;
        private readonly MpBeatmapLevelProvider _beatmapLevelProvider;
        private readonly SiraLog _logger;

        internal MpPlayersDataModel(
            MpPacketSerializer packetSerializer,
            MpBeatmapLevelProvider beatmapLevelProvider,
            SiraLog logger)
        {
            _packetSerializer = packetSerializer;
            _beatmapLevelProvider = beatmapLevelProvider;
            _logger = logger;
        }

        public override void Activate()
        {
            _packetSerializer.RegisterCallback<MpBeatmapPacket>(HandleMpexBeatmapPacket);
            base.Activate();
            _menuRpcManager.getRecommendedBeatmapEvent -= base.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.getRecommendedBeatmapEvent += this.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.recommendBeatmapEvent -= base.HandleMenuRpcManagerRecommendBeatmap;
            _menuRpcManager.recommendBeatmapEvent += this.HandleMenuRpcManagerRecommendBeatmap;
        }

        public override void Deactivate()
        {
            _packetSerializer.UnregisterCallback<MpBeatmapPacket>();
            _menuRpcManager.getRecommendedBeatmapEvent -= this.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.getRecommendedBeatmapEvent += base.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.recommendBeatmapEvent -= this.HandleMenuRpcManagerRecommendBeatmap;
            _menuRpcManager.recommendBeatmapEvent += base.HandleMenuRpcManagerRecommendBeatmap;
            base.Deactivate();
        }

        public new void Dispose()
            => Deactivate();

        private void HandleMpexBeatmapPacket(MpBeatmapPacket packet, IConnectedPlayer player)
        {
            _logger.Debug($"'{player.userId}' selected song '{packet.levelHash}'.");
            BeatmapCharacteristicSO characteristic = _beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(packet.characteristic);
            MpBeatmapLevel preview = new NetworkBeatmapLevel(packet);
            base.SetPlayerBeatmapLevel(player.userId, new PreviewDifficultyBeatmap(preview, characteristic, packet.difficulty));
        }

        public override void HandleMenuRpcManagerGetRecommendedBeatmap(string userId)
        {
            ILobbyPlayerData localPlayerData = _playersData[userId];
            if (localPlayerData.beatmapLevel.beatmapLevel is MpBeatmapLevel)
                _multiplayerSessionManager.Send(new MpBeatmapPacket(localPlayerData.beatmapLevel));

            base.HandleMenuRpcManagerGetRecommendedBeatmap(userId);
        }

        public override void HandleMenuRpcManagerRecommendBeatmap(string userId, BeatmapIdentifierNetSerializable beatmapId)
        {
            if (!string.IsNullOrEmpty(Utilities.HashForLevelID(beatmapId.levelID)))
                return;
            base.HandleMenuRpcManagerRecommendBeatmap(userId, beatmapId);
        }

        public async override void SetLocalPlayerBeatmapLevel(PreviewDifficultyBeatmap beatmapLevel)
        {
            _logger.Debug($"Local player selected song '{beatmapLevel.beatmapLevel.levelID}'");
            string? levelHash = Utilities.HashForLevelID(beatmapLevel.beatmapLevel.levelID);
            if (!string.IsNullOrEmpty(levelHash))
            {
                IPreviewBeatmapLevel? previewBeatmap = await _beatmapLevelProvider.GetBeatmap(levelHash);
                if (previewBeatmap == null)
                    return;

                beatmapLevel.beatmapLevel = previewBeatmap;
                _multiplayerSessionManager.Send(new MpBeatmapPacket(beatmapLevel));
                _menuRpcManager.RecommendBeatmap(beatmapLevel.ToIdentifier());
                SetPlayerBeatmapLevel(localUserId, beatmapLevel);
                return;
            }
            base.SetLocalPlayerBeatmapLevel(beatmapLevel);
        }
    }
}
