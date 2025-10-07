
using UnityEngine;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Plant species ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "PlantSpecies", menuName = "Project Chimera/Genetics/Plant Species")]
    public class PlantSpeciesSO : ScriptableObject
    {
        [Header("Species Information")]
        public string speciesName;
        public string scientificName;
        public string description;

        // Compatibility property for code expecting StrainName
        public string StrainName => speciesName;

        [Header("Genetic Properties")]
        public int chromosomeCount = 20;
        public float averageGenomeSize = 820.0f; // Mbp
        public string geneticMarker;
    }
}
