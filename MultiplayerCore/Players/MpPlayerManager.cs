using MultiplayerCore.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zenject;

namespace MultiplayerCore.Players
{
    public class MpPlayerManager : IInitializable, IDisposable
    {
        public event Action<IConnectedPlayer, MpPlayerData> PlayerConnectedEvent = null!;

        public IReadOnlyDictionary<string, MpPlayerData> Players => _playerData;

        private UserInfo _localPlayerInfo = null!;
        private ConcurrentDictionary<string, MpPlayerData> _playerData = new();

        private readonly MpPacketSerializer _packetSerializer;
        private readonly IMultiplayerSessionManager _sessionManager;

        internal MpPlayerManager(
            MpPacketSerializer packetSerializer,
            IMultiplayerSessionManager sessionManager)
        {
            _packetSerializer = packetSerializer;
            _sessionManager = sessionManager;
        }

        public async void Initialize()
        {
            _sessionManager.SetLocalPlayerState("modded", true);
            _packetSerializer.RegisterCallback<MpPlayerData>(HandlePlayerData);
        }

        public void Dispose()
        {
            _packetSerializer.UnregisterCallback<MpPlayerData>();
        }

        private void HandlePlayerData(MpPlayerData packet, IConnectedPlayer player)
        {
            _playerData[player.userId] = packet;
        }

        public bool TryGetPlayer(string userId, out MpPlayerData player)
            => _playerData.TryGetValue(userId, out player);

        public MpPlayerData? GetPlayer(string userId)
            => _playerData.ContainsKey(userId) ? _playerData[userId] : null;
    }
}
