using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RepeatableResearch
{
    public class RepeatableResearchComp : GameComponent
    {
        private const int TicksPerDay = 60000;

        // Permanent stacks: Stat -> count
        public Dictionary<StatDef, int> Buffs = new Dictionary<StatDef, int>();

        // Temporary stacks as flat entries for robust save/load
        public class TempEntry : IExposable
        {
            public StatDef Stat;
            public int ExpiryTick;

            public TempEntry() { }
            public TempEntry(StatDef stat, int expiry) { Stat = stat; ExpiryTick = expiry; }

            public void ExposeData()
            {
                Scribe_Defs.Look(ref Stat, "stat");
                Scribe_Values.Look(ref ExpiryTick, "exp");
            }
        }
        public List<TempEntry> TempBuffs = new List<TempEntry>();

        public int TotalCompletions;
        public string BaseDesc;

        public RepeatableResearchComp() { }
        public RepeatableResearchComp(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref Buffs, "RR_Buffs", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref TempBuffs, "RR_TempBuffs", LookMode.Deep);
            Scribe_Values.Look(ref TotalCompletions, "RR_TotalCompletions", 0);
            Scribe_Values.Look(ref BaseDesc, "RR_BaseDesc");

            // back-compat for old saves
            if (Buffs == null) Buffs = new Dictionary<StatDef, int>();
            if (TempBuffs == null) TempBuffs = new List<TempEntry>();
        }

        public override void FinalizeInit() { ResearchDescPatcher.UpdateDescription(); }
        public override void LoadedGame() { ResearchDescPatcher.UpdateDescription(); }

        // === API used by other files ===

        public void ClearAllBuffs()
        {
            if (Buffs == null) Buffs = new Dictionary<StatDef, int>();
            Buffs.Clear();

            if (TempBuffs == null) TempBuffs = new List<TempEntry>();
            TempBuffs.Clear();

            TotalCompletions = 0;
            ResearchDescPatcher.UpdateDescription();
        }

        // Legacy entry point (treat as permanent)
        public void GrantRandomBuff() => GrantRandomBuff(true);

        public void GrantRandomBuff(bool permanent)
        {
            var cand = RRMod.Settings != null ? RRMod.Settings.GetActiveCandidates().ToList() : null;
            if (cand == null || cand.Count == 0) return;

            var chosen = cand.RandomElement();
            if (permanent)
            {
                int stacks;
                if (!Buffs.TryGetValue(chosen, out stacks)) stacks = 0;
                Buffs[chosen] = stacks + 1;

                float incP = RRMod.Settings != null ? RRMod.Settings.incrementPercentPerm : 2f;
                Messages.Message(
                    $"Endless progress increases {chosen.label} by +{incP:0.#}% permanently.",
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else
            {
                int now = Find.TickManager.TicksGame;
                int durTicks = DaysToTicks(RRMod.Settings != null ? RRMod.Settings.tempDurationDays : 5f);
                if (TempBuffs == null) TempBuffs = new List<TempEntry>();
                TempBuffs.Add(new TempEntry(chosen, now + durTicks));

                float incT = RRMod.Settings != null ? RRMod.Settings.incrementPercentTemp : 3f;
                Messages.Message(
                    $"Endless progress increases {chosen.label} by +{incT:0.#}% temporarily ({durTicks / (float)TicksPerDay:0.#}d).",
                    MessageTypeDefOf.PositiveEvent
                );
            }

            TotalCompletions++;
            ResearchDescPatcher.UpdateDescription();
        }

        public int PermStacks(StatDef s)
        {
            int v; return Buffs != null && Buffs.TryGetValue(s, out v) ? v : 0;
        }

        public int ActiveTempStacks(StatDef s, int now)
        {
            if (TempBuffs == null || TempBuffs.Count == 0) return 0;

            // prune expired
            for (int i = TempBuffs.Count - 1; i >= 0; i--)
                if (TempBuffs[i].ExpiryTick <= now) TempBuffs.RemoveAt(i);

            int cnt = 0;
            for (int i = 0; i < TempBuffs.Count; i++)
                if (TempBuffs[i].Stat == s) cnt++;
            return cnt;
        }

        public int? NextExpiryTicks(StatDef s, int now)
        {
            if (TempBuffs == null || TempBuffs.Count == 0) return null;
            int? best = null;
            for (int i = 0; i < TempBuffs.Count; i++)
            {
                var e = TempBuffs[i];
                if (e.Stat != s || e.ExpiryTick <= now) continue;
                if (best == null || e.ExpiryTick < best.Value) best = e.ExpiryTick;
            }
            return best;
        }

        public static int DaysToTicks(float days)
        {
            if (days <= 0f) return 0;
            return (int)Math.Round(days * TicksPerDay);
        }
    }
}
