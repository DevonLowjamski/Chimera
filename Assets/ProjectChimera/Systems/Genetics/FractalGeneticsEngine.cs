using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Genetics
{
    /// <summary>
    /// BASIC: Simple genetics calculations for Project Chimera.
    /// Focuses on essential genetic operations without complex fractal mathematics.
    /// </summary>
    public class FractalGeneticsEngine : MonoBehaviour
    {
        [Header("Basic Genetics Settings")]
        [SerializeField] private bool _enableBasicGenetics = true;
        [SerializeField] private float _mutationRate = 0.01f; // 1% mutation rate
        [SerializeField] private bool _enableLogging = true;

        // Basic genetics tracking
        private readonly Dictionary<string, GeneticData> _geneticDatabase = new Dictionary<string, GeneticData>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for genetic operations
        /// </summary>
        public event System.Action<string, GeneticData> OnGeneticDataCreated;
        public event System.Action<string, string> OnBreedingCompleted;

        /// <summary>
        /// Initialize basic genetics system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Create some basic genetic data
            CreateBasicGeneticData();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[FractalGeneticsEngine] Initialized successfully");
            }
        }

        /// <summary>
        /// Create genetic data for a strain
        /// </summary>
        public void CreateGeneticData(string strainId, float thcContent, float cbdContent, float yield, string parent1 = "", string parent2 = "")
        {
            if (!_enableBasicGenetics || !_isInitialized) return;

            var geneticData = new GeneticData
            {
                StrainId = strainId,
                ThcContent = thcContent,
                CbdContent = cbdContent,
                YieldPotential = yield,
                ParentStrain1 = parent1,
                ParentStrain2 = parent2,
                Stability = CalculateStability(thcContent, cbdContent),
                CreatedTime = System.DateTime.Now
            };

            _geneticDatabase[strainId] = geneticData;
            OnGeneticDataCreated?.Invoke(strainId, geneticData);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[FractalGeneticsEngine] Created genetic data for {strainId}");
            }
        }

        /// <summary>
        /// Breed two strains
        /// </summary>
        public string BreedStrains(string parent1Id, string parent2Id, string offspringName)
        {
            if (!_enableBasicGenetics || !_isInitialized) return null;

            if (!_geneticDatabase.ContainsKey(parent1Id) || !_geneticDatabase.ContainsKey(parent2Id))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("[FractalGeneticsEngine] Parent strains not found");
                }
                return null;
            }

            var parent1 = _geneticDatabase[parent1Id];
            var parent2 = _geneticDatabase[parent2Id];

            // Simple breeding - average traits with some variation
            float avgThc = (parent1.ThcContent + parent2.ThcContent) / 2f;
            float avgCbd = (parent1.CbdContent + parent2.CbdContent) / 2f;
            float avgYield = (parent1.YieldPotential + parent2.YieldPotential) / 2f;

            // Add some genetic variation
            avgThc += Random.Range(-2f, 2f);
            avgCbd += Random.Range(-0.5f, 0.5f);
            avgYield += Random.Range(-50f, 50f);

            // Apply mutation rate
            if (Random.value < _mutationRate)
            {
                avgThc += Random.Range(-5f, 5f);
                avgCbd += Random.Range(-1f, 1f);
                avgYield += Random.Range(-100f, 100f);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("[FractalGeneticsEngine] Genetic mutation occurred!");
                }
            }

            // Clamp values to reasonable ranges
            avgThc = Mathf.Clamp(avgThc, 5f, 30f);
            avgCbd = Mathf.Clamp(avgCbd, 0f, 5f);
            avgYield = Mathf.Clamp(avgYield, 200f, 800f);

            string offspringId = offspringName + "_" + System.Guid.NewGuid().ToString().Substring(0, 8);

            CreateGeneticData(offspringId, avgThc, avgCbd, avgYield, parent1Id, parent2Id);
            OnBreedingCompleted?.Invoke(offspringId, offspringName);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[FractalGeneticsEngine] Bred {offspringName} from {parent1Id} and {parent2Id}");
            }

            return offspringId;
        }

        /// <summary>
        /// Get genetic data for a strain
        /// </summary>
        public GeneticData GetGeneticData(string strainId)
        {
            return _geneticDatabase.TryGetValue(strainId, out var data) ? data : null;
        }

        /// <summary>
        /// Get all genetic data
        /// </summary>
        public Dictionary<string, GeneticData> GetAllGeneticData()
        {
            return new Dictionary<string, GeneticData>(_geneticDatabase);
        }

        /// <summary>
        /// Calculate genetic stability
        /// </summary>
        public float CalculateStability(float thcContent, float cbdContent)
        {
            // Simple stability calculation - balanced THC:CBD ratios are more stable
            float ratio = thcContent / Mathf.Max(cbdContent, 0.1f);
            if (ratio >= 2f && ratio <= 10f) return 0.9f; // Good ratio
            if (ratio >= 1f && ratio <= 20f) return 0.7f; // Acceptable ratio
            return 0.5f; // Poor ratio
        }

        /// <summary>
        /// Get breeding recommendations
        /// </summary>
        public string GetBreedingRecommendation(string strainId)
        {
            var data = GetGeneticData(strainId);
            if (data == null) return "Strain not found";

            if (data.ThcContent > 20f)
            {
                return "High THC strain - good for potency breeding";
            }
            else if (data.CbdContent > data.ThcContent)
            {
                return "High CBD strain - good for medicinal breeding";
            }
            else if (data.YieldPotential > 500f)
            {
                return "High yield strain - good for commercial breeding";
            }
            else
            {
                return "Balanced strain - good for general breeding";
            }
        }

        /// <summary>
        /// Clear all genetic data
        /// </summary>
        public void ClearAllData()
        {
            _geneticDatabase.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[FractalGeneticsEngine] Cleared all genetic data");
            }
        }

        /// <summary>
        /// Get genetics statistics
        /// </summary>
        public GeneticsStats GetGeneticsStats()
        {
            int totalStrains = _geneticDatabase.Count;
            float avgThc = totalStrains > 0 ? _geneticDatabase.Values.Average(d => d.ThcContent) : 0f;
            float avgYield = totalStrains > 0 ? _geneticDatabase.Values.Average(d => d.YieldPotential) : 0f;
            int stableStrains = _geneticDatabase.Values.Count(d => d.Stability > 0.8f);

            return new GeneticsStats
            {
                TotalStrains = totalStrains,
                AverageThcContent = avgThc,
                AverageYield = avgYield,
                StableStrains = stableStrains,
                IsGeneticsEnabled = _enableBasicGenetics
            };
        }

        #region Private Methods

        private void CreateBasicGeneticData()
        {
            // Create some basic starter strains
            CreateGeneticData("Basic_Sativa", 18f, 0.5f, 450f);
            CreateGeneticData("Basic_Indica", 15f, 1.2f, 400f);
            CreateGeneticData("Basic_Hybrid", 17f, 0.8f, 425f);
        }

        #endregion
    }

    /// <summary>
    /// Basic genetic data structure
    /// </summary>
    [System.Serializable]
    public class GeneticData
    {
        public string StrainId;
        public float ThcContent;
        public float CbdContent;
        public float YieldPotential;
        public string ParentStrain1;
        public string ParentStrain2;
        public float Stability;
        public System.DateTime CreatedTime;
    }

    /// <summary>
    /// Genetics statistics
    /// </summary>
    [System.Serializable]
    public struct GeneticsStats
    {
        public int TotalStrains;
        public float AverageThcContent;
        public float AverageYield;
        public int StableStrains;
        public bool IsGeneticsEnabled;
    }
}
