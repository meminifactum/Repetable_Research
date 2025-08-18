using HarmonyLib;
using RimWorld;
using Verse;

namespace RepeatableResearch
{
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
    static class Patch_StatWorker
    {
        static void Postfix(StatRequest req, ref float __result, StatDef ___stat)
        {
            var comp = Current.Game?.GetComponent<RepeatableResearchComp>();
            if (comp == null) return;

            int perm = comp.PermStacks(___stat);
            int temp = comp.ActiveTempStacks(___stat, Find.TickManager.TicksGame);
            if (perm <= 0 && temp <= 0) return;

            float pInc = (RRMod.Settings?.incrementPercentPerm ?? 2f) / 100f;
            float tInc = (RRMod.Settings?.incrementPercentTemp ?? 3f) / 100f;

            __result *= (1f + pInc * perm) * (1f + tInc * temp);
        }
    }
}
