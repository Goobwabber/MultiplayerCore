using MultiplayerCore.UI;
using Zenject;

namespace MultiplayerCore.Installers
{
    class MpMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MpColorsUI>().AsSingle();
            Container.BindInterfacesAndSelfTo<MpRequirementsUI>().AsSingle();
            Container.BindInterfacesAndSelfTo<MpLoadingIndicator>().AsSingle();

            // Inject sira stuff that didn't get injected on appinit
            Container.Inject(Container.Resolve<NetworkPlayerEntitlementChecker>());
        }
    }
}
