using UnityEngine;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Plant strain ScriptableObject for genetics namespace
    /// </summary>
    [CreateAssetMenu(fileName = "GeneticsPlantStrain", menuName = "Project Chimera/Genetics/Plant Strain")]
    public class PlantStrainSO : ScriptableObject
    {
        [Header("Basic Information")]
        public string strainName;
        public string StrainName
        {
            get => strainName;
            set => strainName = value;
        }
        public string description;

        [Header("Genetic Properties")]
        public float geneticDiversity = 1.0f;
        public float mutationRate = 0.01f;

                [Header("Quality Metrics")]
        [SerializeField] private float _thcContent = 15.0f;
        [SerializeField] private float _cbdContent = 1.0f;
        [SerializeField] private float _baseYield = 100.0f;
        [SerializeField] private float _baseFloweringTime = 60.0f;
        [SerializeField] private string _baseSpecies = "Cannabis Sativa";
        [SerializeField] private string _strainId;
        [SerializeField] private string _strainDescription;

        [Header("Strain Properties")]
        [SerializeField] private StrainType _strainType = StrainType.Hybrid;

        public float THCContent
        {
            get => _thcContent;
            set => _thcContent = value;
        }

        public float CBDContent
        {
            get => _cbdContent;
            set => _cbdContent = value;
        }

        // Compatibility properties for cultivation system
        public float thcContent => _thcContent;
        public float cbdContent => _cbdContent;

        public float BaseYield
        {
            get => _baseYield;
            set => _baseYield = value;
        }

        public float BaseYieldGrams => _baseYield;

        // Environmental range properties for compatibility
        public Vector2 TemperatureRange = new Vector2(18f, 26f);
        public Vector2 HumidityRange = new Vector2(40f, 60f);
        public Vector2 LightRange = new Vector2(200f, 800f);
        public Vector2 CO2Range = new Vector2(400f, 1200f);

        public float BaseFloweringTime
        {
            get => _baseFloweringTime;
            set => _baseFloweringTime = value;
        }

        public string StrainId
        {
            get => _strainId;
            set => _strainId = value;
        }

        public string StrainDescription
        {
            get => _strainDescription;
            set => _strainDescription = value;
        }

        public StrainType StrainType
        {
            get => _strainType;
            set => _strainType = value;
        }

        public string BaseSpecies
        {
            get => _baseSpecies;
            set => _baseSpecies = value;
        }

        public float HeightModifier { get; set; } = 1.0f;
        public float GrowthRateModifier { get; set; } = 1.0f;
        public float WidthModifier { get; set; } = 1.0f;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_strainId))
                _strainId = System.Guid.NewGuid().ToString();
        }
    }
}
