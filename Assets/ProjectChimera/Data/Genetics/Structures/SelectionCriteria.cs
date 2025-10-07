using System.Collections.Generic;
using System;
using ProjectChimera.Data.Genetics;
using MutationRecord = ProjectChimera.Data.Genetics.MutationRecord;
using TraitExpressionResult = ProjectChimera.Data.Genetics.TraitExpressionResult;
using TraitType = ProjectChimera.Data.Genetics.TraitType;
using BreedingStrategyType = ProjectChimera.Data.Genetics.BreedingStrategyType;

namespace ProjectChimera.Data.Genetics
{
    public class SelectionCriteria
    {
        public Dictionary<string, float> TraitWeights;
        public float SelectionIntensity;
        public int NumberOfGenerations;
        public bool EnableCorrelatedResponse;
    }
}
