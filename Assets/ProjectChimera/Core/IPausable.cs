using ProjectChimera.Core.Logging;
namespace ProjectChimera.Core
{
    public interface IPausable
    {
        void OnPause();
        void OnResume();
    }
}
