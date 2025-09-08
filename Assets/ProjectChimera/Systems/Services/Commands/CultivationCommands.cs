using UnityEngine;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.Commands
{
    /// <summary>
    /// Cultivation pillar commands for contextual menu integration
    /// Implements command pattern for plant care and cultivation operations
    /// </summary>

    /// <summary>
    /// Base class for all cultivation commands
    /// </summary>
    public abstract class CultivationCommand : IMenuCommand
    {
        protected ICultivationService _cultivationService;
        protected IEconomyManager _economyManager;

        public abstract string CommandId { get; }
        public abstract string DisplayName { get; }

        public CultivationCommand(ICultivationService cultivationService, IEconomyManager economyManager)
        {
            _cultivationService = cultivationService ?? throw new System.ArgumentNullException(nameof(cultivationService));
            _economyManager = economyManager ?? throw new System.ArgumentNullException(nameof(economyManager));
        }

        public abstract bool CanExecute();
        public abstract CommandResult Execute();
        public abstract CommandResult Undo();
    }

    /// <summary>
    /// Command for planting seeds
    /// </summary>
    public class PlantSeedCommand : CultivationCommand
    {
        private readonly string _strainId;
        private readonly Vector3Int _gridPosition;
        private readonly string _plantName;
        private string _plantedPlantId = null;

        public override string CommandId => $"plant_seed_{_strainId}";
        public override string DisplayName => $"Plant {_strainId} Seed";

        public PlantSeedCommand(string strainId, Vector3Int gridPosition, string plantName,
            ICultivationService cultivationService, IEconomyManager economyManager)
            : base(cultivationService, economyManager)
        {
            _strainId = strainId;
            _gridPosition = gridPosition;
            _plantName = plantName;
        }

        public override bool CanExecute()
        {
            return _cultivationService.CanPlantSeed(_strainId, _gridPosition);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Cannot plant seed at this position");
                }

                var success = _cultivationService.PlantSeed(_strainId, _gridPosition, _plantName);
                if (success)
                {
                    _plantedPlantId = $"plant_{_gridPosition.x}_{_gridPosition.y}_{_gridPosition.z}"; // Simplified ID generation
                    return CommandResult.Success($"Successfully planted {_strainId} seed");
                }
                else
                {
                    return CommandResult.Failure("Failed to plant seed");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[PlantSeedCommand] Error executing command: {ex.Message}");
                return CommandResult.Failure($"Error planting seed: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            if (!string.IsNullOrEmpty(_plantedPlantId))
            {
                var success = _cultivationService.RemovePlant(_plantedPlantId);
                if (success)
                {
                    _plantedPlantId = null;
                    return CommandResult.Success("Removed planted seed");
                }
                return CommandResult.Failure("Failed to remove plant");
            }
            return CommandResult.Failure("No plant to remove");
        }
    }

    /// <summary>
    /// Command for watering plants
    /// </summary>
    public class WaterPlantCommand : CultivationCommand
    {
        private readonly string _plantId;

        public override string CommandId => $"water_plant_{_plantId}";
        public override string DisplayName => "Water Plant";

        public WaterPlantCommand(string plantId, ICultivationService cultivationService, IEconomyManager economyManager)
            : base(cultivationService, economyManager)
        {
            _plantId = plantId;
        }

        public override bool CanExecute()
        {
            return !string.IsNullOrEmpty(_plantId);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Invalid plant ID");
                }

                var success = _cultivationService.WaterPlant(_plantId);
                if (success)
                {
                    return CommandResult.Success("Plant watered successfully");
                }
                else
                {
                    return CommandResult.Failure("Failed to water plant - plant may not need water");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[WaterPlantCommand] Error executing command: {ex.Message}");
                return CommandResult.Failure($"Error watering plant: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Cannot undo watering
            return CommandResult.Failure("Cannot undo watering action");
        }
    }

    /// <summary>
    /// Command for feeding plants
    /// </summary>
    public class FeedPlantCommand : CultivationCommand
    {
        private readonly string _plantId;

        public override string CommandId => $"feed_plant_{_plantId}";
        public override string DisplayName => "Feed Plant";

        public FeedPlantCommand(string plantId, ICultivationService cultivationService, IEconomyManager economyManager)
            : base(cultivationService, economyManager)
        {
            _plantId = plantId;
        }

        public override bool CanExecute()
        {
            return !string.IsNullOrEmpty(_plantId);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Invalid plant ID");
                }

                var success = _cultivationService.FeedPlant(_plantId);
                if (success)
                {
                    return CommandResult.Success("Plant fed successfully");
                }
                else
                {
                    return CommandResult.Failure("Failed to feed plant - plant may not need nutrients");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[FeedPlantCommand] Error executing command: {ex.Message}");
                return CommandResult.Failure($"Error feeding plant: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Cannot undo feeding
            return CommandResult.Failure("Cannot undo feeding action");
        }
    }

    /// <summary>
    /// Command for training plants (LST, topping, etc.)
    /// </summary>
    public class TrainPlantCommand : CultivationCommand
    {
        private readonly string _plantId;
        private readonly string _trainingType;

        public override string CommandId => $"train_plant_{_plantId}_{_trainingType}";
        public override string DisplayName => $"Train Plant ({_trainingType})";

        public TrainPlantCommand(string plantId, string trainingType, 
            ICultivationService cultivationService, IEconomyManager economyManager)
            : base(cultivationService, economyManager)
        {
            _plantId = plantId;
            _trainingType = trainingType;
        }

        public override bool CanExecute()
        {
            return !string.IsNullOrEmpty(_plantId) && !string.IsNullOrEmpty(_trainingType);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Invalid plant ID or training type");
                }

                var success = _cultivationService.TrainPlant(_plantId, _trainingType);
                if (success)
                {
                    return CommandResult.Success($"Plant trained with {_trainingType} technique");
                }
                else
                {
                    return CommandResult.Failure("Failed to train plant - plant may not be ready for this technique");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[TrainPlantCommand] Error executing command: {ex.Message}");
                return CommandResult.Failure($"Error training plant: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Training cannot be undone easily
            return CommandResult.Failure("Cannot undo plant training");
        }
    }

    /// <summary>
    /// Command for harvesting plants
    /// </summary>
    public class HarvestPlantCommand : CultivationCommand
    {
        private readonly string _plantId;
        private bool _wasHarvested = false;

        public override string CommandId => $"harvest_plant_{_plantId}";
        public override string DisplayName => "Harvest Plant";

        public HarvestPlantCommand(string plantId, ICultivationService cultivationService, IEconomyManager economyManager)
            : base(cultivationService, economyManager)
        {
            _plantId = plantId;
        }

        public override bool CanExecute()
        {
            return !string.IsNullOrEmpty(_plantId);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Invalid plant ID");
                }

                _wasHarvested = _cultivationService.HarvestPlant(_plantId);
                if (_wasHarvested)
                {
                    return CommandResult.Success("Plant harvested successfully");
                }
                else
                {
                    return CommandResult.Failure("Failed to harvest plant - plant may not be ready");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[HarvestPlantCommand] Error executing command: {ex.Message}");
                return CommandResult.Failure($"Error harvesting plant: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Cannot undo harvesting
            return CommandResult.Failure("Cannot undo harvest action");
        }
    }

    /// <summary>
    /// Command for adjusting environmental conditions
    /// </summary>
    public class AdjustEnvironmentCommand : CultivationCommand
    {
        private readonly string _zoneId;
        private readonly EnvironmentalConditions _newConditions;
        private EnvironmentalConditions _previousConditions;
        private bool _wasAdjusted = false;

        public override string CommandId => $"adjust_environment_{_zoneId}";
        public override string DisplayName => "Adjust Environment";

        public AdjustEnvironmentCommand(string zoneId, EnvironmentalConditions newConditions,
            ICultivationService cultivationService, IEconomyManager economyManager)
            : base(cultivationService, economyManager)
        {
            _zoneId = zoneId;
            _newConditions = newConditions;
        }

        public override bool CanExecute()
        {
            return !string.IsNullOrEmpty(_zoneId) && _cultivationService.CanAdjustEnvironment(_zoneId);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Cannot adjust environment for this zone");
                }

                _previousConditions = _cultivationService.GetEnvironmentalConditions(_zoneId);
                _wasAdjusted = _cultivationService.SetEnvironmentalConditions(_zoneId, _newConditions);

                if (_wasAdjusted)
                {
                    return CommandResult.Success("Environmental conditions adjusted successfully");
                }
                else
                {
                    return CommandResult.Failure("Failed to adjust environmental conditions");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[AdjustEnvironmentCommand] Error executing command: {ex.Message}");
                return CommandResult.Failure($"Error adjusting environment: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            if (_wasAdjusted)
            {
                var success = _cultivationService.SetEnvironmentalConditions(_zoneId, _previousConditions);
                if (success)
                {
                    _wasAdjusted = false;
                    return CommandResult.Success("Environmental conditions reverted");
                }
                return CommandResult.Failure("Failed to revert environmental conditions");
            }
            return CommandResult.Failure("No environmental changes to revert");
        }
    }
}