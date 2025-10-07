using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Cultivation
{
    public class FacilityDesignData
    {
        public string DesignId;
        public FacilityDesignApproach Approach;
        public Dictionary<string, float> EfficiencyMetrics;
        public Dictionary<string, string> DesignParameters;
    }
}
