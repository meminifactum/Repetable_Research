using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RepeatableResearch {
    public class RepeatableResearchComp : GameComponent {
        // stat -> stacks
        public Dictionary<StatDef, int> Buffs = new Dictionary<StatDef, int>();

        // for Scribe of Dictionary
        private List<StatDef> _keys = new List<StatDef>();
        private List<int> _vals = new List<int>();

        public string BaseDesc;

        public RepeatableResearchComp() { }
        public RepeatableResearchComp(Game game) { }

        public override void ExposeData() {
            base.ExposeData();

            // Persist dictionary properly
            Scribe_Collections.Look(ref Buffs, "RR_Buffs",
                LookMode.Def, LookMode.Value, ref _keys, ref _vals);

            Scribe_Values.Look(ref BaseDesc, "RR_BaseDesc");
        }

        public override void FinalizeInit() { ResearchDescPatcher.UpdateDescription(); }
        public override void LoadedGame() { ResearchDescPatcher.UpdateDescription(); }

        public void GrantRandomBuff() {
            var cand = RRMod.Settings != null
                ? RRMod.Settings.GetActiveCandidates().ToList()
                : null;
            if (cand == null || cand.Count == 0) return;

            var chosen = cand.RandomElement();

            // increment stack
            int stacks;
            if (!Buffs.TryGetValue(chosen, out stacks)) stacks = 0;
            Buffs[chosen] = stacks + 1;

            // message
            float inc = RRMod.Settings != null ? RRMod.Settings.incrementPercent : 2f;
            Messages.Message(
                $"Endless progress increases {chosen.label} by +{inc:0.#}%.",
                MessageTypeDefOf.PositiveEvent
            );

            // refresh description panel
            ResearchDescPatcher.UpdateDescription();
        }
    }
}
