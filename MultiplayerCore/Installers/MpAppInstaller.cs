using BeatSaverSharp;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Networking;
using MultiplayerCore.NodePoseSyncState;
using MultiplayerCore.Objects;
using MultiplayerCore.Patchers;
using MultiplayerCore.Players;
using MultiplayerCore.Repositories;
using SiraUtil.Zenject;
using Zenject;

namespace MultiplayerCore.Installers
{
    internal class MpAppInstaller : Installer
    {
        private readonly BeatSaver _beatsaver;

        public MpAppInstaller(
            BeatSaver beatsaver)
        {
            _beatsaver = beatsaver;
        }

        public override void InstallBindings()
        {
            Container.BindInstance(new UBinder<Plugin, BeatSaver>(_beatsaver)).AsSingle();
            Container.BindInterfacesAndSelfTo<MpPacketSerializer>().AsSingle();
            Container.BindInterfacesAndSelfTo<MpPlayerManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<MpNodePoseSyncStateManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<MpScoreSyncStateManager>().AsSingle();
            Container.Bind<MpLevelDownloader>().ToSelf().AsSingle();
            Container.Bind<MpBeatmapLevelProvider>().ToSelf().AsSingle();
            Container.BindInterfacesAndSelfTo<CustomLevelsPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<NetworkConfigPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<ModeSelectionPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerCountPatcher>().AsSingle();
            Container.Bind<BGNetDebugLogger>().ToSelf().AsSingle();
            Container.BindInterfacesAndSelfTo<MpStatusRepository>().AsSingle();
        }
    }
}
