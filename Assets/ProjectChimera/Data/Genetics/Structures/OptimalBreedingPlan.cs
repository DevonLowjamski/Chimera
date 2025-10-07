using System.Collections.Generic;
using System;
using ProjectChimera.Data.Genetics;
using MutationRecord = ProjectChimera.Data.Genetics.MutationRecord;
using TraitExpressionResult = ProjectChimera.Data.Genetics.TraitExpressionResult;
using TraitType = ProjectChimera.Data.Genetics.TraitType;
using BreedingStrategyType = ProjectChimera.Data.Genetics.BreedingStrategyType;

namespace ProjectChimera.Data.Genetics
{
    public class OptimalBreedingPlan
    {
        public List<BreedingPair> Phase1Crosses;
        public List<BreedingPair> Phase2Crosses;
        public List<BreedingPair> Phase3Crosses;
        public float ExpectedGeneticGain;
        public int EstimatedTimeToCompletion;
        public List<string> CriticalDecisionPoints;
    }
}



