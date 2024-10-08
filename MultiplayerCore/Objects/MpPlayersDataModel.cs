using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MultiplayerCore.Beatmaps;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Networking;
using SiraUtil.Logging;

namespace MultiplayerCore.Objects
{
    [UsedImplicitly]
    public class MpPlayersDataModel : LobbyPlayersDataModel, ILobbyPlayersDataModel, IDisposable
    {
        private readonly MpPacketSerializer _packetSerializer;
        internal readonly MpBeatmapLevelProvider _beatmapLevelProvider;
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
            _multiplayerSessionManager.playerConnectedEvent += HandlePlayerConnected;
        }

        public new void Deactivate()
        {
            _packetSerializer.UnregisterCallback<MpBeatmapPacket>();
            _menuRpcManager.getRecommendedBeatmapEvent -= this.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.getRecommendedBeatmapEvent += base.HandleMenuRpcManagerGetRecommendedBeatmap;
            _menuRpcManager.recommendBeatmapEvent -= this.HandleMenuRpcManagerRecommendBeatmap;
            _menuRpcManager.recommendBeatmapEvent += base.HandleMenuRpcManagerRecommendBeatmap;
            _multiplayerSessionManager.playerConnectedEvent -= HandlePlayerConnected;
			base.Deactivate();
        }

        public new void Dispose()
            => Deactivate();

        internal void HandlePlayerConnected(IConnectedPlayer connectedPlayer)
        {
            // Send our MpBeatmapPacket again so newly joined players have it
            var selectedBeatmapKey = _playersData[localUserId].beatmapKey;
			if (selectedBeatmapKey.IsValid()) SendMpBeatmapPacket(selectedBeatmapKey, connectedPlayer);
		}
		internal void SetLocalPlayerBeatmapLevel_override(in BeatmapKey beatmapKey)
        {
            // Game: The local player has selected / recommended a beatmap

            // send extended beatmap info to other players
            SendMpBeatmapPacket(beatmapKey);
            
            //base.SetLocalPlayerBeatmapLevel(userId, in beatmapKey);
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
            SendMpBeatmapPacket(selectedBeatmapKey);

            base.HandleMenuRpcManagerGetRecommendedBeatmap(userId);
        }

        private new void HandleMenuRpcManagerRecommendBeatmap(string userId, BeatmapKeyNetSerializable beatmapKeySerializable)
        {
            // RPC: Another player has recommended a beatmap (base game)

            var levelHash = Utilities.HashForLevelID(beatmapKeySerializable.levelID);
			if (!string.IsNullOrEmpty(levelHash) && _beatmapLevelProvider.TryGetBeatmapFromPacketHash(levelHash!) != null) // If we have no packet run basegame behaviour
                return;
            
            base.HandleMenuRpcManagerRecommendBeatmap(userId, beatmapKeySerializable);
        }

        private void SendMpBeatmapPacket(BeatmapKey beatmapKey, IConnectedPlayer? player = null)
        {
            var levelId = beatmapKey.levelId;
            _logger.Debug($"Sending beatmap packet for level {levelId}");

            var levelHash = Utilities.HashForLevelID(levelId);
            if (levelHash == null)
            {
                _logger.Debug("Not a custom level, returning...");
                return;
            }
            
            var levelData = _beatmapLevelProvider.GetBeatmapFromLocalBeatmaps(levelHash);
            var packet = (levelData != null) ? new MpBeatmapPacket(levelData, beatmapKey) : FindLevelPacket(levelHash);
            if (packet == null)
            {
                _logger.Warn($"Could not get level data for beatmap '{levelHash}', returning!");
                return;
            }

            if (player != null)
				_multiplayerSessionManager.SendToPlayer(packet, player);
            else
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

        public MpBeatmap GetLevelLocalOrFromPacketOrDummy(string levelHash)
        {
	        var level = _beatmapLevelProvider.GetBeatmapFromLocalBeatmaps(levelHash);
	        if (level == null)
	        {
		        var packet = FindLevelPacket(levelHash);
		        if (packet != null) level = _beatmapLevelProvider.GetBeatmapFromPacket(packet);
	        }
	        if (level == null) level = new NoInfoBeatmapLevel(levelHash);
            return level;
        }

    }
}
