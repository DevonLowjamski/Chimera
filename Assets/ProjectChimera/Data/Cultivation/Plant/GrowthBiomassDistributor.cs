// REFACTORED: Growth Biomass Distributor
// Extracted from PlantGrowthProcessor for better separation of concerns

using UnityEngine;
using System;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Handles biomass distribution and calculation for plant growth
    /// </summary>
    [System.Serializable]
    public class GrowthBiomassDistributor
    {
        // Growth rates
        [SerializeField] private float _rootDevelopmentRate = 1f;
        [SerializeField] private float _leafGrowthRate = 1.2f;
        [SerializeField] private float _stemGrowthRate = 0.8f;

        // Biomass state
        [SerializeField] private float _totalBiomass = 0f;
        [SerializeField] private float _rootMass = 0f;
        [SerializeField] private float _leafMass = 0f;
        [SerializeField] private float _stemMass = 0f;

        public float TotalBiomass => _totalBiomass;
        public float RootMass => _rootMass;
        public float LeafMass => _leafMass;
        public float StemMass => _stemMass;

        public float RootDevelopmentRate => _rootDevelopmentRate;
        public float LeafGrowthRate => _leafGrowthRate;
        public float StemGrowthRate => _stemGrowthRate;

        public void Initialize()
        {
            _totalBiomass = 0f;
            _rootMass = 0f;
            _leafMass = 0f;
            _stemMass = 0f;
        }

        public void SetGrowthRates(float rootRate, float leafRate, float stemRate)
        {
            _rootDevelopmentRate = rootRate;
            _leafGrowthRate = leafRate;
            _stemGrowthRate = stemRate;
        }

        public void DistributeBiomass(float dailyBiomassGain, float growthProgress)
        {
            // Calculate distribution ratios based on growth stage
            float rootRatio = GetRootMassRatio(growthProgress);
            float leafRatio = GetLeafMassRatio(growthProgress);
            float stemRatio = 1f - rootRatio - leafRatio;

            // Apply growth rate modifiers
            float rootGain = dailyBiomassGain * rootRatio * _rootDevelopmentRate;
            float leafGain = dailyBiomassGain * leafRatio * _leafGrowthRate;
            float stemGain = dailyBiomassGain * stemRatio * _stemGrowthRate;

            // Update masses
            _rootMass += rootGain;
            _leafMass += leafGain;
            _stemMass += stemGain;
            _totalBiomass = _rootMass + _leafMass + _stemMass;
        }

        public void UpdateGrowthRates()
        {
            // Auto-adjust growth rates based on current biomass distribution
            float totalRatio = _rootMass + _leafMass + _stemMass;
            if (totalRatio > 0f)
            {
                float rootRatio = _rootMass / totalRatio;
                float leafRatio = _leafMass / totalRatio;

                // Roots need more development if under-represented
                if (rootRatio < 0.3f)
                    _rootDevelopmentRate = Mathf.Min(_rootDevelopmentRate * 1.1f, 2f);
                
                // Leaves need more development if under-represented
                if (leafRatio < 0.4f)
                    _leafGrowthRate = Mathf.Min(_leafGrowthRate * 1.1f, 2f);
            }
        }

        public float CalculateLeafArea()
        {
            // Leaf area is proportional to leaf mass
            // Assume 1g leaf mass = 50 cm² leaf area (typical for cannabis)
            float specificLeafArea = 50f; // cm² per gram
            return _leafMass * specificLeafArea;
        }

        private float GetRootMassRatio(float progress)
        {
            // Early growth (0-0.3): High root allocation (50%)
            // Mid growth (0.3-0.7): Moderate root allocation (30%)
            // Late growth (0.7-1.0): Low root allocation (15%)
            if (progress < 0.3f)
                return 0.5f;
            if (progress < 0.7f)
                return Mathf.Lerp(0.5f, 0.3f, (progress - 0.3f) / 0.4f);
            return Mathf.Lerp(0.3f, 0.15f, (progress - 0.7f) / 0.3f);
        }

        private float GetLeafMassRatio(float progress)
        {
            // Early growth (0-0.3): Moderate leaf allocation (35%)
            // Mid growth (0.3-0.7): High leaf allocation (50%)
            // Late growth (0.7-1.0): Moderate leaf allocation (40%)
            if (progress < 0.3f)
                return 0.35f;
            if (progress < 0.7f)
                return Mathf.Lerp(0.35f, 0.5f, (progress - 0.3f) / 0.4f);
            return Mathf.Lerp(0.5f, 0.4f, (progress - 0.7f) / 0.3f);
        }

        public float GetLeafAreaStageFactor(float progress)
        {
            // Leaf area increases with growth progress
            // Peak leaf area at flowering stage (0.6-0.8)
            if (progress < 0.3f)
                return Mathf.Lerp(0.2f, 0.6f, progress / 0.3f);
            if (progress < 0.8f)
                return Mathf.Lerp(0.6f, 1f, (progress - 0.3f) / 0.5f);
            return Mathf.Lerp(1f, 0.9f, (progress - 0.8f) / 0.2f);
        }
    }
}

