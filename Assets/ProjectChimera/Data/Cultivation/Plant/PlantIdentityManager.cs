using UnityEngine;
using ProjectChimera.Data.Genetics;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Identity Manager
    /// Single Responsibility: Plant identification, strain management, and genetic data handling
    /// Extracted from PlantInstanceSO for better separation of concerns
    /// </summary>
    [System.Serializable]
    public class PlantIdentityManager
    {
        [Header("Identity Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _validateGenetics = true;
        [SerializeField] private bool _autoGenerateIDs = true;

        // Identity data
        [SerializeField] private string _plantID = "";
        [SerializeField] private string _plantName = "";
        [SerializeField] private PlantStrainSO _strain;
        [SerializeField] private GenotypeDataSO _genotype;
        [SerializeField] private DateTime _creationDate = DateTime.Now;
        [SerializeField] private string _parentPlantID = "";
        [SerializeField] private int _generationNumber = 1;

        // Identity validation
        [SerializeField] private bool _isValidated = false;
        [SerializeField] private string _validationErrors = "";

        // Statistics
        private PlantIdentityStats _stats = new PlantIdentityStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<string, string> OnIdentityChanged; // old ID, new ID
        public event System.Action<PlantStrainSO, PlantStrainSO> OnStrainChanged; // old strain, new strain
        public event System.Action<GenotypeDataSO, GenotypeDataSO> OnGenotypeChanged; // old genotype, new genotype
        public event System.Action<string> OnValidationFailed;

        public bool IsInitialized => _isInitialized;
        public PlantIdentityStats Stats => _stats;
        public string PlantID => _plantID;
        public string PlantName => _plantName;
        public PlantStrainSO Strain => _strain;
        public GenotypeDataSO Genotype => _genotype;
        public DateTime CreationDate => _creationDate;
        public string ParentPlantID => _parentPlantID;
        public int GenerationNumber => _generationNumber;
        public bool IsValidated => _isValidated;

        public void Initialize()
        {
            if (_isInitialized) return;

            if (_autoGenerateIDs && string.IsNullOrEmpty(_plantID))
            {
                GenerateUniqueID();
            }

            ValidateIdentity();
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Plant Identity Manager initialized for {_plantID}");
            }
        }

        /// <summary>
        /// Set plant identity information
        /// </summary>
        public bool SetIdentity(string plantID, string plantName, PlantStrainSO strain, GenotypeDataSO genotype)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            var oldID = _plantID;
            var oldStrain = _strain;
            var oldGenotype = _genotype;

            _plantID = plantID;
            _plantName = plantName;
            _strain = strain;
            _genotype = genotype;

            ValidateIdentity();

            if (_isValidated)
            {
                _stats.IdentityChanges++;
                OnIdentityChanged?.Invoke(oldID, _plantID);

                if (oldStrain != strain)
                {
                    OnStrainChanged?.Invoke(oldStrain, strain);
                }

                if (oldGenotype != genotype)
                {
                    OnGenotypeChanged?.Invoke(oldGenotype, genotype);
                }

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Plant identity updated: {_plantID} ({_plantName})");
                }

                return true;
            }
            else
            {
                // Revert changes if validation failed
                _plantID = oldID;
                _strain = oldStrain;
                _genotype = oldGenotype;

                OnValidationFailed?.Invoke(_validationErrors);

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", $"Plant identity validation failed: {_validationErrors}");
                }

                return false;
            }
        }

        /// <summary>
        /// Update plant name
        /// </summary>
        public bool UpdatePlantName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", "Cannot set empty plant name");
                }
                return false;
            }

            var oldName = _plantName;
            _plantName = newName;
            _stats.NameChanges++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Plant name updated: {oldName} -> {_plantName}");
            }

            return true;
        }

        /// <summary>
        /// Update strain information
        /// </summary>
        public bool UpdateStrain(PlantStrainSO newStrain)
        {
            if (newStrain == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", "Cannot set null strain");
                }
                return false;
            }

            var oldStrain = _strain;
            _strain = newStrain;

            ValidateGenetics();

            if (_isValidated)
            {
                _stats.StrainChanges++;
                OnStrainChanged?.Invoke(oldStrain, newStrain);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Plant strain updated");
                }

                return true;
            }
            else
            {
                // Revert if validation failed
                _strain = oldStrain;
                OnValidationFailed?.Invoke(_validationErrors);
                return false;
            }
        }

        /// <summary>
        /// Update genotype information
        /// </summary>
        public bool UpdateGenotype(GenotypeDataSO newGenotype)
        {
            if (newGenotype == null)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", "Cannot set null genotype");
                }
                return false;
            }

            var oldGenotype = _genotype;
            _genotype = newGenotype;

            ValidateGenetics();

            if (_isValidated)
            {
                _stats.GenotypeChanges++;
                OnGenotypeChanged?.Invoke(oldGenotype, newGenotype);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Plant genotype updated");
                }

                return true;
            }
            else
            {
                // Revert if validation failed
                _genotype = oldGenotype;
                OnValidationFailed?.Invoke(_validationErrors);
                return false;
            }
        }

        /// <summary>
        /// Set parent plant information for breeding tracking
        /// </summary>
        public void SetParentInfo(string parentPlantID, int generationNumber)
        {
            _parentPlantID = parentPlantID;
            _generationNumber = Mathf.Max(1, generationNumber);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Plant lineage set: Parent={_parentPlantID}, Generation={_generationNumber}");
            }
        }

        /// <summary>
        /// Generate unique plant ID
        /// </summary>
        public string GenerateUniqueID()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var random = UnityEngine.Random.Range(1000, 9999);

            var strainCode = "UNK";

            _plantID = $"{strainCode}_{timestamp}_{random}";
            _stats.IDsGenerated++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Generated unique plant ID: {_plantID}");
            }

            return _plantID;
        }

        /// <summary>
        /// Validate plant identity integrity
        /// </summary>
        public bool ValidateIdentity()
        {
            _validationErrors = "";
            _isValidated = true;

            // Check required fields
            if (string.IsNullOrEmpty(_plantID))
            {
                _validationErrors += "Plant ID is required. ";
                _isValidated = false;
            }

            if (string.IsNullOrEmpty(_plantName))
            {
                _validationErrors += "Plant name is required. ";
                _isValidated = false;
            }

            // Validate strain and genotype compatibility if enabled
            if (_validateGenetics && !ValidateGenetics())
            {
                _isValidated = false;
            }

            _stats.ValidationAttempts++;
            if (_isValidated)
            {
                _stats.ValidationSuccesses++;
            }
            else
            {
                _stats.ValidationFailures++;
            }

            return _isValidated;
        }

        /// <summary>
        /// Validate genetic compatibility
        /// </summary>
        private bool ValidateGenetics()
        {
            if (!_validateGenetics) return true;

            if (_strain == null)
            {
                _validationErrors += "Strain is required when genetic validation is enabled. ";
                return false;
            }

            if (_genotype == null)
            {
                _validationErrors += "Genotype is required when genetic validation is enabled. ";
                return false;
            }

            // Additional genetic compatibility checks could go here
            // For now, we just ensure both are present

            return true;
        }

        /// <summary>
        /// Get plant identity summary
        /// </summary>
        public PlantIdentitySummary GetIdentitySummary()
        {
            return new PlantIdentitySummary
            {
                PlantID = _plantID,
                PlantName = _plantName,
                StrainName = "Unknown",
                GenotypeName = "Unknown",
                CreationDate = _creationDate,
                ParentPlantID = _parentPlantID,
                GenerationNumber = _generationNumber,
                IsValidated = _isValidated,
                ValidationErrors = _validationErrors
            };
        }

        /// <summary>
        /// Clone identity for breeding purposes
        /// </summary>
        public PlantIdentityManager CloneForBreeding(string newPlantID, string newPlantName)
        {
            var clone = new PlantIdentityManager();
            clone._plantID = newPlantID;
            clone._plantName = newPlantName;
            clone._strain = _strain; // Same strain
            clone._genotype = _genotype; // Same genotype for clones
            clone._creationDate = DateTime.Now;
            clone._parentPlantID = _plantID; // Current plant becomes parent
            clone._generationNumber = _generationNumber + 1;
            clone._enableLogging = _enableLogging;
            clone._validateGenetics = _validateGenetics;
            clone._autoGenerateIDs = _autoGenerateIDs;

            clone.Initialize();
            clone.ValidateIdentity();

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Cloned plant identity: {_plantID} -> {newPlantID}");
            }

            return clone;
        }

        /// <summary>
        /// Set identity validation parameters
        /// </summary>
        public void SetValidationParameters(bool validateGenetics, bool autoGenerateIDs)
        {
            _validateGenetics = validateGenetics;
            _autoGenerateIDs = autoGenerateIDs;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Validation parameters updated: Genetics={validateGenetics}, AutoID={autoGenerateIDs}");
            }
        }

        /// <summary>
        /// Reset identity statistics
        /// </summary>
        [ContextMenu("Reset Identity Statistics")]
        public void ResetStats()
        {
            _stats = new PlantIdentityStats();

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant identity statistics reset");
            }
        }
    }

    /// <summary>
    /// Plant identity statistics
    /// </summary>
    [System.Serializable]
    public struct PlantIdentityStats
    {
        public int IdentityChanges;
        public int NameChanges;
        public int StrainChanges;
        public int GenotypeChanges;
        public int ValidationAttempts;
        public int ValidationSuccesses;
        public int ValidationFailures;
        public int IDsGenerated;
    }

    /// <summary>
    /// Plant identity summary
    /// </summary>
    [System.Serializable]
    public struct PlantIdentitySummary
    {
        public string PlantID;
        public string PlantName;
        public string StrainName;
        public string GenotypeName;
        public DateTime CreationDate;
        public string ParentPlantID;
        public int GenerationNumber;
        public bool IsValidated;
        public string ValidationErrors;
    }
}
