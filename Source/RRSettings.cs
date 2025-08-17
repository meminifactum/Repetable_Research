using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RepeatableResearch {
    public class RRSettings : ModSettings {
        public HashSet<string> userStatNames = new HashSet<string>();
        public float incrementPercent = 2f;   // NEW

        public override void ExposeData() {
            Scribe_Collections.Look(ref userStatNames, "RR_UserStats", LookMode.Value);
            Scribe_Values.Look(ref incrementPercent, "RR_IncPercent", 2f);
        }

        public IEnumerable<StatDef> GetActiveCandidates() {
            IEnumerable<StatDef> FromNames(IEnumerable<string> names) =>
                names.Select(n => DefDatabase<StatDef>.GetNamedSilentFail(n)).Where(sd => sd != null);

            var user = userStatNames.Count > 0 ? FromNames(userStatNames) : Enumerable.Empty<StatDef>();
            var list = user.Any() ? user : FromNames(RRPool.DefaultActive);   // << changed
            return list.GroupBy(sd => sd.defName).Select(g => g.First());
        }


        public static IEnumerable<StatDef> AllStatsForUI(string query) {
            var q = (query ?? "").Trim().ToLowerInvariant();
            return DefDatabase<StatDef>.AllDefsListForReading
                .Where(sd =>
                    (sd.label ?? "").ToLowerInvariant().Contains(q) ||
                    (sd.defName ?? "").ToLowerInvariant().Contains(q))
                .OrderBy(sd => sd.label ?? sd.defName);
        }
    }
}
