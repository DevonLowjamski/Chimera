using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Camera
{
    public interface ICameraProvider
    {
        UnityEngine.Camera main { get; }
    }
}
