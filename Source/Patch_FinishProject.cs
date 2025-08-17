using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RepeatableResearch {
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class Patch_FinishProject {
        private const string EndlessDefName = "EndlessProgress";

        public static void Postfix(ResearchProjectDef proj) {
            if (proj == null || proj.defName != EndlessDefName) return;
            if (Current.Game == null) return;

            // 1) grant a stack
            var comp = Current.Game.GetComponent<RepeatableResearchComp>();
            comp?.GrantRandomBuff();

            // 2) reset completion/progress (no hard field names)
            var rm = Find.ResearchManager;
            if (rm == null) return;

            try {
                // finishedProjects: HashSet<ResearchProjectDef>
                var finishedField = AccessTools
                    .GetDeclaredFields(typeof(ResearchManager))
                    .FirstOrDefault(f => f.FieldType == typeof(HashSet<ResearchProjectDef>));
                var finished = finishedField?.GetValue(rm) as HashSet<ResearchProjectDef>;
                finished?.Remove(proj);

                // progress: Dictionary<ResearchProjectDef, float>
                var progressField = AccessTools
                    .GetDeclaredFields(typeof(ResearchManager))
                    .FirstOrDefault(f => f.FieldType == typeof(Dictionary<ResearchProjectDef, float>));
                var progress = progressField?.GetValue(rm) as Dictionary<ResearchProjectDef, float>;
                if (progress != null) progress[proj] = 0f;
            } catch (Exception e) {
                Log.Warning($"[RepeatableResearch] reset failed via reflection: {e}");
            }

            rm.ReapplyAllMods();
        }
    }
}
