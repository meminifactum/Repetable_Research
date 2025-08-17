using Verse;

namespace RepeatableResearch {
    public static class RRPool {
        // LEFT list (2.1.0 curated pool)
        public static readonly string[] PoolNames = {
            "GeneralLaborSpeed","SmeltingSpeed","DrugSynthesisSpeed","DrugCookingSpeed","CookSpeed",
            "ButcheryFleshSpeed","ButcheryMechanoidSpeed","ButcheryFleshEfficiency","ButcheryMechanoidEfficiency",
            "MedicalTendSpeed","MedicalTendQuality","MedicalOperationSpeed","MedicalSurgerySuccessChance",
            "ImmunityGainSpeed","InjuryHealingFactor","RestRateMultiplier","EatingSpeed",
            "ToxicResistance","ToxicEnvironmentResistance","ForagedNutritionPerDay","CarryingCapacity",
            "GlobalLearningFactor","MeditationFocusGain","PsychicEntropyMax","PsychicEntropyRecoveryRate",
            "MoveSpeed","MeleeHitChance","MeleeDodgeChance","MeleeDamageFactor","ShootingAccuracyPawn",
            "ShootingAccuracyFactor_Touch","ShootingAccuracyFactor_Short","ShootingAccuracyFactor_Medium","ShootingAccuracyFactor_Long",
            "MeatAmount","LeatherAmount","ResearchSpeed","ConstructionSpeed","MiningSpeed","PlantWorkSpeed"
        };

        // DEFAULT active when user picked none (exactly these 5)
        public static readonly string[] DefaultActive = {
            "DrugCookingSpeed", "ButcheryMechanoidSpeed", "MedicalOperationSpeed",
            "GlobalLearningFactor", "ForagedNutritionPerDay"
        };
    }
}
