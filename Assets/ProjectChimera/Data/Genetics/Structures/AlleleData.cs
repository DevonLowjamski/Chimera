using System.Collections.Generic;
using System;
using ProjectChimera.Data.Genetics;
using MutationRecord = ProjectChimera.Data.Genetics.MutationRecord;
using TraitExpressionResult = ProjectChimera.Data.Genetics.TraitExpressionResult;
using TraitType = ProjectChimera.Data.Genetics.TraitType;
using BreedingStrategyType = ProjectChimera.Data.Genetics.BreedingStrategyType;

namespace ProjectChimera.Data.Genetics
{
    public class AlleleData
    {
        public string AlleleId;
        public string GeneLocus;
        public float EffectValue;
        public float Dominance;
        public bool IsWildType;
        public string OriginStrain;
        public DateTime FirstObserved;
    }
}





