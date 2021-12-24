using BeatSaverSharp;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Networking;
using MultiplayerCore.Objects;
using MultiplayerCore.Patchers;
using MultiplayerCore.Players;
using SiraUtil.Zenject;
using Zenject;

namespace MultiplayerCore.Installers
{
    class MpAppInstaller : Installer
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
            Container.Bind<MpPacketSerializer>().ToSelf().AsSingle();
            Container.Bind<MpPlayerManager>().ToSelf().AsSingle();
            Container.Bind<MpLevelDownloader>().ToSelf().AsSingle();
            Container.Bind<MpBeatmapLevelProvider>().ToSelf().AsSingle();
            Container.BindInterfacesAndSelfTo<CustomLevelsPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<NetworkConfigPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerCountPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<LoggingPatcher>().AsSingle();
            Container.Bind<BGNetDebugLogger>().ToSelf().AsSingle();
        }
    }
}
