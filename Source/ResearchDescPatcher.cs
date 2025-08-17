using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RepeatableResearch {
    public static class ResearchDescPatcher {
        private const string Endless = "EndlessProgress";

        public static void UpdateDescription() {
            if (Current.Game == null) return;
            var comp = Current.Game.GetComponent<RepeatableResearchComp>();
            var proj = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(Endless);
            if (proj == null || comp == null) return;

            // capture base description once
            if (string.IsNullOrEmpty(comp.BaseDesc))
                comp.BaseDesc = proj.description ?? "";

            var sb = new StringBuilder();
            sb.Append(comp.BaseDesc);

            if (comp.Buffs != null && comp.Buffs.Count > 0) {
                float inc = (RRMod.Settings?.incrementPercent ?? 2f);
                sb.AppendLine().AppendLine();
                sb.AppendLine("Active bonuses:");
                foreach (var kv in comp.Buffs.OrderBy(k => k.Key.label ?? k.Key.defName)) {
                    var stat = kv.Key;
                    int stacks = kv.Value;
                    float total = stacks * inc;
                    sb.AppendLine($" • {stat.label.CapitalizeFirst()}: +{total:0.#}%  ({stacks} stacks)");
                }
            } else {
                sb.AppendLine().AppendLine("Active bonuses: none");
            }

            proj.description = sb.ToString();
        }
    }
}
