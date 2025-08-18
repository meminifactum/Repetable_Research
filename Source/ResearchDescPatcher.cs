using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RepeatableResearch
{
    public static class ResearchDescPatcher
    {
        private const string DefPerm = "EndlessProgress_Perm";
        private const string DefTemp = "EndlessProgress_Temp";
        private const string DefLegacy = "EndlessProgress";

        private static readonly Dictionary<string, string> _baseDesc = new Dictionary<string, string>();

        public static void UpdateDescription()
        {
            if (Current.Game == null) return;
            var comp = Current.Game.GetComponent<RepeatableResearchComp>();
            if (comp == null) return;

            var projPerm = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(DefPerm)
                        ?? DefDatabase<ResearchProjectDef>.GetNamedSilentFail(DefLegacy);
            var projTemp = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(DefTemp);

            if (projPerm != null) projPerm.description = BuildDesc(comp, projPerm, permanentNode: true);
            if (projTemp != null) projTemp.description = BuildDesc(comp, projTemp, permanentNode: false);
        }

        private static string BuildDesc(RepeatableResearchComp comp, ResearchProjectDef proj, bool permanentNode)
        {
            if (!_baseDesc.ContainsKey(proj.defName))
                _baseDesc[proj.defName] = proj.description ?? string.Empty;

            var sb = new StringBuilder();
            sb.Append(_baseDesc[proj.defName]);

            float pInc = RRMod.Settings != null ? RRMod.Settings.incrementPercentPerm : 2f;
            float tInc = RRMod.Settings != null ? RRMod.Settings.incrementPercentTemp : 3f;

            if (permanentNode)
            {
                sb.AppendLine().AppendLine("Stat buff — Permanent");
                if (comp.Buffs != null && comp.Buffs.Count > 0)
                {
                    foreach (var kv in comp.Buffs.OrderBy(k => k.Key.label ?? k.Key.defName))
                    {
                        var stat = kv.Key; if (stat == null) continue;
                        int stacks = kv.Value; float total = stacks * pInc;
                        sb.AppendLine($" • {stat.label.CapitalizeFirst()}: +{total:0.#}%  ({stacks} perm)");
                    }
                }
                else sb.AppendLine(" • none");
            }
            else
            {
                sb.AppendLine().AppendLine("Stat buff — Temporary");
                int now = Find.TickManager.TicksGame;
                var list = comp.TempBuffs;
                if (list != null && list.Count > 0)
                {
                    var active = list.Where(e => e != null && e.Stat != null && e.ExpiryTick > now)
                                     .GroupBy(e => e.Stat)
                                     .OrderBy(g => g.Key.label ?? g.Key.defName);
                    bool any = false;
                    foreach (var g in active)
                    {
                        int stacks = g.Count(); if (stacks <= 0) continue; any = true;
                        int soonest = g.Min(e => e.ExpiryTick);
                        float daysLeft = (soonest - now) / 60000f;
                        float total = stacks * tInc;
                        sb.AppendLine($" • {g.Key.label.CapitalizeFirst()}: +{total:0.#}%  ({stacks} temp, {daysLeft:0.#}d left)");
                    }
                    if (!any) sb.AppendLine(" • none");
                }
                else sb.AppendLine(" • none");
            }

            return sb.ToString();
        }
    }
}
