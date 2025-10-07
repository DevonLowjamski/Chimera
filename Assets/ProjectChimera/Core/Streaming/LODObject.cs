using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Streaming.LOD;

namespace ProjectChimera.Core.Streaming
{
    public class LODObject
    {
        public int ObjectId;
        public GameObject GameObject;
        public Transform Transform;
        public LODObjectType ObjectType;
        public float CustomBias;
        public int CurrentLODLevel;
        public float LastUpdateTime;
        public bool IsVisible;
        public float DistanceFromCenter;
        public LODComponentCache OriginalComponents;
    }
}
