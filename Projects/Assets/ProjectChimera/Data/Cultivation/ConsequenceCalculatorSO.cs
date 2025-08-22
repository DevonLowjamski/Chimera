using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Calculates consequences and effects of player choices
    /// </summary>
    [CreateAssetMenu(fileName = "ConsequenceCalculator", menuName = "Project Chimera/Cultivation/Consequence Calculator")]
    public class ConsequenceCalculatorSO : ChimeraDataSO
    {
        [Header("Consequence Rules")]
        [SerializeField] private List<ConsequenceRule> _consequenceRules = new List<ConsequenceRule>();
        
        public List<ConsequenceRule> ConsequenceRules => _consequenceRules;
        
        public float CalculateConsequence(string actionId, Dictionary<string, object> context)
        {
            float totalConsequence = 0f;
            
            foreach (var rule in _consequenceRules)
            {
                if (rule.AppliesTo(actionId, context))
                {
                    totalConsequence += rule.CalculateEffect(context);
                }
            }
            
            return totalConsequence;
        }
    }
    
    /// <summary>
    /// Rule for calculating action consequences
    /// </summary>
    [System.Serializable]
    public class ConsequenceRule
    {
        public string RuleId;
        public string RuleName;
        public string TargetAction;
        public float BaseEffect = 1f;
        public Dictionary<string, float> Modifiers = new Dictionary<string, float>();
        
        public ConsequenceRule()
        {
            RuleId = System.Guid.NewGuid().ToString();
        }
        
        public bool AppliesTo(string actionId, Dictionary<string, object> context)
        {
            return TargetAction == actionId || TargetAction == "*";
        }
        
        public float CalculateEffect(Dictionary<string, object> context)
        {
            float effect = BaseEffect;
            
            foreach (var modifier in Modifiers)
            {
                if (context.ContainsKey(modifier.Key))
                {
                    effect *= modifier.Value;
                }
            }
            
            return effect;
        }
    }
}