using UnityEngine;
using System.Collections.Generic;
using System;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// General plant genotype data structure extending CannabisGenotype.
    /// </summary>
    [Serializable]
    public class PlantGenotype : CannabisGenotype
    {
        [Header("General Plant Properties")]
        public string PlantSpecies = "Cannabis";
        public string Cultivar;
        public PlantLifecycleType Type = PlantLifecycleType.Annual;
        
        [Header("Additional Plant Traits")]
        public float RootSystemDepth = 1.0f;
        public float LeafThickness = 1.0f;
        public float StemStrength = 1.0f;
        public float PhotoperiodSensitivity = 1.0f;
        
        [Header("Advanced Genetics")]
        public int ChromosomeNumber = 20; // Cannabis has 2n=20
        public float GenomeSize = 843.0f; // Mb
        public List<QTL> QuantitativeTraitLoci = new List<QTL>();
        
        // Missing properties for Systems layer compatibility
        public new float OverallFitness { get; set; } = 1.0f; // Intentional shadow to make settable in derived type
        
        public int AlleleCount => QuantitativeTraitLoci?.Count ?? 0;
        public List<object> Genes { get; set; } = new List<object>();
        public string GenotypeID { get; set; } // Settable property for PlantInstanceSO
        public object StrainOrigin { get; set; } // Settable property for PlantInstanceSO  
        public int Generation { get; set; } // Settable property for PlantInstanceSO
        public bool IsFounder { get; set; } // Missing property for PlantInstanceSO
        public System.DateTime CreationDate { get; set; } // Settable property for PlantInstanceSO
        public float InbreedingCoefficient { get; set; } = 0f; // Missing property for InheritanceCalculator
        public List<string> ParentIDs { get; set; } = new List<string>(); // Missing property for InheritanceCalculator
        public Dictionary<string, object> Genotype { get; set; } = new Dictionary<string, object>(); // Missing property for PlantInstanceSO
        public new List<object> Mutations { get; set; } = new List<object>(); // Intentional shadow
        
        public PlantGenotype() : base()
        {
            PlantSpecies = "Cannabis";
            InitializePlantSpecificTraits();
        }
        
        public PlantGenotype(string species, string cultivar) : base()
        {
            PlantSpecies = species;
            Cultivar = cultivar;
            InitializePlantSpecificTraits();
        }
        
        /// <summary>
        /// Adds a trait with the specified name and value to the genotype
        /// </summary>
        /// <param name="traitName">Name of the trait</param>
        /// <param name="value">Value of the trait</param>
        private void AddTrait(string traitName, float value)
        {
            if (Genotype == null)
                Genotype = new Dictionary<string, object>();
                
            Genotype[traitName] = value;
        }
        
        private void InitializePlantSpecificTraits()
        {
            // Add plant-specific traits
            AddTrait("root_depth", UnityEngine.Random.Range(0.8f, 1.5f));
            AddTrait("leaf_thickness", UnityEngine.Random.Range(0.9f, 1.2f));
            AddTrait("stem_strength", UnityEngine.Random.Range(0.7f, 1.4f));
            AddTrait("photoperiod_sensitivity", UnityEngine.Random.Range(0.6f, 1.3f));
        }
    }
    
    /// <summary>
    /// Quantitative Trait Locus data structure
    /// </summary>
    [Serializable]
    public class QTL
    {
        public string QTLId;
        public string TraitName;
        public string ChromosomeLocation;
        public float EffectSize;
        public float HeritabilityContribution;
        public List<string> LinkedMarkers = new List<string>();
    }
    
    public enum PlantLifecycleType
    {
        Annual,
        Biennial,
        Perennial,
        Succulent
    }
    
}