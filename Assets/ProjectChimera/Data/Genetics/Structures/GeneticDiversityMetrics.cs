using System.Collections.Generic;
using System;
using ProjectChimera.Data.Genetics;
using MutationRecord = ProjectChimera.Data.Genetics.MutationRecord;
using TraitExpressionResult = ProjectChimera.Data.Genetics.TraitExpressionResult;
using TraitType = ProjectChimera.Data.Genetics.TraitType;
using BreedingStrategyType = ProjectChimera.Data.Genetics.BreedingStrategyType;

namespace ProjectChimera.Data.Genetics
{
    public class GeneticDiversityMetrics
    {
        public float ShannonIndex;
        public float SimpsonIndex;
        public int NumberOfAlleles;
        public float EffectivePopulationSize;
        public float NucleotideDiversity;
    }
}

