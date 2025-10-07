using UnityEngine;
using ProjectChimera.Systems.Services.Core;
using ProjectChimera.Core;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.Commands
{
    /// <summary>
    /// Genetics pillar commands for contextual menu integration
    /// Implements command pattern for breeding, research, and genetic operations
    /// </summary>

    /// <summary>
    /// Base class for all genetics commands
    /// </summary>
    public abstract class GeneticsCommand : IMenuCommand
    {
        protected IGeneticsService _geneticsService;
        protected IProgressionManager _progressionManager;

        public abstract string CommandId { get; }
        public abstract string DisplayName { get; }

        public GeneticsCommand(IGeneticsService geneticsService, IProgressionManager progressionManager)
        {
            _geneticsService = geneticsService ?? throw new System.ArgumentNullException(nameof(geneticsService));
            _progressionManager = progressionManager ?? throw new System.ArgumentNullException(nameof(progressionManager));
        }

        public abstract bool CanExecute();
        public abstract CommandResult Execute();
        public abstract CommandResult Undo();
    }

    /// <summary>
    /// Command for breeding two plants
    /// </summary>
    public class BreedPlantsCommand : GeneticsCommand
    {
        private readonly string _parentId1;
        private readonly string _parentId2;
        private string _newStrainId = null;

        public override string CommandId => $"breed_plants_{_parentId1}_{_parentId2}";
        public override string DisplayName => "Breed Plants";

        public BreedPlantsCommand(string parentId1, string parentId2,
            IGeneticsService geneticsService, IProgressionManager progressionManager)
            : base(geneticsService, progressionManager)
        {
            _parentId1 = parentId1;
            _parentId2 = parentId2;
        }

        public override bool CanExecute()
        {
            return _geneticsService.CanBreedPlants(_parentId1, _parentId2) &&
                   _progressionManager.IsSkillUnlocked("breeding_basic");
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Cannot breed these plants - check breeding requirements and skills");
                }

                // Use integrated breeding system for enhanced genetics
                var breedingSystem = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Systems.Genetics.BreedingSystemIntegration>();
                if (breedingSystem != null)
                {
                    var breedingResult = breedingSystem.BreedPlants(_parentId1, _parentId2);
                    if (breedingResult.Success)
                    {
                        _newStrainId = breedingResult.SeedId;
                        _progressionManager.AddExperience(50f, "Breeding");
                        
                        string traitInfo = "";
                        if (breedingResult.PredictedTraits != null && breedingResult.PredictedTraits.Length > 0)
                        {
                            traitInfo = $" (Predicted traits: {breedingResult.PredictedTraits.Length} analyzed)";
                        }
                        
                        return CommandResult.Success($"Successfully bred new strain: {_newStrainId}{traitInfo}");
                    }
                    else
                    {
                        return CommandResult.Failure($"Breeding failed: {breedingResult.ErrorMessage}");
                    }
                }
                else
                {
                    // Fallback to basic service
                    var success = _geneticsService.BreedPlants(_parentId1, _parentId2, out _newStrainId);
                    if (success)
                    {
                        _progressionManager.AddExperience(50f, "Breeding");
                        return CommandResult.Success($"Successfully bred new strain: {_newStrainId}");
                    }
                    else
                    {
                        return CommandResult.Failure("Breeding attempt failed");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("GENETICS", "Genetics command operation", null);
                return CommandResult.Failure($"Error breeding plants: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Cannot undo breeding - new strain is created
            return CommandResult.Failure("Cannot undo breeding action");
        }
    }

    /// <summary>
    /// Command for creating tissue culture
    /// </summary>
    public class CreateTissueCultureCommand : GeneticsCommand
    {
        private readonly string _plantId;
        private readonly string _cultureName;
        private bool _wasCreated = false;

        public override string CommandId => $"create_tissue_culture_{_plantId}";
        public override string DisplayName => "Create Tissue Culture";

        public CreateTissueCultureCommand(string plantId, string cultureName,
            IGeneticsService geneticsService, IProgressionManager progressionManager)
            : base(geneticsService, progressionManager)
        {
            _plantId = plantId;
            _cultureName = cultureName;
        }

        public override bool CanExecute()
        {
            return _geneticsService.CanCreateTissueCulture(_plantId) &&
                   _progressionManager.IsSkillUnlocked("tissue_culture");
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Cannot create tissue culture - check requirements and skills");
                }

                // Use integrated breeding system for enhanced tissue culture
                var breedingSystem = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Systems.Genetics.BreedingSystemIntegration>();
                if (breedingSystem != null)
                {
                    _wasCreated = breedingSystem.CreateTissueCulture(_plantId, _cultureName);
                    if (_wasCreated)
                    {
                        _progressionManager.AddExperience(30f, "Tissue Culture");
                        return CommandResult.Success($"Successfully created tissue culture: {_cultureName}");
                    }
                    else
                    {
                        return CommandResult.Failure("Failed to create tissue culture - check plant viability");
                    }
                }
                else
                {
                    // Fallback to basic service
                    _wasCreated = _geneticsService.CreateTissueCulture(_plantId, _cultureName);
                    if (_wasCreated)
                    {
                        _progressionManager.AddExperience(30f, "Tissue Culture");
                        return CommandResult.Success($"Successfully created tissue culture: {_cultureName}");
                    }
                    else
                    {
                        return CommandResult.Failure("Failed to create tissue culture");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("GENETICS", "Genetics command operation", null);
                return CommandResult.Failure($"Error creating tissue culture: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Tissue culture creation cannot be easily undone
            return CommandResult.Failure("Cannot undo tissue culture creation");
        }
    }

    /// <summary>
    /// Command for micropropagation
    /// </summary>
    public class MicropropagateCommand : GeneticsCommand
    {
        private readonly string _cultureId;
        private readonly int _quantity;
        private string[] _seedIds = null;

        public override string CommandId => $"micropropagate_{_cultureId}";
        public override string DisplayName => "Micropropagate";

        public MicropropagateCommand(string cultureId, int quantity,
            IGeneticsService geneticsService, IProgressionManager progressionManager)
            : base(geneticsService, progressionManager)
        {
            _cultureId = cultureId;
            _quantity = quantity;
        }

        public override bool CanExecute()
        {
            return _geneticsService.CanMicropropagate(_cultureId) &&
                   _progressionManager.IsSkillUnlocked("micropropagation");
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Cannot micropropagate - check requirements and skills");
                }

                // Use integrated breeding system for enhanced micropropagation
                var breedingSystem = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Systems.Genetics.BreedingSystemIntegration>();
                if (breedingSystem != null)
                {
                    var success = breedingSystem.Micropropagate(_cultureId, _quantity, out _seedIds);
                    if (success)
                    {
                        _progressionManager.AddExperience(20f * _quantity, "Micropropagation");
                        return CommandResult.Success($"Successfully micropropagated {_quantity} clones from culture {_cultureId}");
                    }
                    else
                    {
                        return CommandResult.Failure("Micropropagation failed - check culture viability");
                    }
                }
                else
                {
                    // Fallback to basic service
                    var success = _geneticsService.Micropropagate(_cultureId, _quantity, out _seedIds);
                    if (success)
                    {
                        _progressionManager.AddExperience(20f * _quantity, "Micropropagation");
                        return CommandResult.Success($"Successfully micropropagated {_quantity} clones");
                    }
                    else
                    {
                        return CommandResult.Failure("Micropropagation failed");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("GENETICS", "Genetics command operation", null);
                return CommandResult.Failure($"Error micropropagating: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Micropropagation cannot be undone
            return CommandResult.Failure("Cannot undo micropropagation");
        }
    }

    /// <summary>
    /// Command for purchasing seeds from seed bank
    /// </summary>
    public class PurchaseSeedsCommand : GeneticsCommand
    {
        private readonly string _strainId;
        private readonly int _quantity;
        private bool _wasPurchased = false;

        public override string CommandId => $"purchase_seeds_{_strainId}";
        public override string DisplayName => $"Purchase {_strainId} Seeds";

        public PurchaseSeedsCommand(string strainId, int quantity,
            IGeneticsService geneticsService, IProgressionManager progressionManager)
            : base(geneticsService, progressionManager)
        {
            _strainId = strainId;
            _quantity = quantity;
        }

        public override bool CanExecute()
        {
            return _geneticsService.IsStrainUnlocked(_strainId) &&
                   _geneticsService.CanAffordSeeds(_strainId, _quantity);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Cannot purchase seeds - check availability and resources");
                }

                _wasPurchased = _geneticsService.PurchaseSeeds(_strainId, _quantity);
                if (_wasPurchased)
                {
                    return CommandResult.Success($"Successfully purchased {_quantity} {_strainId} seeds");
                }
                else
                {
                    return CommandResult.Failure("Failed to purchase seeds");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("GENETICS", "Genetics command operation", null);
                return CommandResult.Failure($"Error purchasing seeds: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Cannot undo seed purchase easily
            return CommandResult.Failure("Cannot undo seed purchase");
        }
    }

    /// <summary>
    /// Command for researching genetic traits
    /// </summary>
    public class ResearchTraitCommand : GeneticsCommand
    {
        private readonly string _traitId;
        private bool _wasResearched = false;

        public override string CommandId => $"research_trait_{_traitId}";
        public override string DisplayName => $"Research {_traitId} Trait";

        public ResearchTraitCommand(string traitId,
            IGeneticsService geneticsService, IProgressionManager progressionManager)
            : base(geneticsService, progressionManager)
        {
            _traitId = traitId;
        }

        public override bool CanExecute()
        {
            return _geneticsService.CanResearchTrait(_traitId) &&
                   _progressionManager.IsSkillUnlocked("genetic_research") &&
                   _progressionManager.SkillPoints >= 10; // Research costs skill points
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Cannot research trait - check requirements, skills, and skill points");
                }

                _progressionManager.SpendSkillPoints(10, $"Research {_traitId} trait");
                _wasResearched = _geneticsService.ResearchTrait(_traitId);

                if (_wasResearched)
                {
                    _progressionManager.AddExperience(100f, "Research");
                    return CommandResult.Success($"Successfully researched {_traitId} trait");
                }
                else
                {
                    // Refund skill points if research failed
                    _progressionManager.AddSkillPoints(10, "Refund: Research failed");
                    return CommandResult.Failure("Research failed");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("GENETICS", "Genetics command operation", null);
                return CommandResult.Failure($"Error researching trait: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Research cannot be undone - knowledge is gained
            return CommandResult.Failure("Cannot undo research");
        }
    }

    /// <summary>
    /// Command for selecting strain from seed bank for planting
    /// </summary>
    public class SelectStrainCommand : GeneticsCommand
    {
        private readonly string _strainId;

        public override string CommandId => $"select_strain_{_strainId}";
        public override string DisplayName => $"Select {_strainId} Strain";

        public SelectStrainCommand(string strainId,
            IGeneticsService geneticsService, IProgressionManager progressionManager)
            : base(geneticsService, progressionManager)
        {
            _strainId = strainId;
        }

        public override bool CanExecute()
        {
            return _geneticsService.HasStrain(_strainId) && _geneticsService.HasSeeds(_strainId);
        }

        public override CommandResult Execute()
        {
            try
            {
                if (!CanExecute())
                {
                    return CommandResult.Failure("Strain not available or no seeds in inventory");
                }

                // This command just selects the strain for planting
                // The actual planting would be handled by cultivation commands
                return CommandResult.Success($"Selected {_strainId} strain for planting");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("GENETICS", "Genetics command operation", null);
                return CommandResult.Failure($"Error selecting strain: {ex.Message}");
            }
        }

        public override CommandResult Undo()
        {
            // Selection can be undone by selecting a different strain
            return CommandResult.Success("Strain selection cleared");
        }
    }
}