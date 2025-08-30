using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RepeatableResearch
{
    public class RRSettings : ModSettings
    {
        public HashSet<string> userStatNames = new HashSet<string>();

        public float incrementPercentPerm = 2f;  // permanent
        public float incrementPercentTemp = 3f;  // temporary
        public float tempDurationDays = 5f;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref userStatNames, "RR_UserStats", LookMode.Value);
            Scribe_Values.Look(ref incrementPercentPerm, "RR_IncPercentPerm", 2f);
            Scribe_Values.Look(ref incrementPercentTemp, "RR_IncPercentTemp", 3f);
            Scribe_Values.Look(ref tempDurationDays, "RR_TempDays", 5f);

            // legacy migrate
            float legacy = -1f;
            Scribe_Values.Look(ref legacy, "RR_IncPercent", -1f);
            if (legacy >= 0f)
            {
                incrementPercentPerm = legacy;
                incrementPercentTemp = legacy;
            }

            // back-compat
            if (userStatNames == null) userStatNames = new HashSet<string>();
            if (tempDurationDays <= 0f) tempDurationDays = 5f;
        }

        public IEnumerable<StatDef> GetActiveCandidates()
        {
            IEnumerable<StatDef> FromNames(IEnumerable<string> names) =>
                names.Select(n => DefDatabase<StatDef>.GetNamedSilentFail(n)).Where(sd => sd != null);

            var user = userStatNames.Count > 0 ? FromNames(userStatNames) : Enumerable.Empty<StatDef>();
            var list = user.Any() ? user : FromNames(RRPool.DefaultActive);
            return list.GroupBy(sd => sd.defName).Select(g => g.First());
        }

        public static IEnumerable<StatDef> AllStatsForUI(string query)
        {
            var q = (query ?? "").Trim().ToLowerInvariant();
            return DefDatabase<StatDef>.AllDefsListForReading
                .Where(sd =>
                    (sd.label ?? "").ToLowerInvariant().Contains(q) ||
                    (sd.defName ?? "").ToLowerInvariant().Contains(q))
                .OrderBy(sd => sd.label ?? sd.defName);
        }
    }
}