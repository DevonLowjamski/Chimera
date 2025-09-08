using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using CultivationTaskType = ProjectChimera.Data.Cultivation.CultivationTaskType;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Manages particle effects for plant care actions
    /// </summary>
    public class CareEffectParticleSystem : MonoBehaviour
    {
        [Header("Particle Configuration")]
        [SerializeField] private ParticleSystem _wateringParticles;
        [SerializeField] private ParticleSystem _pruningParticles;
        [SerializeField] private ParticleSystem _harvestParticles;
        [SerializeField] private ParticleSystem _generalCareParticles;
        
        private bool _isInitialized = false;
        
        public void Initialize()
        {
            _isInitialized = true;
            
            // Initialize particle systems if not assigned
            if (_generalCareParticles == null)
                _generalCareParticles = GetComponent<ParticleSystem>();
        }
        
        public void PlayCareEffect(CultivationTaskType taskType, Vector3 position, float intensity = 1f)
        {
            if (!_isInitialized) return;
            
            ParticleSystem targetSystem = taskType switch
            {
                CultivationTaskType.Watering => _wateringParticles,
                CultivationTaskType.Pruning => _pruningParticles,
                CultivationTaskType.Harvesting => _harvestParticles,
                _ => _generalCareParticles
            };
            
            if (targetSystem != null)
            {
                var emission = targetSystem.emission;
                emission.rateOverTime = 10f * intensity;
                
                targetSystem.transform.position = position;
                targetSystem.Play();
            }
        }
        
        public void PlayMilestoneEffect(Vector3 position)
        {
            if (!_isInitialized) return;
            
            // Use general care particles for milestone celebration with enhanced intensity
            if (_generalCareParticles != null)
            {
                var emission = _generalCareParticles.emission;
                emission.rateOverTime = 25f; // Higher intensity for milestone celebration
                
                _generalCareParticles.transform.position = position;
                _generalCareParticles.Play();
            }
        }
        
        public void StopAllEffects()
        {
            _wateringParticles?.Stop();
            _pruningParticles?.Stop();
            _harvestParticles?.Stop();
            _generalCareParticles?.Stop();
        }
    }
}