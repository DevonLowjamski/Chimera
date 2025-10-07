using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Cultivation
{
    public class CultivationPathEffects
    {
        public string EffectId;
        public CultivationApproach Approach;
        public Dictionary<string, float> StatModifiers;
        public Dictionary<string, bool> FeatureUnlocks;
        public float Duration;
        public bool IsActive;
        public System.DateTime ActivationTime;
    }
}
