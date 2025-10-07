using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Prefabs
{
    /// <summary>
    /// Effect sequences and coordination system for managing complex multi-stage effects
    /// </summary>
    [CreateAssetMenu(fileName = "Effect Sequences Library", menuName = "Project Chimera/Effects/Effect Sequences")]
    public class EffectSequencesSO : ScriptableObject
    {
        [Header("Effect Sequences")]
        [SerializeField] private List<EffectSequence> _effectSequences = new List<EffectSequence>();
        
        [Header("Particle Effect Sets")]
        [SerializeField] private List<ParticleEffectSet> _particleEffects = new List<ParticleEffectSet>();
        
        [Header("Audio Effect Sets")]
        [SerializeField] private List<AudioEffectSet> _audioEffects = new List<AudioEffectSet>();
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableParticlePooling = true;
        [SerializeField] private int _maxParticleInstances = 200;
        [SerializeField] private bool _enable3DAudio = true;
        [SerializeField] private bool _enableDynamicMixing = true;
        [SerializeField] private bool _enableLODEffects = true;
        [SerializeField] private float _cullingDistance = 50f;
        [SerializeField] private int _maxConcurrentEffects = 50;
        
        // Cached lookup tables
        private Dictionary<string, EffectSequence> _sequenceLookup;
        
        public List<EffectSequence> EffectSequences => _effectSequences;
        public List<ParticleEffectSet> ParticleEffects => _particleEffects;
        public List<AudioEffectSet> AudioEffects => _audioEffects;
        
        public bool EnableParticlePooling => _enableParticlePooling;
        public int MaxParticleInstances => _maxParticleInstances;
        public bool Enable3DAudio => _enable3DAudio;
        public bool EnableDynamicMixing => _enableDynamicMixing;
        public bool EnableLODEffects => _enableLODEffects;
        public float CullingDistance => _cullingDistance;
        public int MaxConcurrentEffects => _maxConcurrentEffects;
        
        public void InitializeDefaults()
        {
            if (_effectSequences.Count == 0)
            {
                CreateDefaultEffectSequences();
            }
            
            BuildLookupTables();
        }
        
        private void CreateDefaultEffectSequences()
        {
            // Plant Growth Sequence
            _effectSequences.Add(new EffectSequence
            {
                SequenceId = "plant_growth_sequence",
                SequenceName = "Plant Growth Sequence",
                TotalDuration = 10f,
                SequenceSteps = new List<EffectSequenceStep>
                {
                    new EffectSequenceStep 
                    { 
                        EffectId = "plant_growth_sparkles", 
                        StartTime = 0f, 
                        Duration = 3f,
                        Intensity = 1f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "plant_watering_effect", 
                        StartTime = 2f, 
                        Duration = 5f,
                        Intensity = 0.8f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "plant_growth_sparkles", 
                        StartTime = 6f, 
                        Duration = 4f,
                        Intensity = 1.2f
                    }
                }
            });
            
            // System Startup Sequence
            _effectSequences.Add(new EffectSequence
            {
                SequenceId = "system_startup_sequence",
                SequenceName = "System Startup Sequence",
                TotalDuration = 5f,
                SequenceSteps = new List<EffectSequenceStep>
                {
                    new EffectSequenceStep 
                    { 
                        EffectId = "equipment_power_on", 
                        StartTime = 0f, 
                        Duration = 1.5f,
                        Intensity = 1f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "data_processing_effect", 
                        StartTime = 1f, 
                        Duration = 2f,
                        Intensity = 0.8f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "air_circulation_particles", 
                        StartTime = 2f, 
                        Duration = 0f, // Continuous
                        Intensity = 0.6f
                    }
                }
            });
            
            // Harvest Completion Sequence
            _effectSequences.Add(new EffectSequence
            {
                SequenceId = "harvest_completion_sequence",
                SequenceName = "Harvest Completion Sequence",
                TotalDuration = 4f,
                SequenceSteps = new List<EffectSequenceStep>
                {
                    new EffectSequenceStep 
                    { 
                        EffectId = "harvest_ready_shimmer", 
                        StartTime = 0f, 
                        Duration = 1f,
                        Intensity = 1f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "harvest_completion_burst", 
                        StartTime = 0.5f, 
                        Duration = 1.5f,
                        Intensity = 1.5f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "achievement_fireworks", 
                        StartTime = 1.5f, 
                        Duration = 3f,
                        Intensity = 2f
                    }
                }
            });
            
            // Environmental Weather Sequence
            _effectSequences.Add(new EffectSequence
            {
                SequenceId = "weather_transition_sequence",
                SequenceName = "Weather Transition Sequence",
                TotalDuration = 15f,
                SequenceSteps = new List<EffectSequenceStep>
                {
                    new EffectSequenceStep 
                    { 
                        EffectId = "wind_gust", 
                        StartTime = 0f, 
                        Duration = 3f,
                        Intensity = 1.2f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "rain_effect", 
                        StartTime = 2f, 
                        Duration = 10f,
                        Intensity = 1f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "lightning_flash", 
                        StartTime = 5f, 
                        Duration = 0.5f,
                        Intensity = 5f
                    },
                    new EffectSequenceStep 
                    { 
                        EffectId = "lightning_flash", 
                        StartTime = 8f, 
                        Duration = 0.5f,
                        Intensity = 3f
                    }
                }
            });
        }
        
        private void BuildLookupTables()
        {
            _sequenceLookup = _effectSequences.ToDictionary(s => s.SequenceId, s => s);
        }
        
        public EffectSequence GetEffectSequence(string sequenceId)
        {
            return _sequenceLookup.TryGetValue(sequenceId, out var sequence) ? sequence : null;
        }
        
        public List<EffectSequence> GetSequencesByDuration(float minDuration, float maxDuration)
        {
            return _effectSequences.Where(s => s.TotalDuration >= minDuration && s.TotalDuration <= maxDuration).ToList();
        }
        
        public List<EffectSequence> GetSequencesByEffectId(string effectId)
        {
            return _effectSequences.Where(s => s.SequenceSteps.Any(step => step.EffectId == effectId)).ToList();
        }
        
        public EffectSequence CreateCustomSequence(string sequenceId, string sequenceName, List<EffectSequenceStep> steps)
        {
            var sequence = new EffectSequence
            {
                SequenceId = sequenceId,
                SequenceName = sequenceName,
                SequenceSteps = steps,
                TotalDuration = steps.Count > 0 ? steps.Max(s => s.StartTime + s.Duration) : 0f
            };
            
            _effectSequences.Add(sequence);
            BuildLookupTables();
            
            return sequence;
        }
        
        public bool ValidateSequence(EffectSequence sequence)
        {
            if (sequence == null || string.IsNullOrEmpty(sequence.SequenceId))
                return false;
                
            if (sequence.SequenceSteps == null || sequence.SequenceSteps.Count == 0)
                return false;
                
            // Check for valid timing
            foreach (var step in sequence.SequenceSteps)
            {
                if (step.StartTime < 0f || (step.Duration > 0f && step.StartTime + step.Duration > sequence.TotalDuration))
                    return false;
                    
                if (step.Intensity <= 0f)
                    return false;
            }
            
            return true;
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                BuildLookupTables();
            }
        }
    }
    
    // Supporting data structures for sequences
    [System.Serializable]
    public class EffectSequence
    {
        public string SequenceId;
        public string SequenceName;
        public float TotalDuration;
        public List<EffectSequenceStep> SequenceSteps = new List<EffectSequenceStep>();
        public bool Loop = false;
        public float LoopDelay = 0f;
    }
    
    [System.Serializable]
    public class EffectSequenceStep
    {
        public string EffectId;
        public float StartTime;
        public float Duration;
        public float Intensity = 1f;
        public Vector3 Position = Vector3.zero;
        public bool UseRelativePosition = true;
    }
    
    [System.Serializable]
    public class ParticleEffectSet
    {
        public string SetId;
        public string SetName;
        public List<string> ParticleEffectIds = new List<string>();
        public bool EnableBatching = true;
        public int MaxInstancesPerSet = 10;
    }
    
    [System.Serializable]
    public class AudioEffectSet
    {
        public string SetId;
        public string SetName;
        public List<string> AudioEffectIds = new List<string>();
        public float MasterVolume = 1f;
        public bool EnableRandomization = false;
    }
}