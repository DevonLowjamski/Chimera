using System.Collections.Generic;
using System;
using ProjectChimera.Data.Genetics;
using MutationRecord = ProjectChimera.Data.Genetics.MutationRecord;
using TraitExpressionResult = ProjectChimera.Data.Genetics.TraitExpressionResult;
using TraitType = ProjectChimera.Data.Genetics.TraitType;
using BreedingStrategyType = ProjectChimera.Data.Genetics.BreedingStrategyType;

namespace ProjectChimera.Data.Genetics
{
    public class PopulationGeneticAnalysis
    {
        public int PopulationSize;
        public float AlleleFrequency;
        public float HeterozygosityObserved;
        public float HeterozygosityExpected;
        public float InbreedingCoefficient;
        public List<string> RareAlleles;
        public float GeneticDiversity;
    }
}
