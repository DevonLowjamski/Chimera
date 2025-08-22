using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Simple data structure for trait expression results.
    /// Data-only version for use in ScriptableObjects.
    /// </summary>
    [System.Serializable]
    public class TraitExpressionResult
    {
        [Header("Basic Information")]
        public string GenotypeID;
        public string PlantId;
        public float OverallFitness = 1.0f;
        public float EnvironmentalStress = 0.0f;
        
        [Header("Trait Values")]
        public List<TraitValue> TraitValues = new List<TraitValue>();
        
        [Header("Expression Values")]
        public float EnvironmentalInfluence = 0.5f;
        public System.DateTime CalculationTime;
        
        public void Reset()
        {
            GenotypeID = "";
            OverallFitness = 1.0f;
            EnvironmentalStress = 0.0f;
            TraitValues.Clear();
        }
        
        public float GetTraitValue(TraitType trait)
        {
            var traitValue = TraitValues.Find(tv => tv.Trait == trait);
            return traitValue?.Value ?? 0f;
        }
        
        public void SetTraitValue(TraitType trait, float value)
        {
            var existing = TraitValues.Find(tv => tv.Trait == trait);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                TraitValues.Add(new TraitValue { Trait = trait, Value = value });
            }
        }
        
        // PC014-FIX-24: Added missing properties for GeneticsManager compatibility
        
        /// <summary>
        /// Gets/sets the height expression value for compatibility.
        /// </summary>
        public float HeightExpression
        {
            get => GetTraitValue(TraitType.PlantHeight);
            set => SetTraitValue(TraitType.PlantHeight, value);
        }
        
        /// <summary>
        /// Gets/sets the yield expression value for compatibility.
        /// </summary>
        public float YieldExpression
        {
            get => GetTraitValue(TraitType.Yield);
            set => SetTraitValue(TraitType.Yield, value);
        }
        
        /// <summary>
        /// Gets/sets the THC expression value for compatibility.
        /// </summary>
        public float THCExpression
        {
            get => GetTraitValue(TraitType.THCContent);
            set => SetTraitValue(TraitType.THCContent, value);
        }
        
        /// <summary>
        /// Gets/sets the CBD expression value for compatibility.
        /// </summary>
        public float CBDExpression
        {
            get => GetTraitValue(TraitType.CBDContent);
            set => SetTraitValue(TraitType.CBDContent, value);
        }
        
        /// <summary>
        /// Gets/sets the terpene expression value for compatibility.
        /// </summary>
        public float TerpeneExpression
        {
            get => GetTraitValue(TraitType.TerpeneProduction);
            set => SetTraitValue(TraitType.TerpeneProduction, value);
        }
        
        // Backward compatibility methods
        public float HeightExpressionMethod() => HeightExpression;
        public float YieldExpressionMethod() => YieldExpression;
        public float THCExpressionMethod() => THCExpression;
        public float CBDExpressionMethod() => CBDExpression;
        public float TerpeneExpressionMethod() => TerpeneExpression;
    }
    
    [System.Serializable]
    public class TraitValue
    {
        public TraitType Trait;
        public float Value;
    }
}