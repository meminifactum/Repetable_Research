using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RepeatableResearch
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class Patch_FinishProject
    {
        private const string DefPerm = "EndlessProgress_Perm";
        private const string DefTemp = "EndlessProgress_Temp";
        private const string DefLegacy = "EndlessProgress";

        public static void Postfix(ResearchProjectDef proj)
        {
            if (proj == null || Current.Game == null) return;

            const string DefPerm = "EndlessProgress_Perm";
            const string DefTemp = "EndlessProgress_Temp";
            const string DefLegacy = "EndlessProgress";

            bool isPerm = proj.defName == DefPerm || proj.defName == DefLegacy;
            bool isTemp = proj.defName == DefTemp;
            if (!isPerm && !isTemp) return; // ← 우리 프로젝트 외에는 아무 것도 하지 않음

            var rm = Find.ResearchManager;
            if (rm == null) return;

            // grant (예외시에도 아래 reset은 진행)
            try
            {
                var comp = Current.Game.GetComponent<RepeatableResearchComp>();
                comp?.GrantRandomBuff(isPerm);
            }
            catch (System.Exception e)
            {
                Log.Warning($"[RepeatableResearch] grant buff failed: {e}");
            }

            // reset only for our project
            try
            {
                var finishedField = AccessTools.GetDeclaredFields(typeof(ResearchManager))
                    .FirstOrDefault(f => f.FieldType == typeof(HashSet<ResearchProjectDef>));
                (finishedField?.GetValue(rm) as HashSet<ResearchProjectDef>)?.Remove(proj);

                var progressField = AccessTools.GetDeclaredFields(typeof(ResearchManager))
                    .FirstOrDefault(f => f.FieldType == typeof(Dictionary<ResearchProjectDef, float>));
                var progress = progressField?.GetValue(rm) as Dictionary<ResearchProjectDef, float>;
                if (progress != null) progress[proj] = 0f;
            }
            catch (System.Exception e)
            {
                Log.Warning($"[RepeatableResearch] reset failed via reflection: {e}");
            }

            rm.ReapplyAllMods();
        }

    }
}
