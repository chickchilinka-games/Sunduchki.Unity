using Zenject;

namespace Core.Installers
{
    public abstract class DebuggerPluginInstaller : ScriptableObjectInstaller
    {
        public abstract bool IsActive { get; }

        public abstract override void InstallBindings();
    }
}