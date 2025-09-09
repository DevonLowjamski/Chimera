namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// DEPRECATED: Main Save/Load Panel has been broken down into focused components.
    /// This file now serves as a reference point for the decomposed UI panel structure.
    /// 
    /// New Component Structure:
    /// - SaveLoadPanelCore.cs: Core panel infrastructure, tab system, and state management
    /// - SaveTabUIBuilder.cs: Save tab UI creation and management
    /// - LoadTabUIBuilder.cs: Load tab UI creation and slot management
    /// - SaveLoadOperationHandler.cs: Save/load operation logic and event handling
    /// - SaveSlotUIRenderer.cs: Save slot rendering and interaction management
    /// </summary>
    
    // The SaveLoadPanel functionality has been moved to focused component files.
    // This file is kept for reference and to prevent breaking changes during migration.
    // 
    // To use the new component structure, inherit from SaveLoadPanelCore:
    // 
    // public class SaveLoadPanel : SaveLoadPanelCore
    // {
    //     // Your custom save/load panel implementation
    // }
    // 
    // The following classes are now available in their focused components:
    // 
    // From SaveLoadPanelCore.cs:
    // - SaveLoadPanelCore (base class with core functionality)
    // - ITickable implementation for auto-refresh
    // - Tab system management
    // 
    // From SaveTabUIBuilder.cs:
    // - SaveTabUIBuilder (save form creation and management)
    // - Save button handling and status display
    // 
    // From LoadTabUIBuilder.cs:
    // - LoadTabUIBuilder (load slot list and details panel)
    // - Save slot selection and detail display
    // 
    // From SaveLoadOperationHandler.cs:
    // - SaveLoadOperationHandler (async save/load operations)
    // - Event handling and operation state management
    // 
    // From SaveSlotUIRenderer.cs:
    // - SaveSlotUIRenderer (individual save slot UI creation)
    // - Hover effects and interaction handling
    
    /// <summary>
    /// Concrete implementation of SaveLoadPanel using the new component structure.
    /// Inherits all functionality from SaveLoadPanelCore.
    /// </summary>
    public class SaveLoadPanel : SaveLoadPanelCore
    {
        // This class inherits all functionality from SaveLoadPanelCore
        // Add any custom SaveLoadPanel-specific functionality here if needed
    }
}