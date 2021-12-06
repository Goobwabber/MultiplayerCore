using Zenject;

namespace MultiplayerCore.Installers
{
    class MpMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            // Inject sira stuff that didn't get injected on appinit
            Container.Inject(Container.Resolve<NetworkPlayerEntitlementChecker>());
        }
    }
}
