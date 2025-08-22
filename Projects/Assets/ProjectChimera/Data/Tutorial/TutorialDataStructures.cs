using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Data.Tutorial
{
    [Serializable]
    public class EducationalProgress
    {
        [Header("Educational Progress")]
        public string PlayerId;
        public string ObjectiveId;
        public float CompletionPercentage = 0f;
        public DateTime StartTime = DateTime.Now;
        public DateTime? CompletionTime = null;
        public List<string> CompletedSteps = new List<string>();
        public Dictionary<string, object> ProgressData = new Dictionary<string, object>();

        public EducationalProgress()
        {
            CompletedSteps = new List<string>();
            ProgressData = new Dictionary<string, object>();
        }
    }
}
