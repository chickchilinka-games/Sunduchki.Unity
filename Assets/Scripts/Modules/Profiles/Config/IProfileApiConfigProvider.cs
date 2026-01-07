using Modules.Profiles.Data;

namespace Modules.Profiles.Config
{
    public interface IProfileApiConfigProvider
    {
        ProfileApiConfig GetConfig();
    }
}
