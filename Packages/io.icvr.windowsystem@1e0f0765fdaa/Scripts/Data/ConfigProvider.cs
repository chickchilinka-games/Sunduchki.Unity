

using UnityEngine;
using Zenject;

namespace ICVR.Window.Data
{
    internal class ConfigProvider : IInitializable
    {
        public WindowSystemConfig Config { get; private set; }
        
        public void Initialize()
        {
            Config = Resources.Load<WindowSystemConfig>(Consts.Name.ConfigAssetName);
        }
    }
}