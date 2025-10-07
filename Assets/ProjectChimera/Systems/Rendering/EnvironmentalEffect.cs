using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Rendering
{
    public class EnvironmentalEffect
    {
        public EffectType Type { get; private set; }
        public Vector3 Position { get; private set; }
        public float Duration { get; private set; }
        public float ElapsedTime { get; private set; }
        public bool IsFinished => ElapsedTime >= Duration;

        private ParticleSystem _particleSystem;
        private bool _isActive;

        public EnvironmentalEffect() { }

        public EnvironmentalEffect(EffectType type, Vector3 position, float duration)
        {
            Reset(type, position, duration);
        }

        public void Reset(EffectType type, Vector3 position, float duration)
        {
            Type = type;
            Position = position;
            Duration = duration;
            ElapsedTime = 0f;
            _isActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!_isActive) return;

            ElapsedTime += deltaTime;
            
            if (ElapsedTime >= Duration)
            {
                _isActive = false;
                
                if (_particleSystem != null)
                {
                    _particleSystem.Stop();
                }
            }
        }

        public void ReduceQuality()
        {
            if (_particleSystem != null)
            {
                var main = _particleSystem.main;
                main.maxParticles = Mathf.Max(10, main.maxParticles / 2);
            }
        }
    }
}