using System;

namespace ProjectChimera.Core
{
    public interface IServiceScope : IDisposable
    {
        ProjectChimera.Core.DependencyInjection.IServiceProvider ServiceProvider { get; }
    }
}
