using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RepeatableResearch
{
    public class RepeatableResearchComp : GameComponent
    {
        private const int TicksPerDay = 60000;
        private const int PruneIntervalTicks = 300; // ~몇 초 간격으로 임시버프 만료 정리

        // 영구 스택: Stat -> stacks
        public Dictionary<StatDef, int> Buffs = new Dictionary<StatDef, int>();

        // 임시 스택 원본(세이브용)
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

        // 성능용 캐시: 현재 유효한 임시 스택 수 집계
        private Dictionary<StatDef, int> _tempCounts = new Dictionary<StatDef, int>();
        private int _nextPruneTick;

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

            if (Buffs == null) Buffs = new Dictionary<StatDef, int>();
            if (TempBuffs == null) TempBuffs = new List<TempEntry>();
            if (_tempCounts == null) _tempCounts = new Dictionary<StatDef, int>();

            RebuildTempCache(); // 로드 시 캐시 재구축
        }

        public override void FinalizeInit() { ResearchDescPatcher.UpdateDescription(); }
        public override void LoadedGame() { ResearchDescPatcher.UpdateDescription(); }

        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now < _nextPruneTick) return;
            _nextPruneTick = now + PruneIntervalTicks;

            if (TempBuffs == null || TempBuffs.Count == 0) return;

            // 만료 제거 + 캐시 감소
            for (int i = TempBuffs.Count - 1; i >= 0; i--)
            {
                var e = TempBuffs[i];
                if (e == null || e.ExpiryTick <= now || e.Stat == null)
                {
                    if (e != null && e.Stat != null && _tempCounts.TryGetValue(e.Stat, out var c))
                    {
                        if (c <= 1) _tempCounts.Remove(e.Stat); else _tempCounts[e.Stat] = c - 1;
                    }
                    TempBuffs.RemoveAt(i);
                }
            }
        }

        // ---------- 퍼블릭 API (다른 파일에서 호출) ----------

        public void ClearAllBuffs()
        {
            if (Buffs == null) Buffs = new Dictionary<StatDef, int>();
            Buffs.Clear();

            if (TempBuffs == null) TempBuffs = new List<TempEntry>();
            TempBuffs.Clear();

            if (_tempCounts == null) _tempCounts = new Dictionary<StatDef, int>();
            _tempCounts.Clear();

            TotalCompletions = 0;
            ResearchDescPatcher.UpdateDescription();
        }

        // 구버전 호환
        public void GrantRandomBuff() => GrantRandomBuff(true);

        public void GrantRandomBuff(bool permanent)
        {
            var settings = RRMod.Settings;
            // 후보군
            List<StatDef> cand = null;
            if (settings != null)
            {
                var e = settings.GetActiveCandidates();
                if (e != null) { cand = new List<StatDef>(e); }
            }
            if (cand == null || cand.Count == 0) return;

            var chosen = cand.RandomElement();
            if (permanent)
            {
                int stacks;
                if (!Buffs.TryGetValue(chosen, out stacks)) stacks = 0;
                Buffs[chosen] = stacks + 1;

                float incP = settings != null ? settings.incrementPercentPerm : 2f;
                Messages.Message($"Endless progress increases {chosen.label} by +{incP:0.#}% permanently.", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                int now = Find.TickManager.TicksGame;
                int durTicks = DaysToTicks(settings != null ? settings.tempDurationDays : 5f);
                if (TempBuffs == null) TempBuffs = new List<TempEntry>();
                TempBuffs.Add(new TempEntry(chosen, now + durTicks));

                // 캐시 증가
                if (_tempCounts == null) _tempCounts = new Dictionary<StatDef, int>();
                _tempCounts[chosen] = (_tempCounts.TryGetValue(chosen, out var c) ? c : 0) + 1;

                float incT = settings != null ? settings.incrementPercentTemp : 3f;
                Messages.Message($"Endless progress increases {chosen.label} by +{incT:0.#}% temporarily ({durTicks / (float)TicksPerDay:0.#}d).", MessageTypeDefOf.PositiveEvent);
            }

            TotalCompletions++;
            ResearchDescPatcher.UpdateDescription();
        }

        // 기존 코드와 호환용
        public int PermStacks(StatDef s)
        {
            int v; return Buffs != null && Buffs.TryGetValue(s, out v) ? v : 0;
        }
        public int ActiveTempStacks(StatDef s, int now)
        {
            // 핫패스에서 호출하지 말 것. (남아있는 레거시 대비)
            int cnt = 0;
            if (TempBuffs == null) return 0;
            for (int i = 0; i < TempBuffs.Count; i++)
            {
                var e = TempBuffs[i];
                if (e != null && e.Stat == s && e.ExpiryTick > now) cnt++;
            }
            return cnt;
        }
        public int? NextExpiryTicks(StatDef s, int now)
        {
            int? best = null;
            if (TempBuffs == null) return null;
            for (int i = 0; i < TempBuffs.Count; i++)
            {
                var e = TempBuffs[i];
                if (e == null || e.Stat != s || e.ExpiryTick <= now) continue;
                if (best == null || e.ExpiryTick < best.Value) best = e.ExpiryTick;
            }
            return best;
        }

        // 패치된 StatWorker용 O(1) 조회
        public int PermCountFast(StatDef s)
        {
            if (Buffs != null && Buffs.TryGetValue(s, out var v)) return v;
            return 0;
        }
        public int TempCountFast(StatDef s)
        {
            if (_tempCounts != null && _tempCounts.TryGetValue(s, out var v)) return v;
            return 0;
        }

        // ---------- 내부 유틸 ----------

        private void RebuildTempCache()
        {
            if (_tempCounts == null) _tempCounts = new Dictionary<StatDef, int>();
            _tempCounts.Clear();
            if (TempBuffs == null || TempBuffs.Count == 0) return;

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            for (int i = 0; i < TempBuffs.Count; i++)
            {
                var e = TempBuffs[i];
                if (e == null || e.Stat == null) continue;
                if (e.ExpiryTick <= now) continue;
                _tempCounts[e.Stat] = (_tempCounts.TryGetValue(e.Stat, out var c) ? c : 0) + 1;
            }
        }

        public static int DaysToTicks(float days)
        {
            if (days <= 0f) return 0;
            return (int)Math.Round(days * TicksPerDay);
        }
    }
}
