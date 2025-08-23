using RimWorld;
using Verse;
using HarmonyLib;

namespace RepeatableResearch
{
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
    public static class Patch_StatWorker
    {
        [HarmonyPostfix]
        public static void Postfix(StatRequest req, ref float __result, StatDef ___stat)
        {
            var comp = Current.Game?.GetComponent<RepeatableResearchComp>();
            if (comp == null) return;

            // 대상이 colonist pawn이 아니면 무시
            if (!(req.Thing is Pawn pawn)) return;
            if (!pawn.IsColonist) return;

            // 빠른 조회 (O(1))
            int perm = comp.PermCountFast(___stat);
            int temp = comp.TempCountFast(___stat);
            if ((perm | temp) == 0) return;

            float pInc = (RRMod.Settings?.incrementPercentPerm ?? 2f) / 100f;
            float tInc = (RRMod.Settings?.incrementPercentTemp ?? 3f) / 100f;

            __result *= (1f + pInc * perm) * (1f + tInc * temp);
        }
    }
}
