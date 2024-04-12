using MultiplayerCore.Patchers;
using MultiplayerCore.UI;
using Zenject;

namespace MultiplayerCore.Installers
{
    internal class MpMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MpColorsUI>().AsSingle();
            Container.BindInterfacesAndSelfTo<MpRequirementsUI>().AsSingle();
            Container.BindInterfacesAndSelfTo<MpLoadingIndicator>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameServerPlayerTableCellPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatmapSelectionViewPatcher>().AsSingle();

            // Inject sira stuff that didn't get injected on appinit
            Container.Inject(Container.Resolve<NetworkPlayerEntitlementChecker>());
        }
    }
}
