using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Data.Genetics
{
    [Serializable]
    public class GeneticProfile
    {
        [Header("Genetic Information")]
        public string ProfileId;
        public BaseSpecies BaseSpecies;

        [Header("Traits")]
        public float GeneticYieldModifier = 1f;
        public float DiseaseResistance = 0.5f;
        public float StressTolerance = 0.5f;

        [Header("Chemical Profile")]
        public CannabinoidProfile BaselineProfile;
        public TerpeneProfile BaseTerpenes;

        public GeneticProfile()
        {
            ProfileId = System.Guid.NewGuid().ToString();
        }
    }
}
