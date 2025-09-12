using UnityEngine;
using System.Collections.Generic;
using System;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// SIMPLE: Basic cannabis genotype aligned with Project Chimera's genetics vision.
    /// Focuses on essential strain traits for breeding and cultivation mechanics.
    /// </summary>
    [Serializable]
    public class CannabisGenotype
    {
        [Header("Basic Identity")]
        public string GenotypeId;
        public string StrainName;
        public string ParentStrain; // Added for breeding compatibility
        public string Description;
        public bool IsCustomStrain = false;

        [Header("Basic Traits")]
        public float YieldPotential = 100f; // grams per plant
        public float PotencyPotential = 15f; // THC percentage
        public int FloweringTime = 60; // days
        public float MaxHeight = 2.0f; // meters
        public PlantType PlantType = PlantType.Hybrid;

        /// <summary>
        /// Get a summary of the strain's key traits
        /// </summary>
        public string GetStrainSummary()
        {
            return $"{StrainName}: {YieldPotential}g yield, {PotencyPotential}% THC, {FloweringTime} day flower, {PlantType}";
        }

        /// <summary>
        /// Check if this is a high-yielding strain
        /// </summary>
        public bool IsHighYield()
        {
            return YieldPotential >= 150f;
        }

        /// <summary>
        /// Check if this is a potent strain
        /// </summary>
        public bool IsHighPotency()
        {
            return PotencyPotential >= 20f;
        }

        /// <summary>
        /// Calculate strain quality score
        /// </summary>
        public float GetQualityScore()
        {
            return (YieldPotential / 200f + PotencyPotential / 25f) / 2f;
        }
    }

    /// <summary>
    /// Basic plant types for cannabis strains
    /// </summary>
    public enum PlantType
    {
        Indica,
        Sativa,
        Hybrid
    }
}
