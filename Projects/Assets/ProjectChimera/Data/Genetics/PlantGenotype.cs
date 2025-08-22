using UnityEngine;
using System.Collections.Generic;
using System;

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
        public PlantType Type = PlantType.Annual;
        
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
        public string GenotypeID { get; set; } // Settable property for PlantInstanceSO
        public PlantStrainSO StrainOrigin { get; set; } // Settable property for PlantInstanceSO  
        public int Generation { get; set; } // Settable property for PlantInstanceSO
        public bool IsFounder { get; set; } // Missing property for PlantInstanceSO
        public System.DateTime CreationDate { get; set; } // Settable property for PlantInstanceSO
        public Dictionary<string, AlleleCouple> Genotype { get; set; } = new Dictionary<string, AlleleCouple>(); // Missing property for PlantInstanceSO
        public new List<MutationRecord> Mutations { get; set; } = new List<MutationRecord>(); // Intentional shadow
        
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
    
    public enum PlantType
    {
        Annual,
        Biennial,
        Perennial,
        Succulent
    }
    
}