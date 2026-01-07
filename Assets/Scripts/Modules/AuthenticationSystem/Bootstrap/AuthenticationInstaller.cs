using Modules.AuthenticationSystem.Adapters;
using Modules.AuthenticationSystem.Entities;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Providers;
using Modules.AuthenticationSystem.Services;
using UnityEngine;
using Zenject;

namespace Modules.AuthenticationSystem.Bootstrap
{
    public class AuthenticationInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<AuthenticationModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<AuthenticationService>().AsSingle();
        }
    }
}
