using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// System for managing player expression and creative choices
    /// </summary>
    [CreateAssetMenu(fileName = "ExpressionSystem", menuName = "Project Chimera/Cultivation/Expression System")]
    public class ExpressionSystemSO : ChimeraDataSO
    {
        [Header("Expression Configuration")]
        [SerializeField] private List<ExpressionCategory> _expressionCategories = new List<ExpressionCategory>();
        
        public List<ExpressionCategory> ExpressionCategories => _expressionCategories;
        
        public ExpressionCategory GetCategory(string categoryId)
        {
            return _expressionCategories.Find(c => c.CategoryId == categoryId);
        }
    }
    
    /// <summary>
    /// Category of player expression options
    /// </summary>
    [System.Serializable]
    public class ExpressionCategory
    {
        public string CategoryId;
        public string CategoryName;
        public string Description;
        public List<ExpressionOption> Options = new List<ExpressionOption>();
        
        public ExpressionCategory()
        {
            CategoryId = System.Guid.NewGuid().ToString();
        }
    }
    
    /// <summary>
    /// Individual expression option for players
    /// </summary>
    [System.Serializable]
    public class ExpressionOption
    {
        public string OptionId;
        public string OptionName;
        public string Description;
        public bool IsUnlocked = false;
        public float CostModifier = 1f;
        public Dictionary<string, object> Effects = new Dictionary<string, object>();
        
        public ExpressionOption()
        {
            OptionId = System.Guid.NewGuid().ToString();
        }
    }
}