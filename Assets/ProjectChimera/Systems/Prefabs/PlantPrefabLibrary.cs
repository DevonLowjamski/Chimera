using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Minimal plant prefab library - simplified to avoid compilation issues
    /// Will be expanded once PlantGrowthStage enum conflicts are resolved
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant Prefab Library", menuName = "Project Chimera/Prefabs/Plant Library")]
    public class PlantPrefabLibrarySO : ScriptableObject
    {
        [Header("Plant Prefab Configuration")]
        [SerializeField] private List<GameObject> _plantPrefabs = new List<GameObject>();

        [Header("Basic Settings")]
        [SerializeField] private bool _enableRandomVariations = true;
        [SerializeField] private int _maxVariationsPerStrain = 5;

        public List<GameObject> PlantPrefabs => _plantPrefabs;
        public bool EnableRandomVariations => _enableRandomVariations;
        public int MaxVariationsPerStrain => _maxVariationsPerStrain;

        public void InitializeDefaults()
        {
            ChimeraLogger.LogInfo("Prefabs", "PlantPrefabLibrary defaults initialized");
        }

        public GameObject GetRandomPlantPrefab()
        {
            if (_plantPrefabs.Count == 0)
                return null;

            int randomIndex = Random.Range(0, _plantPrefabs.Count);
            return _plantPrefabs[randomIndex];
        }

        public int GetTotalPrefabCount()
        {
            return _plantPrefabs.Count;
        }

        private void OnValidate()
        {
            // Basic validation without complex enum dependencies
            if (_maxVariationsPerStrain < 1)
                _maxVariationsPerStrain = 1;
        }
    }
}
