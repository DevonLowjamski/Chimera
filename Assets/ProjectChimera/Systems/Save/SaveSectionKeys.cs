using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Standardized section keys and naming conventions for save system providers.
    /// Ensures consistent naming across all save sections and prevents conflicts.
    /// </summary>
    public static class SaveSectionKeys
    {
        #region Core Domain Section Keys

        /// <summary>
        /// Plant cultivation, genetics, breeding, and harvest data
        /// </summary>
        public const string CULTIVATION = "cultivation";

        /// <summary>
        /// Facility construction, placement, and infrastructure
        /// </summary>
        public const string CONSTRUCTION = "construction";

        /// <summary>
        /// Economy, market data, trading, and financial systems
        /// </summary>
        public const string ECONOMY = "economy";

        /// <summary>
        /// Player progression, skills, research, and achievements
        /// </summary>
        public const string PROGRESSION = "progression";

        /// <summary>
        /// UI state, preferences, and interface customization
        /// </summary>
        public const string UI_STATE = "ui_state";

        /// <summary>
        /// Environmental systems, climate control, and automation
        /// </summary>
        public const string ENVIRONMENT = "environment";

        /// <summary>
        /// Game settings, configuration, and player preferences
        /// </summary>
        public const string SETTINGS = "settings";

        /// <summary>
        /// Player profile, statistics, and account data
        /// </summary>
        public const string PLAYER = "player";

        /// <summary>
        /// Time management, scheduling, and temporal events
        /// </summary>
        public const string TIME = "time";

        /// <summary>
        /// Analytics, metrics, and performance data
        /// </summary>
        public const string ANALYTICS = "analytics";

        #endregion

        #region Extended Domain Section Keys

        /// <summary>
        /// Event system, notifications, and dynamic content
        /// </summary>
        public const string EVENTS = "events";

        /// <summary>
        /// AI systems, automation, and intelligent agents
        /// </summary>
        public const string AI_SYSTEMS = "ai_systems";

        /// <summary>
        /// Tutorial system, guidance, and onboarding state
        /// </summary>
        public const string TUTORIAL = "tutorial";

        /// <summary>
        /// Multiplayer data, social features, and collaboration
        /// </summary>
        public const string MULTIPLAYER = "multiplayer";

        /// <summary>
        /// Modding support, custom content, and extensions
        /// </summary>
        public const string MODDING = "modding";

        /// <summary>
        /// Inventory management and item systems
        /// </summary>
        public const string INVENTORY = "inventory";

        /// <summary>
        /// Quest and objective tracking
        /// </summary>
        public const string OBJECTIVES = "objectives";

        /// <summary>
        /// Random event systems and dynamic gameplay
        /// </summary>
        public const string RANDOM_EVENTS = "random_events";

        #endregion

        #region Section Key Validation and Conventions

        /// <summary>
        /// All valid section keys for validation purposes
        /// </summary>
        public static readonly HashSet<string> ValidSectionKeys = new HashSet<string>
        {
            CULTIVATION, CONSTRUCTION, ECONOMY, PROGRESSION, UI_STATE,
            ENVIRONMENT, SETTINGS, PLAYER, TIME, ANALYTICS,
            EVENTS, AI_SYSTEMS, TUTORIAL, MULTIPLAYER, MODDING,
            INVENTORY, OBJECTIVES, RANDOM_EVENTS
        };

        /// <summary>
        /// Section keys that are required for basic game functionality
        /// </summary>
        public static readonly HashSet<string> RequiredSectionKeys = new HashSet<string>
        {
            PLAYER, SETTINGS, TIME
        };

        /// <summary>
        /// Section keys that support incremental saves
        /// </summary>
        public static readonly HashSet<string> IncrementalSaveSupported = new HashSet<string>
        {
            CULTIVATION, CONSTRUCTION, ECONOMY, PROGRESSION, 
            ENVIRONMENT, ANALYTICS, EVENTS
        };

        /// <summary>
        /// Priority order for save operations (higher priority saved first)
        /// </summary>
        public static readonly Dictionary<string, int> SavePriorities = new Dictionary<string, int>
        {
            { PLAYER, 100 },         // Always save player data first
            { SETTINGS, 95 },        // Settings are critical
            { TIME, 90 },           // Time state is important for offline progression
            { ECONOMY, 80 },        // Economy affects many other systems
            { PROGRESSION, 75 },    // Progression unlocks content
            { CULTIVATION, 70 },    // Core gameplay system
            { CONSTRUCTION, 65 },   // Facility state affects other systems  
            { ENVIRONMENT, 60 },    // Environmental state
            { INVENTORY, 55 },      // Item systems
            { OBJECTIVES, 50 },     // Quest state
            { AI_SYSTEMS, 45 },     // AI and automation
            { EVENTS, 40 },         // Event systems
            { RANDOM_EVENTS, 35 },  // Dynamic content
            { TUTORIAL, 30 },       // Tutorial state
            { UI_STATE, 25 },       // UI preferences
            { ANALYTICS, 20 },      // Analytics data
            { MULTIPLAYER, 15 },    // Multiplayer (if applicable)
            { MODDING, 10 }         // Modding support
        };

        /// <summary>
        /// Load order dependencies (key depends on values being loaded first)
        /// </summary>
        public static readonly Dictionary<string, string[]> LoadDependencies = new Dictionary<string, string[]>
        {
            { CULTIVATION, new[] { PLAYER, SETTINGS, TIME, ECONOMY } },
            { CONSTRUCTION, new[] { PLAYER, SETTINGS, ECONOMY } },
            { ENVIRONMENT, new[] { CONSTRUCTION, SETTINGS } },
            { PROGRESSION, new[] { PLAYER, CULTIVATION, ECONOMY } },
            { AI_SYSTEMS, new[] { SETTINGS, CULTIVATION, CONSTRUCTION } },
            { OBJECTIVES, new[] { PLAYER, PROGRESSION } },
            { EVENTS, new[] { PLAYER, TIME } },
            { ANALYTICS, new[] { PLAYER, TIME } },
            { UI_STATE, new[] { PLAYER, SETTINGS } },
            { TUTORIAL, new[] { PLAYER, PROGRESSION } },
            { INVENTORY, new[] { PLAYER, ECONOMY } },
            { MULTIPLAYER, new[] { PLAYER, SETTINGS } },
            { MODDING, new[] { PLAYER, SETTINGS } }
        };

        #endregion

        #region Section Key Utilities

        /// <summary>
        /// Validate that a section key follows naming conventions
        /// </summary>
        /// <param name="sectionKey">Key to validate</param>
        /// <returns>True if key is valid</returns>
        public static bool IsValidSectionKey(string sectionKey)
        {
            if (string.IsNullOrEmpty(sectionKey))
                return false;

            // Check if it's a predefined key
            if (ValidSectionKeys.Contains(sectionKey))
                return true;

            // Check custom key naming convention: lowercase, underscores, alphanumeric
            var customKeyPattern = @"^[a-z][a-z0-9_]*[a-z0-9]$";
            return Regex.IsMatch(sectionKey, customKeyPattern);
        }

        /// <summary>
        /// Get the recommended priority for a section key
        /// </summary>
        /// <param name="sectionKey">Section key</param>
        /// <returns>Priority value (higher = more important)</returns>
        public static int GetSectionPriority(string sectionKey)
        {
            return SavePriorities.TryGetValue(sectionKey, out int priority) ? priority : 50;
        }

        /// <summary>
        /// Check if a section key is required for basic functionality
        /// </summary>
        /// <param name="sectionKey">Section key</param>
        /// <returns>True if required</returns>
        public static bool IsRequiredSection(string sectionKey)
        {
            return RequiredSectionKeys.Contains(sectionKey);
        }

        /// <summary>
        /// Check if a section supports incremental saves
        /// </summary>
        /// <param name="sectionKey">Section key</param>
        /// <returns>True if incremental saves are supported</returns>
        public static bool SupportsIncrementalSave(string sectionKey)
        {
            return IncrementalSaveSupported.Contains(sectionKey);
        }

        /// <summary>
        /// Get the dependencies for a section key
        /// </summary>
        /// <param name="sectionKey">Section key</param>
        /// <returns>Array of dependency section keys</returns>
        public static string[] GetSectionDependencies(string sectionKey)
        {
            return LoadDependencies.TryGetValue(sectionKey, out string[] dependencies) 
                ? dependencies 
                : new string[0];
        }

        /// <summary>
        /// Generate a valid custom section key from a name
        /// </summary>
        /// <param name="name">Descriptive name</param>
        /// <returns>Valid section key</returns>
        public static string GenerateSectionKey(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "custom_section";

            // Convert to lowercase, replace spaces with underscores, remove invalid chars
            string key = name.ToLowerInvariant()
                            .Replace(" ", "_")
                            .Replace("-", "_");

            // Remove non-alphanumeric characters except underscores
            key = Regex.Replace(key, @"[^a-z0-9_]", "");

            // Ensure it doesn't start with a number or underscore
            if (key.Length > 0 && (char.IsDigit(key[0]) || key[0] == '_'))
            {
                key = "section_" + key;
            }

            // Ensure it doesn't end with an underscore
            key = key.TrimEnd('_');

            // Ensure minimum length
            if (key.Length < 2)
            {
                key = "custom_section";
            }

            return key;
        }

        /// <summary>
        /// Get human-readable name for a section key
        /// </summary>
        /// <param name="sectionKey">Section key</param>
        /// <returns>Display name</returns>
        public static string GetSectionDisplayName(string sectionKey)
        {
            var displayNames = new Dictionary<string, string>
            {
                { CULTIVATION, "Plant Cultivation" },
                { CONSTRUCTION, "Facility Construction" },
                { ECONOMY, "Economy & Trading" },
                { PROGRESSION, "Player Progression" },
                { UI_STATE, "Interface Settings" },
                { ENVIRONMENT, "Environmental Systems" },
                { SETTINGS, "Game Settings" },
                { PLAYER, "Player Profile" },
                { TIME, "Time & Scheduling" },
                { ANALYTICS, "Analytics Data" },
                { EVENTS, "Game Events" },
                { AI_SYSTEMS, "AI & Automation" },
                { TUTORIAL, "Tutorial Progress" },
                { MULTIPLAYER, "Multiplayer Data" },
                { MODDING, "Mod Support" },
                { INVENTORY, "Inventory Management" },
                { OBJECTIVES, "Objectives & Quests" },
                { RANDOM_EVENTS, "Dynamic Events" }
            };

            if (displayNames.TryGetValue(sectionKey, out string displayName))
                return displayName;

            // Generate display name from key
            return sectionKey.Replace("_", " ")
                            .Split(' ')
                            .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1))
                            .Aggregate((a, b) => a + " " + b);
        }

        #endregion
    }
}