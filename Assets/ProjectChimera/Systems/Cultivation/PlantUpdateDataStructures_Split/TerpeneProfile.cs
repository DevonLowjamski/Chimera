using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class TerpeneProfile
    {
        public float Myrcene { get; set; }
        public float Limonene { get; set; }
        public float Pinene { get; set; }
        public float Linalool { get; set; }
        public float Caryophyllene { get; set; }
        public float Humulene { get; set; }
        public float Terpinolene { get; set; }
        public float Ocimene { get; set; }

        /// <summary>
        /// Get total terpene content
        /// </summary>
        public float GetTotalTerpenes()
        {
            return Myrcene + Limonene + Pinene + Linalool +
                   Caryophyllene + Humulene + Terpinolene + Ocimene;
        }

        /// <summary>
        /// Get dominant terpene
        /// </summary>
        public string GetDominantTerpene()
        {
            var terpenes = new Dictionary<string, float>
            {
                { "Myrcene", Myrcene },
                { "Limonene", Limonene },
                { "Pinene", Pinene },
                { "Linalool", Linalool },
                { "Caryophyllene", Caryophyllene },
                { "Humulene", Humulene },
                { "Terpinolene", Terpinolene },
                { "Ocimene", Ocimene }
            };

            string dominant = "None";
            float maxValue = 0f;

            foreach (var kvp in terpenes)
            {
                if (kvp.Value > maxValue)
                {
                    maxValue = kvp.Value;
                    dominant = kvp.Key;
                }
            }

            return maxValue > 0.01f ? dominant : "None";
        }

        /// <summary>
        /// Get aroma profile based on terpenes
        /// </summary>
        public string GetAromaProfile()
        {
            string dominant = GetDominantTerpene();

            switch (dominant)
            {
                case "Myrcene": return "Earthy, Musky";
                case "Limonene": return "Citrus, Fresh";
                case "Pinene": return "Pine, Fresh";
                case "Linalool": return "Floral, Sweet";
                case "Caryophyllene": return "Spicy, Peppery";
                case "Humulene": return "Woody, Earthy";
                case "Terpinolene": return "Floral, Herbal";
                case "Ocimene": return "Sweet, Herbaceous";
                default: return "Complex Profile";
            }
        }
    }

    /// <summary>
    /// Phenotypic traits affecting plant characteristics
    /// </summary>

}
