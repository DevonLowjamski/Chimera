using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Genetics
{
    // Local type definitions to replace deleted types
    public enum GeneticPuzzleType
    {
        Inheritance, Crossing, Selection, Breeding, Analysis
    }

    /// <summary>
    /// Genetic Visualization Configuration - Visual settings for genetic gaming interface
    /// Defines genetic puzzle visuals, inheritance displays, and breeding interface elements
    /// Part of Enhanced Scientific Gaming System v2.0
    /// </summary>
    [CreateAssetMenu(fileName = "New Genetic Visualization Config", menuName = "Project Chimera/Gaming/Genetic Visualization Config")]
    public class GeneticVisualizationConfigSO : ChimeraDataSO
    {
        [Header("Genetic Display Settings")]
        public Color DominantAlleleColor = Color.red;
        public Color RecessiveAlleleColor = Color.blue;
        public Color HeterozygousColor = Color.purple;
        public Color HomozygousColor = Color.green;
        
        [Header("Puzzle Interface")]
        public List<GeneticPuzzleVisualConfig> PuzzleConfigurations = new List<GeneticPuzzleVisualConfig>();
        public List<InheritancePatternVisual> InheritancePatterns = new List<InheritancePatternVisual>();
        
        [Header("Animation Settings")]
        public float AlleleAnimationSpeed = 1.0f;
        public bool EnableGeneticAnimations = true;
        public AnimationCurve GeneticTransitionCurve;
        
        #region Runtime Methods
        
        public GeneticPuzzleVisualConfig GetPuzzleVisual(GeneticPuzzleType puzzleType)
        {
            return PuzzleConfigurations.Find(p => p.PuzzleType == puzzleType);
        }
        
        public InheritancePatternVisual GetInheritancePattern(InheritanceType inheritanceType)
        {
            return InheritancePatterns.Find(i => i.InheritanceType == inheritanceType);
        }
        
        #endregion
    }
    
    [System.Serializable]
    public class GeneticPuzzleVisualConfig
    {
        public string ConfigName;
        public GeneticPuzzleType PuzzleType;
        public Sprite PuzzleIcon;
        public Color PuzzleColor = Color.white;
        public List<GeneticVisualElement> VisualElements = new List<GeneticVisualElement>();
        public string Description;
    }
    
    [System.Serializable]
    public class InheritancePatternVisual
    {
        public string PatternName;
        public InheritanceType InheritanceType;
        public Sprite PatternIcon;
        public Color PatternColor = Color.white;
        public AnimationClip PatternAnimation;
        public string Description;
    }
    
    [System.Serializable]
    public class GeneticVisualElement
    {
        public string ElementName;
        public GeneticVisualElementType ElementType;
        public Vector2 Position;
        public Vector2 Size;
        public Color ElementColor = Color.white;
        public Sprite ElementSprite;
    }
    
    public enum InheritanceType
    {
        Mendelian,
        Polygenic,
        Epistatic,
        Linkage,
        SexLinked,
        Mitochondrial
    }
    
    public enum GeneticVisualElementType
    {
        Gene,
        Allele,
        Chromosome,
        Trait,
        Connection,
        Label,
        Animation
    }
}