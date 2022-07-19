using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MultiplayerCore.Networking;
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
        private readonly IPlatformUserModel _platformUserModel;
        private readonly IMultiplayerSessionManager _sessionManager;

        internal MpPlayerManager(
            MpPacketSerializer packetSerializer,
            IPlatformUserModel platformUserModel,
            IMultiplayerSessionManager sessionManager)
        {
            _packetSerializer = packetSerializer;
            _platformUserModel = platformUserModel;
            _sessionManager = sessionManager;
        }

        public async void Initialize()
        {
            _sessionManager.SetLocalPlayerState("modded", true);
            _packetSerializer.RegisterCallback<MpPlayerData>(HandlePlayerData);
            _localPlayerInfo = await _platformUserModel.GetUserInfo();
            _sessionManager.playerConnectedEvent += HandlePlayerConnected;
        }

        public void Dispose()
        {
            _packetSerializer.UnregisterCallback<MpPlayerData>();
        }

        private void HandlePlayerConnected(IConnectedPlayer player)
        {
            var playerData = new MpPlayerData
            {
                PlatformId = _localPlayerInfo.platformUserId,
                Platform = _localPlayerInfo.platform switch
                {
                    UserInfo.Platform.Test => Platform.Unknown,
                    UserInfo.Platform.Steam => Platform.Steam,
                    UserInfo.Platform.Oculus => Platform.OculusPC,
                    UserInfo.Platform.PS4 => Platform.PS4,
                    _ => throw new NotImplementedException()
                }
            };
            _sessionManager.Send(playerData);
            PlayerConnectedEvent?.Invoke(player, playerData);
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