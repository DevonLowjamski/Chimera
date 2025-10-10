using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation.PlantWork
{
    /// <summary>
    /// Plant Work system - pruning and training techniques for cultivation optimization.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Master cultivation techniques - shape plants for maximum yield!"
    ///
    /// **Player Experience**:
    /// - Pruning: Topping, FIMming, Lollipopping, Defoliation
    /// - Training: LST (Low Stress), HST (High Stress), ScrOG, Supercropping
    /// - Visual feedback on plant structure changes
    /// - Timing critical (too early/late reduces effectiveness)
    /// - Skill progression unlocks advanced techniques
    ///
    /// **Strategic Depth**:
    /// - Topping: +15% yield, -20% height, +2 main colas (week 3-4 veg)
    /// - FIMming: +12% yield, 3-5 colas, less stress than topping
    /// - Lollipopping: +8% yield, +12% quality (focus energy on top buds)
    /// - Defoliation: +5% yield, +15% light penetration (week 1-3 flower)
    /// - LST: +10% yield, -30% height (continuous low stress)
    /// - HST: +15% yield, -40% height (supercropping, high stress)
    /// - ScrOG: +20% yield, extreme canopy control (net training)
    /// - Supercropping: +12% yield, +15% nutrient uptake (stem bending)
    ///
    /// **Integration**:
    /// - Links to plant health (stress tracking)
    /// - Affects yield calculations (final harvest weight)
    /// - Visual mesh updates (plant structure changes)
    /// - Skill tree unlocks (progression gating)
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Topping applied: +2 colas, +15% yield potential" → simple!
    /// Behind scenes: Growth stage validation, stress calculations, yield multipliers, recovery periods.
    /// </summary>
    public class PlantWorkSystem : MonoBehaviour, IPlantWorkSystem
    {
        [Header("Pruning Configuration")]
        [SerializeField] private float _toppingYieldBonus = 0.15f;
        [SerializeField] private float _toppingHeightReduction = 0.20f;
        [SerializeField] private int _toppingColaIncrease = 2;
        [SerializeField] private float _toppingStress = 0.25f;

        [SerializeField] private float _fimmingYieldBonus = 0.12f;
        [SerializeField] private int _fimmingColaMin = 3;
        [SerializeField] private int _fimmingColaMax = 5;
        [SerializeField] private float _fimmingStress = 0.15f;

        [SerializeField] private float _lollipoppingYieldBonus = 0.08f;
        [SerializeField] private float _lollipoppingQualityBonus = 0.12f;
        [SerializeField] private float _lollipoppingStress = 0.10f;

        [SerializeField] private float _defoliationYieldBonus = 0.05f;
        [SerializeField] private float _defoliationLightPenetration = 0.15f;
        [SerializeField] private float _defoliationStress = 0.20f;

        [Header("Training Configuration")]
        [SerializeField] private float _lstYieldBonus = 0.10f;
        [SerializeField] private float _lstHeightReduction = 0.30f;
        [SerializeField] private float _lstStress = 0.05f;

        [SerializeField] private float _hstYieldBonus = 0.15f;
        [SerializeField] private float _hstHeightReduction = 0.40f;
        [SerializeField] private float _hstStress = 0.30f;

        [SerializeField] private float _scrogYieldBonus = 0.20f;
        [SerializeField] private float _scrogStress = 0.08f;

        [SerializeField] private float _supercroppingYieldBonus = 0.12f;
        [SerializeField] private float _supercroppingNutrientBonus = 0.15f;
        [SerializeField] private float _supercroppingStress = 0.25f;

        [Header("Timing Configuration")]
        [SerializeField] private int _toppingMinDaysVeg = 14;
        [SerializeField] private int _toppingMaxDaysVeg = 28;
        [SerializeField] private int _defoliationMinDaysFlower = 7;
        [SerializeField] private int _defoliationMaxDaysFlower = 21;

        // Plant work tracking
        private Dictionary<string, List<PlantWorkRecord>> _workHistory = new Dictionary<string, List<PlantWorkRecord>>();
        private Dictionary<string, PlantWorkEffects> _activeEffects = new Dictionary<string, PlantWorkEffects>();

        // Events
        public event Action<string, PlantWorkType> OnWorkPerformed;
        public event Action<string, string> OnWorkFailed;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Register with service container
            var container = ServiceContainerFactory.Instance;
            container?.RegisterSingleton<IPlantWorkSystem>(this);

            ChimeraLogger.Log("PLANT_WORK",
                "Plant Work system initialized - pruning and training ready!", this);
        }

        #region Pruning Operations

        /// <summary>
        /// Applies topping to a plant (removes main stem tip to create multiple colas).
        /// GAMEPLAY: Player selects plant → Topping → +15% yield, +2 colas, -20% height.
        /// </summary>
        public bool ApplyTopping(string plantId, PlantInstance plant)
        {
            if (!ValidateTiming(plant, PlantWorkType.Topping, _toppingMinDaysVeg, _toppingMaxDaysVeg))
            {
                OnWorkFailed?.Invoke(plantId, "Topping: vegetative stage days 14-28");
                return false;
            }

            if (HasWorkApplied(plantId, PlantWorkType.Topping))
            {
                OnWorkFailed?.Invoke(plantId, "Plant already topped");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.Topping,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _toppingYieldBonus;
            effects.HeightMultiplier -= _toppingHeightReduction;
            effects.ColaCount += _toppingColaIncrease;
            effects.CurrentStress += _toppingStress;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.Topping);
            ChimeraLogger.Log("PLANT_WORK",
                $"Topping: {plantId} +{_toppingYieldBonus:P0} yield, +{_toppingColaIncrease} colas", this);
            return true;
        }

        /// <summary>
        /// Applies FIMming (Fuck I Missed - similar to topping but less precise, creates 3-5 colas).
        /// GAMEPLAY: +12% yield, less stress than topping, variable cola count.
        /// </summary>
        public bool ApplyFIMming(string plantId, PlantInstance plant)
        {
            if (!ValidateTiming(plant, PlantWorkType.FIMming, _toppingMinDaysVeg, _toppingMaxDaysVeg))
            {
                OnWorkFailed?.Invoke(plantId, "FIMming: vegetative stage days 14-28");
                return false;
            }

            if (HasWorkApplied(plantId, PlantWorkType.FIMming))
            {
                OnWorkFailed?.Invoke(plantId, "Plant already FIMmed");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.FIMming,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            // Random cola count (3-5)
            int colaIncrease = UnityEngine.Random.Range(_fimmingColaMin, _fimmingColaMax + 1);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _fimmingYieldBonus;
            effects.ColaCount += colaIncrease;
            effects.CurrentStress += _fimmingStress;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.FIMming);
            ChimeraLogger.Log("PLANT_WORK", $"FIMming: {plantId} +{_fimmingYieldBonus:P0} yield, +{colaIncrease} colas", this);
            return true;
        }

        /// <summary>
        /// Applies lollipopping (removes lower growth to focus energy on top buds).
        /// GAMEPLAY: +8% yield, +12% quality, best for flowering stage.
        /// </summary>
        public bool ApplyLollipopping(string plantId, PlantInstance plant)
        {
            if (plant.CurrentGrowthStage != PlantGrowthStage.Flowering)
            {
                OnWorkFailed?.Invoke(plantId, "Lollipopping: early flowering stage");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.Lollipopping,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _lollipoppingYieldBonus;
            effects.QualityMultiplier += _lollipoppingQualityBonus;
            effects.CurrentStress += _lollipoppingStress;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.Lollipopping);
            ChimeraLogger.Log("PLANT_WORK", $"Lollipopping: {plantId} +{_lollipoppingYieldBonus:P0} yield", this);
            return true;
        }

        /// <summary>
        /// Applies defoliation (removes fan leaves to improve light penetration).
        /// GAMEPLAY: +5% yield, +15% light penetration, timing critical.
        /// </summary>
        public bool ApplyDefoliation(string plantId, PlantInstance plant)
        {
            if (!ValidateTiming(plant, PlantWorkType.Defoliation, _defoliationMinDaysFlower, _defoliationMaxDaysFlower))
            {
                OnWorkFailed?.Invoke(plantId, "Defoliation: flowering stage days 7-21");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.Defoliation,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _defoliationYieldBonus;
            effects.LightPenetration += _defoliationLightPenetration;
            effects.CurrentStress += _defoliationStress;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.Defoliation);
            ChimeraLogger.Log("PLANT_WORK", $"Defoliation: {plantId} +{_defoliationYieldBonus:P0} yield", this);
            return true;
        }

        #endregion

        #region Training Operations

        /// <summary>
        /// Applies LST (Low Stress Training - bending stems to create even canopy).
        /// GAMEPLAY: +10% yield, -30% height, continuous low stress, can apply multiple times.
        /// </summary>
        public bool ApplyLST(string plantId, PlantInstance plant)
        {
            if (plant.CurrentGrowthStage != PlantGrowthStage.Vegetative && plant.CurrentGrowthStage != PlantGrowthStage.Flowering)
            {
                OnWorkFailed?.Invoke(plantId, "LST requires vegetative or early flowering stage");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.LST,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _lstYieldBonus;
            effects.HeightMultiplier -= _lstHeightReduction;
            effects.CurrentStress += _lstStress;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.LST);
            ChimeraLogger.Log("PLANT_WORK", $"LST: {plantId} +{_lstYieldBonus:P0} yield", this);
            return true;
        }

        /// <summary>
        /// Applies HST (High Stress Training - aggressive techniques for canopy control).
        /// GAMEPLAY: +15% yield, -40% height, higher stress but greater yield boost.
        /// </summary>
        public bool ApplyHST(string plantId, PlantInstance plant)
        {
            if (plant.CurrentGrowthStage != PlantGrowthStage.Vegetative)
            {
                OnWorkFailed?.Invoke(plantId, "HST requires vegetative stage");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.HST,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _hstYieldBonus;
            effects.HeightMultiplier -= _hstHeightReduction;
            effects.CurrentStress += _hstStress;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.HST);
            ChimeraLogger.Log("PLANT_WORK", $"HST: {plantId} +{_hstYieldBonus:P0} yield", this);
            return true;
        }

        /// <summary>
        /// Applies ScrOG (Screen of Green - net training for extreme canopy control).
        /// GAMEPLAY: +20% yield, extreme canopy control, requires dedicated setup.
        /// </summary>
        public bool ApplyScrOG(string plantId, PlantInstance plant)
        {
            if (plant.CurrentGrowthStage != PlantGrowthStage.Vegetative)
            {
                OnWorkFailed?.Invoke(plantId, "ScrOG requires vegetative stage");
                return false;
            }

            if (HasWorkApplied(plantId, PlantWorkType.ScrOG))
            {
                OnWorkFailed?.Invoke(plantId, "Plant already in ScrOG setup");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.ScrOG,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _scrogYieldBonus;
            effects.CurrentStress += _scrogStress;
            effects.IsScrogged = true;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.ScrOG);
            ChimeraLogger.Log("PLANT_WORK", $"ScrOG: {plantId} +{_scrogYieldBonus:P0} yield", this);
            return true;
        }

        /// <summary>
        /// Applies supercropping (bending/pinching stems to increase nutrient uptake).
        /// GAMEPLAY: +12% yield, +15% nutrient uptake, high stress technique.
        /// </summary>
        public bool ApplySupercropping(string plantId, PlantInstance plant)
        {
            if (plant.CurrentGrowthStage != PlantGrowthStage.Vegetative)
            {
                OnWorkFailed?.Invoke(plantId, "Supercropping requires vegetative stage");
                return false;
            }

            var record = new PlantWorkRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                PlantId = plantId,
                WorkType = PlantWorkType.Supercropping,
                ApplicationDate = DateTime.Now,
                GrowthStage = plant.CurrentGrowthStage,
                DaysIntoStage = (int)(plant.Age) // Data layer PlantInstance doesn't track stage transitions
            };

            AddWorkRecord(plantId, record);

            var effects = GetOrCreateEffects(plantId);
            effects.YieldMultiplier += _supercroppingYieldBonus;
            effects.NutrientUptake += _supercroppingNutrientBonus;
            effects.CurrentStress += _supercroppingStress;
            _activeEffects[plantId] = effects;

            OnWorkPerformed?.Invoke(plantId, PlantWorkType.Supercropping);
            ChimeraLogger.Log("PLANT_WORK", $"Supercropping: {plantId} +{_supercroppingYieldBonus:P0} yield", this);
            return true;
        }

        #endregion

        #region Helper Methods

        private bool ValidateTiming(PlantInstance plant, PlantWorkType workType, int minDays, int maxDays)
        {
            int daysInStage = (int)plant.Age; // Data layer PlantInstance doesn't track stage transitions

            if (workType == PlantWorkType.Topping || workType == PlantWorkType.FIMming)
            {
                return plant.CurrentGrowthStage == PlantGrowthStage.Vegetative &&
                       daysInStage >= minDays && daysInStage <= maxDays;
            }

            if (workType == PlantWorkType.Defoliation)
            {
                return plant.CurrentGrowthStage == PlantGrowthStage.Flowering &&
                       daysInStage >= minDays && daysInStage <= maxDays;
            }

            return true;
        }

        private bool HasWorkApplied(string plantId, PlantWorkType workType) =>
            PlantWorkHelpers.HasWorkApplied(_workHistory, plantId, workType);

        private void AddWorkRecord(string plantId, PlantWorkRecord record) =>
            PlantWorkHelpers.AddWorkRecord(_workHistory, plantId, record);

        private PlantWorkEffects GetOrCreateEffects(string plantId) =>
            PlantWorkHelpers.GetOrCreateEffects(_activeEffects, plantId);

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets work history for a plant.
        /// </summary>
        public List<PlantWorkRecord> GetWorkHistory(string plantId)
        {
            return _workHistory.ContainsKey(plantId) ? _workHistory[plantId] : new List<PlantWorkRecord>();
        }

        /// <summary>
        /// Gets active effects for a plant.
        /// </summary>
        public PlantWorkEffects GetEffects(string plantId)
        {
            return _activeEffects.TryGetValue(plantId, out var effects) ? effects : new PlantWorkEffects
            {
                PlantId = plantId,
                YieldMultiplier = 1.0f,
                HeightMultiplier = 1.0f,
                QualityMultiplier = 1.0f,
                ColaCount = 1
            };
        }

        /// <summary>
        /// Gets plant work statistics for UI display.
        /// </summary>
        public PlantWorkStats GetStatistics()
        {
            var allRecords = _workHistory.Values.SelectMany(h => h).ToList();
            return new PlantWorkStats
            {
                TotalOperations = allRecords.Count,
                PruningOperations = allRecords.Count(r => r.WorkType <= PlantWorkType.Defoliation),
                TrainingOperations = allRecords.Count(r => r.WorkType > PlantWorkType.Defoliation),
                AverageYieldBoost = _activeEffects.Values.Any() ? _activeEffects.Values.Average(e => e.YieldMultiplier) - 1f : 0f,
                PlantsWithWork = _activeEffects.Count,
                MostCommonWork = allRecords.GroupBy(r => r.WorkType).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault()
            };
        }

        #endregion
    }
}
