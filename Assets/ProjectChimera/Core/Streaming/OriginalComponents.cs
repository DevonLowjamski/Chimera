using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Core.Streaming
{
    public class OriginalComponents
    {
        // Fallback simple state captures to avoid missing type errors
        public Dictionary<Renderer, bool> RendererStates; // true if enabled
        public Dictionary<ParticleSystem, bool> ParticleStates; // true if playing
    }
}
