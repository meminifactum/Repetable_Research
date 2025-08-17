using HarmonyLib;
using RimWorld;
using Verse;

namespace RepeatableResearch {
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
    static class Patch_StatWorker {
        static void Postfix(StatRequest req, ref float __result, StatDef ___stat) {
            var comp = Current.Game?.GetComponent<RepeatableResearchComp>();
            if (comp == null || comp.Buffs == null) return;

            int stacks;
            if (comp.Buffs.TryGetValue(___stat, out stacks) && stacks > 0) {
                float inc = (RRMod.Settings?.incrementPercent ?? 2f) / 100f;
                __result *= 1f + inc * stacks;
            }
        }
    }


}
