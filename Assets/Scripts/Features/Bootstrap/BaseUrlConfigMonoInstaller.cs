using Modules.AuthenticationSystem.Config;
using UnityEngine;
using Zenject;

namespace Features.Bootstrap
{
    public class BaseUrlConfigMonoInstaller : MonoInstaller
    {
        [SerializeField]
        private BaseUrlConfigAsset _baseUrlConfigAsset;

        public override void InstallBindings()
        {
            Container.BindInstance(_baseUrlConfigAsset).AsSingle();
        }
    }
}
