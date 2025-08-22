using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Construction;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Integration service bridging Economy system with Construction system for Phase 8 MVP
    /// Manages the flow of unlocked schematics into usable construction templates
    /// </summary>
    public class SchematicConstructionIntegration : ChimeraManager
    {
        [Header("Integration Configuration")]
        [SerializeField] private bool _enableSchematicIntegration = true;
        [SerializeField] private bool _autoRegisterUnlocks = true;
        [SerializeField] private bool _enableConstructionNotifications = true;
        [SerializeField] private float _integrationCheckInterval = 5f;
        
        [Header("Schematic to Template Mapping")]
        [SerializeField] private List<SchematicTemplateMapping> _schematicMappings = new List<SchematicTemplateMapping>();
        [SerializeField] private bool _enableDynamicMappingGeneration = true;
        [SerializeField] private string _defaultTemplatePrefix = "Schematic_";
        
        [Header("Construction Integration")]
        [SerializeField] private bool _updateConstructionCatalog = true;
        [SerializeField] private bool _notifyConstructionManager = true;
        [SerializeField] private bool _enableProgressiveUnlocks = true;
        
        // Service dependencies
        private ConstructionManager _constructionManager;
        
        // Integration state
        private HashSet<string> _registeredUnlocks = new HashSet<string>();
        private Dictionary<string, string> _schematicToTemplateMap = new Dictionary<string, string>();
        private Dictionary<string, DateTime> _unlockTimestamps = new Dictionary<string, DateTime>();
        private List<string> _pendingNotifications = new List<string>();
        
        // Integration metrics
        private int _totalSchematicsIntegrated = 0;
        private int _activeConstructionTemplates = 0;
        private float _lastIntegrationCheck = 0f;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Public Properties
        public bool IntegrationEnabled { get => _enableSchematicIntegration; set => _enableSchematicIntegration = value; }
        public int IntegratedSchematicsCount => _registeredUnlocks.Count;
        public int TotalSchematicsIntegrated => _totalSchematicsIntegrated;
        public List<string> UnlockedSchematicIds => new List<string>(_registeredUnlocks);
        public Dictionary<string, string> SchematicTemplateMapping => new Dictionary<string, string>(_schematicToTemplateMap);
        
        // Events
        public System.Action<ConstructionSchematicSO, string> OnSchematicIntegrated; // schematic, templateName
        public System.Action<string> OnConstructionTemplateUnlocked; // templateName
        public System.Action<int> OnIntegrationUpdate; // total unlocked count
        public System.Action<string> OnIntegrationError; // error message
        
        protected override void OnManagerInitialize()
        {
            InitializeServiceReferences();
            InitializeSchematicMappings();
            RegisterExistingUnlocks();
            SubscribeToSchematicEvents();
            
            LogInfo($"SchematicConstructionIntegration initialized - {IntegratedSchematicsCount} schematics integrated");
        }
        
        protected override void OnManagerUpdate()
        {
            float currentTime = Time.time;
            
            // Periodic integration check
            if (_enableSchematicIntegration && currentTime - _lastIntegrationCheck >= _integrationCheckInterval)
            {
                ProcessPendingIntegrations();
                _lastIntegrationCheck = currentTime;
            }
        }
        
        /// <summary>
        /// Get list of unlocked construction templates based on purchased schematics
        /// </summary>
        public List<string> GetUnlockedConstructionTemplates()
        {
            var unlockedTemplates = new List<string>();
            
            foreach (string schematicId in _registeredUnlocks)
            {
                if (_schematicToTemplateMap.TryGetValue(schematicId, out string templateName))
                {
                    unlockedTemplates.Add(templateName);
                }
            }
            
            return unlockedTemplates;
        }
        
        /// <summary>
        /// Check if a specific construction template is unlocked via schematics
        /// </summary>
        public bool IsConstructionTemplateUnlocked(string templateName)
        {
            return _schematicToTemplateMap.ContainsValue(templateName) && 
                   _schematicToTemplateMap.Any(kvp => kvp.Value == templateName && _registeredUnlocks.Contains(kvp.Key));
        }
        
        /// <summary>
        /// Get the schematic that unlocks a specific construction template
        /// </summary>
        public string GetSchematicForTemplate(string templateName)
        {
            var mapping = _schematicToTemplateMap.FirstOrDefault(kvp => kvp.Value == templateName);
            return mapping.Key ?? null;
        }
        
        /// <summary>
        /// Manually register a schematic unlock for construction integration
        /// </summary>
        public bool RegisterSchematicUnlock(string schematicId)
        {
            if (string.IsNullOrEmpty(schematicId) || _registeredUnlocks.Contains(schematicId))
                return false;
            
            _registeredUnlocks.Add(schematicId);
            _unlockTimestamps[schematicId] = DateTime.Now;
            _totalSchematicsIntegrated++;
            
            // Find and register corresponding construction template
            if (_schematicToTemplateMap.TryGetValue(schematicId, out string templateName))
            {
                NotifyConstructionUnlock(templateName);
                OnConstructionTemplateUnlocked?.Invoke(templateName);
                
                LogInfo($"Registered schematic unlock: {schematicId} → {templateName}");
                return true;
            }
            else if (_enableDynamicMappingGeneration)
            {
                // Generate dynamic mapping
                string generatedTemplateName = GenerateTemplateName(schematicId);
                _schematicToTemplateMap[schematicId] = generatedTemplateName;
                
                NotifyConstructionUnlock(generatedTemplateName);
                OnConstructionTemplateUnlocked?.Invoke(generatedTemplateName);
                
                LogInfo($"Dynamically mapped schematic: {schematicId} → {generatedTemplateName}");
                return true;
            }
            
            LogWarning($"No template mapping found for schematic: {schematicId}");
            return false;
        }
        
        /// <summary>
        /// Add or update schematic to template mapping
        /// </summary>
        public void AddSchematicMapping(string schematicId, string templateName)
        {
            if (string.IsNullOrEmpty(schematicId) || string.IsNullOrEmpty(templateName))
                return;
            
            _schematicToTemplateMap[schematicId] = templateName;
            
            // If schematic is already unlocked, register the template immediately
            if (_registeredUnlocks.Contains(schematicId))
            {
                NotifyConstructionUnlock(templateName);
                OnConstructionTemplateUnlocked?.Invoke(templateName);
            }
            
            LogInfo($"Added schematic mapping: {schematicId} → {templateName}");
        }
        
        /// <summary>
        /// Bulk add schematic mappings from configuration
        /// </summary>
        public void ConfigureSchematicMappings(List<SchematicTemplateMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.IsValid)
                {
                    AddSchematicMapping(mapping.SchematicId, mapping.TemplateNames[0]);
                }
            }
            
            LogInfo($"Configured {mappings.Count} schematic mappings");
        }
        
        /// <summary>
        /// Get integration statistics
        /// </summary>
        public SchematicIntegrationStats GetIntegrationStats()
        {
            return new SchematicIntegrationStats
            {
                TotalSchematicsRegistered = _registeredUnlocks.Count,
                TotalTemplatesUnlocked = GetUnlockedConstructionTemplates().Count,
                MappingCount = _schematicToTemplateMap.Count,
                IntegrationEnabled = _enableSchematicIntegration,
                LastUpdateTime = DateTime.Now
            };
        }
        
        private void InitializeServiceReferences()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                _constructionManager = gameManager.GetManager<ConstructionManager>();
            }
            
            if (_constructionManager == null)
                LogWarning("ConstructionManager not found - construction notifications disabled");
        }
        
        private void InitializeSchematicMappings()
        {
            // Initialize mappings from configuration
            foreach (var mapping in _schematicMappings)
            {
                if (mapping.IsValid)
                {
                    foreach (string templateName in mapping.TemplateNames)
                    {
                        _schematicToTemplateMap[mapping.SchematicId] = templateName;
                    }
                }
            }
            
            LogInfo($"Initialized {_schematicToTemplateMap.Count} schematic-to-template mappings");
        }
        
        private void RegisterExistingUnlocks()
        {
            // Note: Economy system integration handled through events to prevent circular dependency
            // In full implementation, would query Economy system for existing unlocks
            LogInfo("RegisterExistingUnlocks: Event-based integration (no direct Economy reference)");
        }
        
        private void SubscribeToSchematicEvents()
        {
            // Note: Economy system integration handled through events to prevent circular dependency
            // In full implementation, would subscribe to Economy system events
            LogInfo("SubscribeToSchematicEvents: Event-based integration (no direct Economy reference)");
        }
        
        private void ProcessPendingIntegrations()
        {
            // Process any pending integration notifications
            foreach (string templateName in _pendingNotifications.ToList())
            {
                NotifyConstructionUnlock(templateName);
                _pendingNotifications.Remove(templateName);
            }
        }
        
        private void NotifyConstructionUnlock(string templateName)
        {
            if (!_notifyConstructionManager || _constructionManager == null)
                return;
            
            // The construction manager can use GetUnlockedConstructionTemplates() to get current unlocks
            // This integration provides the data source for construction system queries
            
            if (_enableConstructionNotifications)
            {
                LogInfo($"Construction template unlocked: {templateName}");
            }
        }
        
        private string GenerateTemplateName(string schematicId)
        {
            // Generate a template name based on schematic ID
            return $"{_defaultTemplatePrefix}{schematicId}";
        }
        
        private void OnSchematicUnlockedHandler(ConstructionSchematicSO schematic)
        {
            if (_autoRegisterUnlocks)
            {
                RegisterSchematicUnlock(schematic.SchematicId);
                OnSchematicIntegrated?.Invoke(schematic, _schematicToTemplateMap.GetValueOrDefault(schematic.SchematicId, "Unknown"));
                OnIntegrationUpdate?.Invoke(_registeredUnlocks.Count);
            }
        }
        
        private void OnSchematicPurchasedHandler(ConstructionSchematicSO schematic, float cost)
        {
            // Schematic purchase automatically unlocks it, so this will trigger the unlock handler
            LogInfo($"Schematic purchased and ready for construction integration: {schematic.SchematicName}");
        }
        
        protected override void OnManagerShutdown()
        {
            // Note: Economy system integration handled through events to prevent circular dependency
            // In full implementation, would unsubscribe from Economy system events
            
            LogInfo($"SchematicConstructionIntegration shutdown - {IntegratedSchematicsCount} schematics were integrated");
        }
    }
    
    /// <summary>
    /// Configuration mapping between schematics and construction templates
    /// </summary>
    [System.Serializable]
    public class SchematicTemplateMapping
    {
        [Header("Mapping Configuration")]
        [SerializeField] public string SchematicId;
        [SerializeField] public List<string> TemplateNames = new List<string>();
        [SerializeField] public bool IsActive = true;
        [SerializeField] public string Description;
        
        public bool IsValid => !string.IsNullOrEmpty(SchematicId) && TemplateNames.Count > 0 && IsActive;
    }
    
    /// <summary>
    /// Statistics for schematic-construction integration
    /// </summary>
    [System.Serializable]
    public class SchematicIntegrationStats
    {
        public int TotalSchematicsRegistered;
        public int TotalTemplatesUnlocked;
        public int MappingCount;
        public bool IntegrationEnabled;
        public DateTime LastUpdateTime;
    }
}