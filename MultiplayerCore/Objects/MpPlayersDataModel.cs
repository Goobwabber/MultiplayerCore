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
            base.SetPlayerBeatmapLevel(player.userId, preview, packet.difficulty, characteristic);
        }

        public override void HandleMenuRpcManagerGetRecommendedBeatmap(string userId)
        {
            ILobbyPlayerData localPlayerData = playersData[localUserId];

            if (localPlayerData.beatmapLevel is MpBeatmapLevel mpexBeatmapLevel)
                _multiplayerSessionManager.Send(new MpBeatmapPacket(mpexBeatmapLevel, localPlayerData.beatmapCharacteristic.serializedName, localPlayerData.beatmapDifficulty));

            base.HandleMenuRpcManagerGetRecommendedBeatmap(userId);
        }

        public override void HandleMenuRpcManagerRecommendBeatmap(string userId, BeatmapIdentifierNetSerializable beatmapId)
        {
            if (!string.IsNullOrEmpty(Utilities.HashForLevelID(beatmapId.levelID)))
                return;
            base.HandleMenuRpcManagerRecommendBeatmap(userId, beatmapId);
        }

        public async override void SetLocalPlayerBeatmapLevel(string levelId, BeatmapDifficulty beatmapDifficulty, BeatmapCharacteristicSO characteristic)
        {
            _logger.Debug($"Local player selected song '{levelId}'");
            string? levelHash = Utilities.HashForLevelID(levelId);
            if (!string.IsNullOrEmpty(levelHash))
            {
                IPreviewBeatmapLevel? beatmapLevel = await _beatmapLevelProvider.GetBeatmap(levelHash);
                if (beatmapLevel == null)
                    return;

                _multiplayerSessionManager.Send(new MpBeatmapPacket(beatmapLevel, characteristic.serializedName, beatmapDifficulty));
                _menuRpcManager.RecommendBeatmap(new BeatmapIdentifierNetSerializable(levelId, characteristic.serializedName, beatmapDifficulty));
                SetPlayerBeatmapLevel(localUserId, beatmapLevel, beatmapDifficulty, characteristic);
                return;
            }
            base.SetLocalPlayerBeatmapLevel(levelId, beatmapDifficulty, characteristic);
        }
    }
}
