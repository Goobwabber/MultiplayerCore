using MultiplayerCore.Patchers;
using Zenject;

namespace MultiplayerCore.Installers
{
    class MpGameInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<IntroAnimationPatcher>().AsSingle();
            Container.BindInterfacesAndSelfTo<OutroAnimationPatcher>().AsSingle();
        }
    }
}
