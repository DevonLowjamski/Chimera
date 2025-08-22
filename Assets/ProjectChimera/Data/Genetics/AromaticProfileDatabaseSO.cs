using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Genetics
{
    // Local type definitions to replace deleted types
    public enum FlavorProfileType
    {
        Sweet, Citrus, Earthy, Pine, Floral, Spicy, Fruity, Herbal, Woody, Diesel
    }
    
    public enum TerpeneRole
    {
        Primary, Secondary, Accent, Modifier
    }
    
    public enum TerpeneCategory 
    {
        Monoterpenes, Sesquiterpenes, Phenolic, Alcohol, Ketone, Ester
    }

    /// <summary>
    /// Aromatic Profile Database - Collection of aromatic profiles for blending and analysis
    /// Contains flavor profiles, aromatic combinations, and quality standards
    /// Part of Enhanced Scientific Gaming System v2.0
    /// </summary>
    [CreateAssetMenu(fileName = "New Aromatic Profile Database", menuName = "Project Chimera/Gaming/Aromatic Profile Database")]
    public class AromaticProfileDatabaseSO : ChimeraDataSO
    {
        [Header("Aromatic Profiles")]
        public List<AromaticProfile> Profiles = new List<AromaticProfile>();
        
        [Header("Profile Categories")]
        public List<ProfileCategoryData> Categories = new List<ProfileCategoryData>();
        
        [Header("Quality Standards")]
        public List<QualityStandard> QualityStandards = new List<QualityStandard>();
        
        #region Runtime Methods
        
        public AromaticProfile GetProfile(string profileID)
        {
            return Profiles.Find(p => p.ProfileID == profileID);
        }
        
        public List<AromaticProfile> GetProfilesByCategory(FlavorProfileType category)
        {
            return Profiles.FindAll(p => p.ProfileType == category);
        }
        
        public QualityStandard GetQualityStandard(QualityTier tier)
        {
            return QualityStandards.Find(q => q.QualityTier == tier);
        }
        
        #endregion
    }
    
    [System.Serializable]
    public class AromaticProfile
    {
        public string ProfileID;
        public string ProfileName;
        public FlavorProfileType ProfileType;
        public List<TerpeneProfileComponent> TerpeneComponents = new List<TerpeneProfileComponent>();
        public float ComplexityScore;
        public float QualityRating;
        public List<string> AromaticDescriptors = new List<string>();
        public Sprite ProfileIcon;
        public string Description;
    }
    
    [System.Serializable]
    public class TerpeneProfileComponent
    {
        public string TerpeneName;
        public float Concentration;
        public TerpeneRole Role;
        public float ContributionWeight;
        public bool IsSignatureTerpene;
    }
    
    [System.Serializable]
    public class ProfileCategoryData
    {
        public string CategoryID;
        public string CategoryName;
        public FlavorProfileType ProfileType;
        public Color CategoryColor = Color.white;
        public List<string> TypicalDescriptors = new List<string>();
        public Sprite CategoryIcon;
        public string Description;
    }
    
    [System.Serializable]
    public class QualityStandard
    {
        public string StandardName;
        public QualityTier QualityTier;
        public float MinimumComplexity;
        public float MinimumBalance;
        public List<QualityCriterion> Criteria = new List<QualityCriterion>();
        public string Description;
    }
    
    [System.Serializable]
    public class QualityCriterion
    {
        public string CriterionName;
        public QualityCriterionType CriterionType;
        public float MinimumValue;
        public float MaximumValue;
        public float Weight;
        public string Description;
    }
    
    public enum QualityTier
    {
        Basic,
        Good,
        Excellent,
        Premium,
        Exceptional,
        Legendary
    }
    
    public enum QualityCriterionType
    {
        Complexity,
        Balance,
        Intensity,
        Purity,
        Harmony,
        Innovation,
        Authenticity,
        Appeal
    }
}