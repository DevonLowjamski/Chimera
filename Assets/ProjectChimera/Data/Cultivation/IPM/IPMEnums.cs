
using UnityEngine;

namespace ProjectChimera.Data.Cultivation.IPM
{
    /// <summary>
    /// Core enums for Integrated Pest Management system
    /// </summary>
    public enum IPMApproach
    {
        Biological_First,
        Cultural_First,
        Mechanical_First,
        Chemical_Last_Resort,
        Preventive_Focused,
        Reactive_Focused,
        Balanced_Integrated
    }

    public enum IPMComplexityLevel
    {
        Basic,
        Intermediate,
        Professional,
        Advanced,
        Expert
    }

    public enum PestType
    {
        Spider_Mites,
        SpiderMites,
        Thrips,
        Aphids,
        Whiteflies,
        FungusGnats,
        PowderyMildew,
        Botrytis,
        Pythium,
        Fusarium,
        RootAphids,
        Mealybugs,
        ScaleInsects,
        Leafhoppers,
        Caterpillars,
        Slugs,
        Snails,
        Nematodes,
        Viruses,
        NutrientDeficiencies
    }

    public enum BeneficialType
    {
        Predator,
        Predators,
        Parasites,
        Pathogens,
        Competitors,
        Pollinators
    }

    public enum InspectionFrequency
    {
        Daily,
        Every_Other_Day,
        EveryOtherDay,
        Weekly,
        BiWeekly,
        Monthly,
        AsNeeded
    }

    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Evening,
        Night,
        AnyTime
    }

    public enum PracticeFrequency
    {
        Daily,
        Weekly,
        BiWeekly,
        Monthly,
        Seasonal,
        AsNeeded
    }

    public enum TreatmentType
    {
        Biological,
        Cultural,
        Mechanical,
        Chemical,
        Organic,
        Integrated
    }

    public enum ResistanceRisk
    {
        Low,
        Moderate,
        High,
        Critical,
        Unknown
    }

    public enum InterventionPriority
    {
        Low,
        Medium,
        High,
        Critical,
        Immediate
    }

}
