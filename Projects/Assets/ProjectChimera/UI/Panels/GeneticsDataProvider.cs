using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Provides strain and trait data for genetics operations.
    /// Extracted from GeneticsContextMenu.cs to reduce file size and improve separation of concerns.
    /// </summary>
    public class GeneticsDataProvider
    {
        // Available Strains and Operations
        private readonly Dictionary<string, StrainInfo> _availableStrains = new Dictionary<string, StrainInfo>();
        private readonly Dictionary<string, GeneticTrait> _availableTraits = new Dictionary<string, GeneticTrait>();
        
        public GeneticsDataProvider()
        {
            InitializeAvailableStrains();
            InitializeAvailableTraits();
        }
        
        /// <summary>
        /// Initializes available cannabis strains for genetics work
        /// </summary>
        private void InitializeAvailableStrains()
        {
            var strains = new Dictionary<string, StrainInfo>
            {
                ["og-kush"] = new StrainInfo
                {
                    Id = "og-kush",
                    DisplayName = "OG Kush",
                    Type = "Hybrid",
                    THCRange = "19-24%",
                    CBDRange = "0.1-0.3%",
                    FloweringTime = 63, // days
                    Yield = "Medium-High",
                    Stability = 85,
                    IsAvailable = true,
                    ParentStrains = new List<string> { "hindu-kush", "chemdawg" }
                },
                ["northern-lights"] = new StrainInfo
                {
                    Id = "northern-lights",
                    DisplayName = "Northern Lights",
                    Type = "Indica",
                    THCRange = "16-21%",
                    CBDRange = "0.1-1.0%",
                    FloweringTime = 56,
                    Yield = "High",
                    Stability = 95,
                    IsAvailable = true,
                    ParentStrains = new List<string> { "afghani", "thai" }
                },
                ["jack-herer"] = new StrainInfo
                {
                    Id = "jack-herer",
                    DisplayName = "Jack Herer",
                    Type = "Sativa",
                    THCRange = "18-23%",
                    CBDRange = "0.03-0.2%",
                    FloweringTime = 70,
                    Yield = "Medium",
                    Stability = 75,
                    IsAvailable = true,
                    ParentStrains = new List<string> { "haze", "northern-lights", "shiva-skunk" }
                },
                ["white-widow"] = new StrainInfo
                {
                    Id = "white-widow",
                    DisplayName = "White Widow",
                    Type = "Hybrid",
                    THCRange = "20-25%",
                    CBDRange = "0.1-0.2%",
                    FloweringTime = 60,
                    Yield = "High",
                    Stability = 90,
                    IsAvailable = false, // Requires unlocking
                    ParentStrains = new List<string> { "brazilian", "south-indian-indica" }
                },
                ["purple-haze"] = new StrainInfo
                {
                    Id = "purple-haze",
                    DisplayName = "Purple Haze",
                    Type = "Sativa",
                    THCRange = "16-20%",
                    CBDRange = "0.1-0.5%",
                    FloweringTime = 65,
                    Yield = "Medium",
                    Stability = 80,
                    IsAvailable = true,
                    ParentStrains = new List<string> { "haze", "purple-thai" }
                },
                ["blue-dream"] = new StrainInfo
                {
                    Id = "blue-dream",
                    DisplayName = "Blue Dream",
                    Type = "Hybrid",
                    THCRange = "17-24%",
                    CBDRange = "0.1-0.2%",
                    FloweringTime = 67,
                    Yield = "High",
                    Stability = 88,
                    IsAvailable = true,
                    ParentStrains = new List<string> { "blueberry", "haze" }
                }
            };
            
            foreach (var strain in strains)
            {
                _availableStrains[strain.Key] = strain.Value;
            }
        }
        
        /// <summary>
        /// Initializes available genetic traits for analysis
        /// </summary>
        private void InitializeAvailableTraits()
        {
            var traits = new Dictionary<string, GeneticTrait>
            {
                ["thc-production"] = new GeneticTrait
                {
                    Id = "thc-production",
                    DisplayName = "THC Production",
                    Description = "Controls cannabinoid THC synthesis levels",
                    TraitType = "Chemical",
                    Heritability = 0.75f,
                    IsAnalyzable = true
                },
                ["cbd-production"] = new GeneticTrait
                {
                    Id = "cbd-production",
                    DisplayName = "CBD Production",
                    Description = "Controls cannabinoid CBD synthesis levels",
                    TraitType = "Chemical",
                    Heritability = 0.80f,
                    IsAnalyzable = true
                },
                ["flowering-time"] = new GeneticTrait
                {
                    Id = "flowering-time",
                    DisplayName = "Flowering Time",
                    Description = "Duration from flower initiation to harvest",
                    TraitType = "Phenological",
                    Heritability = 0.85f,
                    IsAnalyzable = true
                },
                ["yield-potential"] = new GeneticTrait
                {
                    Id = "yield-potential",
                    DisplayName = "Yield Potential",
                    Description = "Maximum biomass production capacity",
                    TraitType = "Growth",
                    Heritability = 0.65f,
                    IsAnalyzable = true
                },
                ["disease-resistance"] = new GeneticTrait
                {
                    Id = "disease-resistance",
                    DisplayName = "Disease Resistance",
                    Description = "Natural resistance to common pathogens",
                    TraitType = "Defense",
                    Heritability = 0.70f,
                    IsAnalyzable = true
                },
                ["terpene-profile"] = new GeneticTrait
                {
                    Id = "terpene-profile",
                    DisplayName = "Terpene Profile",
                    Description = "Aromatic compound expression patterns",
                    TraitType = "Chemical",
                    Heritability = 0.72f,
                    IsAnalyzable = true
                },
                ["stress-tolerance"] = new GeneticTrait
                {
                    Id = "stress-tolerance",
                    DisplayName = "Stress Tolerance",
                    Description = "Ability to withstand environmental stress",
                    TraitType = "Defense",
                    Heritability = 0.68f,
                    IsAnalyzable = true
                }
            };
            
            foreach (var trait in traits)
            {
                _availableTraits[trait.Key] = trait.Value;
            }
        }
        
        /// <summary>
        /// Gets all available strains
        /// </summary>
        public Dictionary<string, StrainInfo> GetAvailableStrains()
        {
            return new Dictionary<string, StrainInfo>(_availableStrains);
        }
        
        /// <summary>
        /// Gets strains filtered by availability
        /// </summary>
        public IEnumerable<StrainInfo> GetAvailableStrains(bool availableOnly = true)
        {
            return _availableStrains.Values.Where(s => !availableOnly || s.IsAvailable);
        }
        
        /// <summary>
        /// Gets strain by ID
        /// </summary>
        public StrainInfo GetStrain(string strainId)
        {
            return _availableStrains.GetValueOrDefault(strainId);
        }
        
        /// <summary>
        /// Gets strain by display name
        /// </summary>
        public StrainInfo GetStrainByDisplayName(string displayName)
        {
            return _availableStrains.Values.FirstOrDefault(s => s.DisplayName == displayName);
        }
        
        /// <summary>
        /// Gets all available genetic traits
        /// </summary>
        public Dictionary<string, GeneticTrait> GetAvailableTraits()
        {
            return new Dictionary<string, GeneticTrait>(_availableTraits);
        }
        
        /// <summary>
        /// Gets traits filtered by analyzability
        /// </summary>
        public IEnumerable<GeneticTrait> GetAvailableTraits(bool analyzableOnly = true)
        {
            return _availableTraits.Values.Where(t => !analyzableOnly || t.IsAnalyzable);
        }
        
        /// <summary>
        /// Gets trait by ID
        /// </summary>
        public GeneticTrait GetTrait(string traitId)
        {
            return _availableTraits.GetValueOrDefault(traitId);
        }
        
        /// <summary>
        /// Validates if strain is available for use
        /// </summary>
        public bool IsStrainAvailable(string strainId)
        {
            var strain = GetStrain(strainId);
            return strain != null && strain.IsAvailable;
        }
        
        /// <summary>
        /// Validates if trait is available for analysis
        /// </summary>
        public bool IsTraitAnalyzable(string traitId)
        {
            var trait = GetTrait(traitId);
            return trait != null && trait.IsAnalyzable;
        }
        
        /// <summary>
        /// Gets strains by type (Indica, Sativa, Hybrid)
        /// </summary>
        public IEnumerable<StrainInfo> GetStrainsByType(string type, bool availableOnly = true)
        {
            return GetAvailableStrains(availableOnly).Where(s => s.Type == type);
        }
        
        /// <summary>
        /// Gets traits by type (Chemical, Growth, Defense, etc.)
        /// </summary>
        public IEnumerable<GeneticTrait> GetTraitsByType(string type, bool analyzableOnly = true)
        {
            return GetAvailableTraits(analyzableOnly).Where(t => t.TraitType == type);
        }
    }
    
    /// <summary>
    /// Information about a cannabis strain
    /// </summary>
    public class StrainInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; } // Indica, Sativa, Hybrid
        public string THCRange { get; set; }
        public string CBDRange { get; set; }
        public int FloweringTime { get; set; } // Days
        public string Yield { get; set; }
        public int Stability { get; set; } // 0-100 scale
        public bool IsAvailable { get; set; }
        public List<string> ParentStrains { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Information about a genetic trait
    /// </summary>
    public class GeneticTrait
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string TraitType { get; set; } // Chemical, Growth, Defense, etc.
        public float Heritability { get; set; } // 0.0-1.0 scale
        public bool IsAnalyzable { get; set; }
    }
}