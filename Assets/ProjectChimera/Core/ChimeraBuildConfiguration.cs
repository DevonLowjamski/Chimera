using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// ScriptableObject that stores build configuration settings for Project Chimera
    /// Allows for persistent storage of build profiles and custom script defines
    /// </summary>
    [CreateAssetMenu(fileName = "ChimeraBuildConfiguration", menuName = "Project Chimera/Build Configuration", order = 100)]
    public class ChimeraBuildConfiguration : ScriptableObject
    {
        [Header("Build Profile Configuration")]
        [SerializeField] private string _defaultProfile = "Development";
        [SerializeField] private bool _autoApplyOnPlayMode = true;
        [SerializeField] private bool _validateOnBuild = true;
        
        [Header("Custom Script Defines")]
        [SerializeField] private List<string> _additionalDefines = new List<string>();
        [SerializeField] private List<string> _excludedDefines = new List<string>();
        
        [Header("Build Profile Overrides")]
        [SerializeField] private List<BuildProfileOverride> _profileOverrides = new List<BuildProfileOverride>();
        
        [Header("Platform-Specific Settings")]
        [SerializeField] private List<PlatformBuildSettings> _platformSettings = new List<PlatformBuildSettings>();

        public string DefaultProfile => _defaultProfile;
        public bool AutoApplyOnPlayMode => _autoApplyOnPlayMode;
        public bool ValidateOnBuild => _validateOnBuild;
        public List<string> AdditionalDefines => _additionalDefines;
        public List<string> ExcludedDefines => _excludedDefines;
        public List<BuildProfileOverride> ProfileOverrides => _profileOverrides;
        public List<PlatformBuildSettings> PlatformSettings => _platformSettings;

        /// <summary>
        /// Get script defines for a specific profile with overrides applied
        /// </summary>
        public string[] GetDefinesForProfile(string profileName)
        {
            var defines = new List<string>();
            
            // Start with base profile defines
            if (TryGetProfileOverride(profileName, out var profileOverride))
            {
                defines.AddRange(profileOverride.ScriptDefines);
            }
            
            // Add additional defines
            defines.AddRange(_additionalDefines);
            
            // Remove excluded defines
            defines.RemoveAll(d => _excludedDefines.Contains(d));
            
            return defines.Distinct().ToArray();
        }
        
        /// <summary>
        /// Get platform-specific settings for current platform
        /// </summary>
        public PlatformBuildSettings GetCurrentPlatformSettings()
        {
#if UNITY_EDITOR
            var currentTarget = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
            return _platformSettings.FirstOrDefault(p => p.BuildTargetGroup == currentTarget);
#else
            return null;
#endif
        }
        
        /// <summary>
        /// Try to get profile override settings
        /// </summary>
        private bool TryGetProfileOverride(string profileName, out BuildProfileOverride profileOverride)
        {
            profileOverride = _profileOverrides.FirstOrDefault(p => p.ProfileName == profileName);
            return profileOverride != null;
        }
        
        /// <summary>
        /// Add or update a profile override
        /// </summary>
        public void SetProfileOverride(string profileName, string[] defines)
        {
            var existingOverride = _profileOverrides.FirstOrDefault(p => p.ProfileName == profileName);
            if (existingOverride != null)
            {
                existingOverride.ScriptDefines = new List<string>(defines);
            }
            else
            {
                _profileOverrides.Add(new BuildProfileOverride
                {
                    ProfileName = profileName,
                    ScriptDefines = new List<string>(defines)
                });
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// Validate the current configuration
        /// </summary>
        public bool ValidateConfiguration(out List<string> errors)
        {
            errors = new List<string>();
            
            // Check for conflicting defines
            var allDefines = _additionalDefines.Concat(_profileOverrides.SelectMany(p => p.ScriptDefines)).ToList();
            
            var conflictingPairs = new[]
            {
                new[] { "CHIMERA_PRODUCTION", "CHIMERA_DEV_LOGS" },
                new[] { "CHIMERA_PRODUCTION", "CHIMERA_DEBUG_UI" },
                new[] { "CHIMERA_OPTIMIZED", "CHIMERA_FULL_LOGGING" },
                new[] { "CHIMERA_RELEASE", "CHIMERA_ASSERTS" }
            };
            
            foreach (var pair in conflictingPairs)
            {
                if (pair.All(d => allDefines.Contains(d)))
                {
                    errors.Add($"Conflicting defines detected: {string.Join(" and ", pair)}");
                }
            }
            
            // Check for invalid define names
            var invalidDefines = allDefines.Where(d => string.IsNullOrWhiteSpace(d) || d.Contains(" ") || d.Contains(";")).ToList();
            if (invalidDefines.Any())
            {
                errors.Add($"Invalid define names: {string.Join(", ", invalidDefines)}");
            }
            
            return errors.Count == 0;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Project Chimera/Build Configuration")]
        public static void CreateBuildConfiguration()
        {
            var asset = CreateInstance<ChimeraBuildConfiguration>();
            
            // Set up default values
            asset._defaultProfile = "Development";
            asset._autoApplyOnPlayMode = true;
            asset._validateOnBuild = true;
            
            // Add some default additional defines
            asset._additionalDefines.Add("CHIMERA_CUSTOM");
            
            UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/ProjectChimera/Core/DefaultBuildConfiguration.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            
            UnityEditor.EditorUtility.FocusProjectWindow();
            UnityEditor.Selection.activeObject = asset;
        }
#endif
    }

    /// <summary>
    /// Represents an override for a specific build profile
    /// </summary>
    [System.Serializable]
    public class BuildProfileOverride
    {
        [SerializeField] public string ProfileName;
        [SerializeField] public List<string> ScriptDefines = new List<string>();
        [SerializeField] public string Description;
        [SerializeField] public bool Enabled = true;
    }

    /// <summary>
    /// Platform-specific build settings
    /// </summary>
    [System.Serializable]
    public class PlatformBuildSettings
    {
#if UNITY_EDITOR
        [SerializeField] public UnityEditor.BuildTargetGroup BuildTargetGroup;
#endif
        [SerializeField] public List<string> AdditionalDefines = new List<string>();
        [SerializeField] public List<string> ExcludedDefines = new List<string>();
        [SerializeField] public bool EnableOptimizations = true;
        [SerializeField] public string Notes;
    }
}