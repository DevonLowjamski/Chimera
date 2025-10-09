using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Genetics.Blockchain;

namespace ProjectChimera.Core.Interfaces
{
    /// <summary>
    /// Service interface for blockchain-verified genetic breeding system.
    ///
    /// GAMEPLAY PURPOSE:
    /// This service powers the invisible blockchain that makes strain breeding
    /// fun, rewarding, and trustworthy:
    ///
    /// 1. BREEDING: Players breed plants → blockchain records genetics automatically
    /// 2. VERIFICATION: Players see "✅ Verified Strain" badge on authentic genetics
    /// 3. LINEAGE: Players view family tree showing breeding history
    /// 4. MARKETPLACE: Verified strains can be safely traded (can't be forged)
    ///
    /// INVISIBLE BLOCKCHAIN PRINCIPLE:
    /// Players never see technical terms like "hash", "nonce", "proof-of-work".
    /// They just see:
    /// - Breeding progress bar ("Calculating genetics...")
    /// - Verification badge ("✅ Verified")
    /// - Visual lineage tree (parent strains → offspring)
    /// - Generation labels (F1, F2, F3...)
    ///
    /// The blockchain runs silently in the background, making breeding secure and trustworthy.
    /// </summary>
    public interface IBlockchainGeneticsService
    {
        /// <summary>
        /// Breeds two plants and creates a blockchain-verified offspring.
        ///
        /// GAMEPLAY FLOW:
        /// 1. Player selects two parent plants
        /// 2. Player clicks "Breed" button
        /// 3. UI shows "Calculating genetics..." progress bar
        /// 4. This method runs (0.5-2 seconds depending on CPU/GPU)
        /// 5. Offspring genetics calculated + blockchain mining happens invisibly
        /// 6. UI shows "Breeding complete! New strain created."
        /// 7. Player receives offspring with "✅ Verified Strain" badge
        ///
        /// PERFORMANCE TARGET: <1 second on GPU, <2 seconds on CPU (gameplay feels instant)
        ///
        /// Returns: Blockchain-verified genotype for the offspring plant
        /// </summary>
        Task<PlantGenotype> BreedPlantsAsync(
            PlantGenotype parent1,
            PlantGenotype parent2,
            string strainName = null);

        /// <summary>
        /// Verifies that a strain is authentic and hasn't been tampered with.
        ///
        /// GAMEPLAY USE CASES:
        /// 1. MARKETPLACE: Before buying strain, check if it's verified
        ///    - Shows "✅ Verified Strain" or "⚠️ Unverified" badge
        ///
        /// 2. SAVE LOAD: Detect if save file was edited/corrupted
        ///    - If verification fails, warn player about data integrity
        ///
        /// 3. ACHIEVEMENT: Track verified breeding chains
        ///    - "Created 50 verified strains" achievement
        ///
        /// Returns: True if strain is authentic and blockchain-verified
        /// </summary>
        bool VerifyStrainAuthenticity(PlantGenotype genotype);

        /// <summary>
        /// Gets the complete breeding lineage (family tree) for a strain.
        ///
        /// GAMEPLAY USE CASE:
        /// Player clicks "View Lineage" button on a strain.
        /// UI displays visual family tree:
        ///
        ///     OG Kush (purchased)
        ///          ×
        ///    Blue Dream (purchased)
        ///          ↓
        ///     Hybrid F1 (bred 2024-01-15)
        ///          ×
        ///    Sour Diesel (purchased)
        ///          ↓
        ///     Hybrid F2 (bred 2024-02-20) ← Current strain
        ///
        /// Shows: parent names, breeding dates, generation labels
        /// Players can see their breeding achievements and plan future crosses.
        ///
        /// Returns: List of breeding events from genesis → current strain
        /// </summary>
        List<GeneEventPacket> GetStrainLineage(PlantGenotype genotype);

        /// <summary>
        /// Gets generation number for a strain (0 = purchased, 1+ = bred).
        ///
        /// GAMEPLAY: Displayed in strain UI as:
        /// - "Generation: Purchased Seed" (gen 0)
        /// - "Generation: F1" (first breeding)
        /// - "Generation: F2" (second breeding)
        /// - etc.
        ///
        /// Higher generations show player's breeding mastery.
        /// Achievement: "Breed a Generation F10 strain"
        /// </summary>
        int GetGeneration(PlantGenotype genotype);

        /// <summary>
        /// Gets all strains the player has bred (for "My Breeding" UI).
        ///
        /// GAMEPLAY USE CASE:
        /// "My Strains" menu showing:
        /// - All strains player has bred
        /// - Breeding dates
        /// - Verification status
        /// - Generation counts
        ///
        /// Sortable by: date, generation, trait values (THC%, yield, etc.)
        /// </summary>
        List<GeneEventPacket> GetPlayerBreedingHistory();

        /// <summary>
        /// Gets total number of verified breeding events (achievement tracking).
        ///
        /// GAMEPLAY: Achievement milestones
        /// - "10 verified breeds"   → Bronze achievement
        /// - "100 verified breeds"  → Silver achievement
        /// - "1000 verified breeds" → Gold achievement
        /// </summary>
        int GetTotalBreedingCount();

        /// <summary>
        /// Validates the entire blockchain for data integrity.
        ///
        /// GAMEPLAY: Called on game load.
        /// If validation fails → show "Save file corrupted" warning.
        /// If validation succeeds → silent, player never knows it happened.
        ///
        /// Returns: True if blockchain is intact and valid
        /// </summary>
        bool ValidateBlockchain();

        /// <summary>
        /// Registers a genesis (starter) strain in the blockchain.
        ///
        /// GAMEPLAY USE CASE:
        /// When player purchases a seed from marketplace or starts tutorial,
        /// the strain needs to enter the blockchain as a "genesis" (has no parents).
        ///
        /// Example: Player buys "OG Kush" seed
        /// → RegisterGenesisStrain called
        /// → Blockchain records OG Kush as generation 0
        /// → Player can now breed with it
        ///
        /// This ensures even purchased strains are verified and traceable.
        /// </summary>
        void RegisterGenesisStrain(PlantGenotype genotype, string strainName);

        /// <summary>
        /// Gets blockchain verification data for UI display.
        ///
        /// GAMEPLAY: Powers the strain info panel:
        /// - Blockchain ID: "a3f5...d8f1" (short hash)
        /// - Verification: "✅ Verified"
        /// - Generation: "F2"
        /// - Bred on: "2024-10-08"
        /// - Bred by: "PlayerName" (for marketplace listings)
        ///
        /// Returns structured data for UI rendering.
        /// </summary>
        BlockchainVerificationInfo GetVerificationInfo(PlantGenotype genotype);
    }

    /// <summary>
    /// Data structure for displaying blockchain verification info in UI.
    /// Contains player-friendly descriptions (no technical jargon).
    /// </summary>
    public struct BlockchainVerificationInfo
    {
        public bool IsVerified;              // ✅ or ⚠️
        public string ShortHash;             // "a3f5...d8f1" (for display)
        public string FullHash;              // Full hash (for advanced users/debugging)
        public int Generation;               // 0, 1, 2, 3... (F0, F1, F2, F3...)
        public string GenerationLabel;       // "Purchased Seed", "F1", "F2", etc.
        public string BreedingDate;          // "2024-10-08" (human-readable)
        public string BreederName;           // "PlayerName" (for marketplace)
        public string StrainName;            // "Blue Dream F2"
        public int LineageDepth;             // How many ancestors (for "view full lineage" button)
        public bool HasLineage;              // True if player can view family tree

        /// <summary>
        /// Gets a player-friendly verification status message.
        /// </summary>
        public string GetStatusMessage()
        {
            if (!IsVerified)
                return "⚠️ Unverified Strain (may not be tradeable)";

            if (Generation == 0)
                return "✅ Verified Seed (Purchased)";

            return $"✅ Verified Breeding (Generation {GenerationLabel})";
        }
    }
}
