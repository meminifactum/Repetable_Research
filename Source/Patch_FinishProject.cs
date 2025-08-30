using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RepeatableResearch {
    [HarmonyPatch(typeof(ResearchManager), "FinishProject")]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_FinishProject_All {
        private static bool _resolved;
        private static bool _disable;

        private static FieldInfo _fiProgress;      // Dictionary<ResearchProjectDef,float>
        private static FieldInfo _fiCurrentProj;   // ResearchProjectDef
        private static FieldInfo _fiTechprints;    // Dictionary<ResearchProjectDef,int>   (optional)
        private static FieldInfo _fiAnomKnow;      // Dictionary<ResearchProjectDef,float> (optional)

        private static void Resolve() {
            if (_resolved) return;
            _resolved = true;
            try {
                var t = typeof(ResearchManager);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                foreach (var fi in t.GetFields(flags)) {
                    if (_fiProgress == null &&
                        typeof(Dictionary<ResearchProjectDef, float>).IsAssignableFrom(fi.FieldType))
                        _fiProgress = fi;

                    if (_fiCurrentProj == null && fi.FieldType == typeof(ResearchProjectDef))
                        _fiCurrentProj = fi;

                    if (_fiTechprints == null &&
                        typeof(Dictionary<ResearchProjectDef, int>).IsAssignableFrom(fi.FieldType))
                        _fiTechprints = fi;

                    if (_fiAnomKnow == null &&
                        typeof(Dictionary<ResearchProjectDef, float>).IsAssignableFrom(fi.FieldType) &&
                        fi.Name.ToLowerInvariant().Contains("anomaly"))
                        _fiAnomKnow = fi;
                }

                if (_fiProgress == null) {
                    var sb = new StringBuilder();
                    sb.AppendLine("[RR] ResearchManager fields dump (no progress found):");
                    foreach (var fi in t.GetFields(flags)) sb.AppendLine($" - {fi.Name} : {fi.FieldType.FullName}");
                    Log.Error(sb.ToString());
                    throw new MissingFieldException("Dictionary<ResearchProjectDef,float> progress not found");
                }
            } catch (Exception e) {
                _disable = true;
                Log.Error("[RR] Resolve ResearchManager fields failed. Disabling repeat-reset. " + e);
            }
        }

        public static void Postfix(ResearchManager __instance, ResearchProjectDef proj) {
            if (__instance == null || proj == null) return;
            Resolve();
            if (_disable) return;

            bool ours = proj.defName == "EndlessProgress_Perm"
                     || proj.defName == "EndlessProgress_Temp"
                     || proj.defName == "EndlessProgress";
            if (!ours) return;

            try {
                // grant buff
                try {
                    bool isPerm = proj.defName != "EndlessProgress_Temp";
                    Current.Game?.GetComponent<RepeatableResearchComp>()?.GrantRandomBuff(isPerm);
                } catch (Exception eGrant) { Log.Warning("[RR] grant failed: " + eGrant); }

                // reset progress (authoritative in your build)
                var progress = _fiProgress?.GetValue(__instance) as Dictionary<ResearchProjectDef, float>;
                if (progress != null) progress[proj] = 0f;

                // clear current selection if it points to us
                if (_fiCurrentProj != null && Equals(_fiCurrentProj.GetValue(__instance), proj))
                    _fiCurrentProj.SetValue(__instance, null);

                // optional cleanups (harmless if not used by our proj)
                var techprints = _fiTechprints?.GetValue(__instance) as Dictionary<ResearchProjectDef, int>;
                if (techprints != null && techprints.ContainsKey(proj)) techprints[proj] = 0;

                var anom = _fiAnomKnow?.GetValue(__instance) as Dictionary<ResearchProjectDef, float>;
                if (anom != null && anom.ContainsKey(proj)) anom[proj] = 0f;

                __instance.ReapplyAllMods();
                ResearchDescPatcher.UpdateDescription();

                Log.Message("[RR] reset " + proj.defName + " ok");
            } catch (Exception e) {
                Log.Warning("[RR] reset failed: " + e);
            }
        }
    }
}
