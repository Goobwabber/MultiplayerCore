using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MultiplayerCore.Beatmaps.Packets;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Networking;
using SiraUtil.Logging;

namespace MultiplayerCore.Objects
{
    [UsedImplicitly]
    internal class MpPlayersDataModel : LobbyPlayersDataModel, ILobbyPlayersDataModel, IDisposable
    {
        private readonly MpPacketSerializer _packetSerializer;
        private readonly MpBeatmapLevelProvider _beatmapLevelProvider;
        private readonly SiraLog _logger;
        private readonly Dictionary<string, MpBeatmapPacket> _lastPlayerBeatmapPackets = new();
        public IReadOnlyDictionary<string, MpBeatmapPacket> PlayerPackets => _lastPlayerBeatmapPackets;

        internal MpPlayersDataModel(
            MpPacketSerializer packetSerializer,
            MpBeatmapLevelProvider beatmapLevelProvider,
            SiraLog logger)
        {
            _packetSerializer = packetSerializer;
            _beatmapLevelProvider = beatmapLevelProvider;
            _logger = logger;
        }

        public new void Activate()
        {
            _packetSerializer.RegisterCallback<MpBeatmapPacket>(HandleMpCoreBeatmapPacket);
            base.Activate();
            _menuRpcManager.getRecommendedBeatmapEvent -= base.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.getRecommendedBeatmapEvent += this.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.recommendBeatmapEvent -= base.HandleMenuRpcManagerRecommendBeatmap;
            _menuRpcManager.recommendBeatmapEvent += this.HandleMenuRpcManagerRecommendBeatmap;
        }

        public new void Deactivate()
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

        internal void SetPlayerBeatmapLevel_override(string userId, in BeatmapKey beatmapKey)
        {
            // Game: A player (can be the local player!) has selected / recommended a beatmap

            if (userId == _multiplayerSessionManager.localPlayer.userId)
                // If local player: send extended beatmap info to other players
                _ = SendMpBeatmapPacket(beatmapKey);
            
            //base.SetPlayerBeatmapLevel(userId, in beatmapKey);
        }
        
        private void HandleMpCoreBeatmapPacket(MpBeatmapPacket packet, IConnectedPlayer player)
        {
            // Packet: Another player has recommended a beatmap (MpCore), we have received details for the level preview
            
            _logger.Debug($"'{player.userId}' selected song '{packet.levelHash}'.");
            
            var beatmap = _beatmapLevelProvider.GetBeatmapFromPacket(packet);
            var characteristic = _beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(packet.characteristicName);

            PutPlayerPacket(player.userId, packet);
            base.SetPlayerBeatmapLevel(player.userId, new BeatmapKey(beatmap.LevelID, characteristic, packet.difficulty));
        }

        private new void HandleMenuRpcManagerGetRecommendedBeatmap(string userId)
        {
            // RPC: The server / another player has asked us to send our recommended beatmap
            
            var selectedBeatmapKey = _playersData[localUserId].beatmapKey;
            _ = SendMpBeatmapPacket(selectedBeatmapKey);

            base.HandleMenuRpcManagerGetRecommendedBeatmap(userId);
        }

        private new void HandleMenuRpcManagerRecommendBeatmap(string userId, BeatmapKeyNetSerializable beatmapKeySerializable)
        {
            // RPC: Another player has recommended a beatmap (base game)
            
            if (!string.IsNullOrEmpty(Utilities.HashForLevelID(beatmapKeySerializable.levelID)))
                return;
            
            base.HandleMenuRpcManagerRecommendBeatmap(userId, beatmapKeySerializable);
        }

        private async Task SendMpBeatmapPacket(BeatmapKey beatmapKey)
        {
            var levelId = beatmapKey.levelId;
            _logger.Debug($"Sending beatmap packet for level {levelId}");

            var levelHash = Utilities.HashForLevelID(levelId);
            if (levelHash == null)
            {
                _logger.Debug("Not a custom level, returning...");
                return;
            }
            
            var levelData = await _beatmapLevelProvider.GetBeatmap(levelHash);
            if (levelData == null)
            {
                _logger.Debug("Could not get level data for beatmap, returning!");
                return;
            }

            var packet = new MpBeatmapPacket(levelData, beatmapKey);
            _logger.Debug("Actually sending packet");
            _multiplayerSessionManager.Send(packet);
        }

        public MpBeatmapPacket? GetPlayerPacket(string playerId)
        {
            _lastPlayerBeatmapPackets.TryGetValue(playerId, out var packet);
            _logger.Debug($"Got player packet for {playerId} with levelHash: {packet?.levelHash ?? "NULL"}");
            return packet;
        }

        private void PutPlayerPacket(string playerId, MpBeatmapPacket packet)
        {
            _logger.Debug($"Putting packet for player {playerId} with levelHash: {packet.levelHash}");
            _lastPlayerBeatmapPackets[playerId] = packet;
        }

        public MpBeatmapPacket? FindLevelPacket(string levelHash)
        {
            var packet = _lastPlayerBeatmapPackets.Values.FirstOrDefault(packet => packet.levelHash == levelHash);
            _logger.Debug($"Found packet: {packet?.levelHash ?? "NULL"}");
            return packet;
        }

    }
}
