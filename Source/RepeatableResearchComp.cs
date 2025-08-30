using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RepeatableResearch {
    public class RepeatableResearchComp : GameComponent {
        private const int TicksPerDay = 60000;
        private const int PruneInterval = 3000;   // 50s

        // ?? ???
        public Dictionary<StatDef, int> Buffs = new Dictionary<StatDef, int>();
        public List<TempEntry> TempBuffs = new List<TempEntry>();
        public int TotalCompletions;

        // ??? ??
        private DefMap<StatDef, int> _permMap;
        private DefMap<StatDef, int> _tempMap;
        private bool _hasAnyBuffs;
        private HashSet<StatDef> _affected;
        private int _nextPruneTick;

        public RepeatableResearchComp(Game game) { }

        public override void FinalizeInit() {
            EnsureMaps();
            RebuildAffected();
            ResearchDescPatcher.UpdateDescription();
        }

        public override void LoadedGame() {
            EnsureMaps();
            RebuildAffected();
            ResearchDescPatcher.UpdateDescription();
        }

        // === ?? ?? ===

        private static void ZeroMap(DefMap<StatDef, int> map) {
            var list = DefDatabase<StatDef>.AllDefsListForReading;
            for (int i = 0; i < list.Count; i++) map[list[i]] = 0;
        }

        private void EnsureMaps() {
            if (_permMap == null) { _permMap = new DefMap<StatDef, int>(); ZeroMap(_permMap); }
            if (_tempMap == null) { _tempMap = new DefMap<StatDef, int>(); ZeroMap(_tempMap); }

            // ??? Buffs? ? ? ??
            if (Buffs != null && Buffs.Count > 0) {
                foreach (var kv in Buffs)
                    if (kv.Key != null) _permMap[kv.Key] = Math.Max(_permMap[kv.Key], kv.Value);
            }
        }

        public void RebuildAffected() {
            _affected = new HashSet<StatDef>(
                (RRMod.Settings?.GetActiveCandidates() ?? Enumerable.Empty<StatDef>())
                .Where(sd => sd != null));
        }

        private void RebuildTempCounts(int now) {
            EnsureMaps();
            ZeroMap(_tempMap);
            if (TempBuffs == null || TempBuffs.Count == 0) return;

            for (int i = 0; i < TempBuffs.Count; i++) {
                var e = TempBuffs[i];
                if (e == null || e.Stat == null || e.ExpiryTick <= now) continue;
                _tempMap[e.Stat] = _tempMap[e.Stat] + 1;
            }
        }

        // === API ===

        public void ClearAllBuffs() {
            Buffs ??= new Dictionary<StatDef, int>();
            Buffs.Clear();

            TempBuffs ??= new List<TempEntry>();
            TempBuffs.Clear();

            EnsureMaps();
            ZeroMap(_permMap);
            ZeroMap(_tempMap);

            _hasAnyBuffs = false;
            TotalCompletions = 0;
            ResearchDescPatcher.UpdateDescription();
        }

        public void GrantRandomBuff(bool permanent) {
            var cand = RRMod.Settings?.GetActiveCandidates().ToList();
            if (cand == null || cand.Count == 0) return;

            var chosen = cand.RandomElement();
            EnsureMaps();

            if (permanent) {
                int stacks = 0; Buffs.TryGetValue(chosen, out stacks);
                Buffs[chosen] = stacks + 1;
                _permMap[chosen] = _permMap[chosen] + 1;
                _hasAnyBuffs = true;

                float incP = RRMod.Settings?.incrementPercentPerm ?? 2f;
                Messages.Message($"Endless progress increases {chosen.label} by +{incP:0.#}% permanently.",
                    MessageTypeDefOf.PositiveEvent);
            } else {
                int now = Find.TickManager.TicksGame;
                int durTicks = DaysToTicks(RRMod.Settings?.tempDurationDays ?? 5f);
                TempBuffs ??= new List<TempEntry>();
                TempBuffs.Add(new TempEntry(chosen, now + durTicks));

                _tempMap[chosen] = _tempMap[chosen] + 1;
                _hasAnyBuffs = true;

                float incT = RRMod.Settings?.incrementPercentTemp ?? 3f;
                Messages.Message($"Endless progress increases {chosen.label} by +{incT:0.#}% temporarily ({durTicks / 60000f:0.#}d).",
                    MessageTypeDefOf.PositiveEvent);
            }

            TotalCompletions++;
            ResearchDescPatcher.UpdateDescription();
        }

        public int PermStacks(StatDef s) {
            if (s == null) return 0;
            EnsureMaps();
            return _permMap[s];
        }

        public int ActiveTempStacks(StatDef s, int now) {
            if (s == null) return 0;
            EnsureMaps();
            return _tempMap[s];
        }

        public override void GameComponentTick() {
            int now = Find.TickManager.TicksGame;
            if (now < _nextPruneTick) return;
            _nextPruneTick = now + PruneInterval;

            if (TempBuffs != null && TempBuffs.Count > 0) {
                for (int i = TempBuffs.Count - 1; i >= 0; i--)
                    if (TempBuffs[i].ExpiryTick <= now) TempBuffs.RemoveAt(i);
            }
            RebuildTempCounts(now);

            // _hasAnyBuffs ???
            bool anyPerm = Buffs != null && Buffs.Any(kv => kv.Value > 0);
            bool anyTemp = false;
            var defs = DefDatabase<StatDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++) { if (_tempMap[defs[i]] > 0) { anyTemp = true; break; } }
            _hasAnyBuffs = anyPerm || anyTemp;
        }

        public static int DaysToTicks(float d) => (int)(d * TicksPerDay);
    }

    public class TempEntry {
        public StatDef Stat;
        public int ExpiryTick;
        public TempEntry() { }
        public TempEntry(StatDef s, int exp) { Stat = s; ExpiryTick = exp; }
    }
}
