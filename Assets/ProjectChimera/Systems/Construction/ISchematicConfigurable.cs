using ProjectChimera.Data.Construction;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Interface for objects that can be configured when instantiated from schematics.
    /// Allows objects to apply custom properties and configuration from SchematicItem data.
    /// </summary>
    public interface ISchematicConfigurable
    {
        /// <summary>
        /// Apply configuration from a schematic item to this object
        /// </summary>
        /// <param name="item">The schematic item containing configuration data</param>
        void ApplySchematicConfiguration(SchematicItem item);
    }
}