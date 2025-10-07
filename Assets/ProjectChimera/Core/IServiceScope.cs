using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    public interface IServiceScope : IDisposable
    {
        IServiceProvider ServiceProvider { get; }
    }
}
