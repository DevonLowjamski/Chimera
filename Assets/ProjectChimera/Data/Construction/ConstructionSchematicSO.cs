using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Data.Construction
{
    /// <summary>
    /// Construction Schematic ScriptableObject
    /// Defines blueprints for constructible items and structures
    /// </summary>
    [CreateAssetMenu(fileName = "Construction Schematic", menuName = "Project Chimera/Construction/Construction Schematic")]
    public class ConstructionSchematicSO : ScriptableObject
    {
        [Header("Schematic Identity")]
        [SerializeField] private string _schematicId;
        [SerializeField] private string _schematicName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Construction Properties")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Vector3Int _gridSize = Vector3Int.one;
        [SerializeField] private float _constructionCost = 100f;
        [SerializeField] private float _constructionTime = 5f;

        [Header("Requirements")]
        [SerializeField] private List<ResourceRequirement> _resourceRequirements = new List<ResourceRequirement>();
        [SerializeField] private int _requiredPlayerLevel = 1;
        [SerializeField] private string[] _requiredSkills = new string[0];

        [Header("Category")]
        [SerializeField] private ConstructionCategory _category = ConstructionCategory.Structure;
        [SerializeField] private string _subcategory = "Basic";

        // Public properties
        public string SchematicId => _schematicId;
        public string SchematicName => _schematicName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public GameObject Prefab => _prefab;
        public Vector3Int GridSize => _gridSize;
        public float ConstructionCost => _constructionCost;
        public float ConstructionTime => _constructionTime;
        public List<ResourceRequirement> ResourceRequirements => _resourceRequirements;
        public int RequiredPlayerLevel => _requiredPlayerLevel;
        public string[] RequiredSkills => _requiredSkills;
        public ConstructionCategory Category => _category;
        public string Subcategory => _subcategory;

        /// <summary>
        /// Check if player meets requirements to build this schematic
        /// </summary>
        public bool CanPlayerBuild(int playerLevel, string[] playerSkills)
        {
            if (playerLevel < _requiredPlayerLevel) return false;

            foreach (var requiredSkill in _requiredSkills)
            {
                bool hasSkill = false;
                foreach (var playerSkill in playerSkills)
                {
                    if (playerSkill == requiredSkill)
                    {
                        hasSkill = true;
                        break;
                    }
                }
                if (!hasSkill) return false;
            }

            return true;
        }

        /// <summary>
        /// Get total resource cost for construction
        /// </summary>
        public float GetTotalResourceCost()
        {
            float totalCost = _constructionCost;
            foreach (var resource in _resourceRequirements)
            {
                totalCost += resource.Amount * resource.CostPerUnit;
            }
            return totalCost;
        }
    }

    [System.Serializable]
    public struct ResourceRequirement
    {
        public string ResourceType;
        public float Amount;
        public float CostPerUnit;
        public bool IsOptional;
    }

}