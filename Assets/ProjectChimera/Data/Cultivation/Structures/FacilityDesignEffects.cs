using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Cultivation
{
    public class FacilityDesignEffects
    {
        public string EffectId;
        public FacilityDesignApproach Approach;
        public Dictionary<string, float> EfficiencyModifiers;
        public Dictionary<string, bool> DesignFeatures;
        public float Duration;
        public bool IsActive;
        public float ActivationTime;
    }
}
