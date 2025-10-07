using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Systems.Rendering.Core;
using CoreGrowLight = ProjectChimera.Systems.Rendering.Core.GrowLight;

namespace ProjectChimera.Systems.Rendering
{
    public class LightingZone
    {
        public int ID;
        public Bounds Bounds;
        public LightingZoneType ZoneType;
        public bool IsActive;
        public List<CoreGrowLight> AssignedLights;
        public float LightingLevel;
    }
}
