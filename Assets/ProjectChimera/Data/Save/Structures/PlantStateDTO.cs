using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save
{
    public class PlantStateDTO
    {
        public string PlantId;
        public string StrainName;
        public UnityEngine.Vector3 Position;
        public float Age;
        public float Health;
        public float GrowthStage;
        public float NutrientLevel = 1f;
        public float WaterLevel = 1f;
        public bool IsHealthy = true;
        public System.DateTime LastUpdate;
    }
}
