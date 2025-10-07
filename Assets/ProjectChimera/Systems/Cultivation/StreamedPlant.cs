using UnityEngine;
using ProjectChimera.Core.Streaming;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Systems.Cultivation.Pooling;
using System.Collections.Generic;
using System.Collections;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Cultivation
{
    public class StreamedPlant
    {
        public PlantInstance PlantData;
        public Vector3 Position;
        public bool IsLoaded;
        public bool IsVisible;
        public int CurrentLODLevel;
        public float LastUpdateTime;
        public float DistanceFromViewer;
        public ProjectChimera.Core.Streaming.Core.StreamingPriority StreamingPriority;
        public GameObject PlantGameObject;
        public PlantInstanceComponent PlantComponent;
        public int LODObjectId = -1;
    }
}