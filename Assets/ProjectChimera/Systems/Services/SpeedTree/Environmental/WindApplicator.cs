// REFACTORED: Wind Applicator
// Extracted from WindSystem for better separation of concerns

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using IServiceContainer = ProjectChimera.Core.IServiceContainer;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Applies wind effects to renderers
    /// </summary>
    public class WindApplicator
    {
        private readonly int _windStrengthPropertyId;
        private readonly int _windDirectionPropertyId;
        private readonly int _windGustPropertyId;

        public WindApplicator()
        {
            _windStrengthPropertyId = Shader.PropertyToID("_WindStrength");
            _windDirectionPropertyId = Shader.PropertyToID("_WindDirection");
            _windGustPropertyId = Shader.PropertyToID("_WindGust");
        }

        public void ApplyGlobalWindToRenderers(float windStrength, Vector3 windDirection)
        {
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allRenderers = registry?.GetAll<Renderer>();

            if (allRenderers == null || !allRenderers.Any())
            {
                ChimeraLogger.LogWarning("SPEEDTREE/WIND", "No Renderers found - ensure they are registered with GameObjectRegistry in Awake()", null);
                return;
            }

            // Filter for SpeedTree renderers
            var renderers = allRenderers
                .Where(r => r.sharedMaterial != null &&
                           r.sharedMaterial.shader != null &&
                           r.sharedMaterial.shader.name.Contains("SpeedTree"));

            foreach (var renderer in renderers)
                ApplyWindToRenderer(renderer, windStrength, windDirection);
        }

        public void ApplyWindToZoneRenderers(WindZone windZone, WindZoneSettings settings, float globalStrength, Vector3 globalDirection)
        {
            if (windZone == null) return;

            var container = ServiceContainerFactory.Instance;
            if (container == null) return;

            var registry = container.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allRenderers = registry?.GetAll<Renderer>();

            if (allRenderers == null || !allRenderers.Any())
            {
                ChimeraLogger.LogWarning("SPEEDTREE/WIND", "No Renderers found for wind zone", null);
                return;
            }

            // Find renderers within wind zone range
            var renderers = allRenderers
                .Where(r => r.sharedMaterial != null &&
                           r.sharedMaterial.shader != null &&
                           r.sharedMaterial.shader.name.Contains("SpeedTree") &&
                           IsRendererInWindZone(r, windZone));

            foreach (var renderer in renderers)
            {
                float zoneStrength = settings.Strength * globalStrength;
                Vector3 zoneDirection = settings.Direction != Vector3.zero ? settings.Direction : globalDirection;
                ApplyWindToRenderer(renderer, zoneStrength, zoneDirection);
            }
        }

        private void ApplyWindToRenderer(Renderer renderer, float strength, Vector3 direction)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;

            try
            {
                var material = renderer.material;

                if (material.HasProperty(_windStrengthPropertyId))
                    material.SetFloat(_windStrengthPropertyId, strength);

                if (material.HasProperty(_windDirectionPropertyId))
                    material.SetVector(_windDirectionPropertyId, direction);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/WIND", "ApplyWindToRenderer exception", null);
            }
        }

        private bool IsRendererInWindZone(Renderer renderer, WindZone windZone)
        {
            if (renderer == null || windZone == null) return false;

            float distance = Vector3.Distance(renderer.transform.position, windZone.transform.position);
            return distance <= windZone.radius;
        }
    }
}

