using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RepeatableResearch {
    public class RRMod : Mod {
        public static RRSettings Settings;
        private string _search = "";
        private Vector2 _leftScroll, _rightScroll;

        public RRMod(ModContentPack pack) : base(pack) {
            Settings = GetSettings<RRSettings>();
        }




        public override string SettingsCategory() => "Repeatable Research";

        public override void DoSettingsWindowContents(Rect inRect) {
            var lsTop = new Listing_Standard();

            // give more height to fit the slider
            var top = new Rect(inRect.x, inRect.y, inRect.width, 92f);
            lsTop.Begin(top);

            lsTop.Label("Choose which stats can be rolled. If none are selected, a fixed default set is used.");
            lsTop.GapLine();

            // slider: per-completion bonus
            lsTop.Label($"Increase per completion: {Settings.incrementPercent:0.0}%");
            Settings.incrementPercent = Widgets.HorizontalSlider(
                lsTop.GetRect(24f), Settings.incrementPercent, 0.5f, 3.0f, false, "", "0.5%", "3.0%", 0.5f);

            if (lsTop.ButtonText("Reset to defaults")) {
                Settings.userStatNames.Clear();
                WriteSettings();
            }
            lsTop.End();

            float pad = 6f;
            var body = new Rect(inRect.x, inRect.y + top.height + 4f, inRect.width, inRect.height - top.height - 4f);
            float colW = (body.width - pad) / 2f;

            DrawLeft(new Rect(body.x, body.y, colW, body.height));
            DrawRight(new Rect(body.x + colW + pad, body.y, colW, body.height));
        }


        private static IEnumerable<StatDef> DefaultPoolForUI() =>
    RRPool.PoolNames
        .Select(n => DefDatabase<StatDef>.GetNamedSilentFail(n))
        .Where(sd => sd != null)
        .OrderBy(sd => sd.label ?? sd.defName);


        // RRMod.cs 상단 usings 유지: using RimWorld; using UnityEngine; using Verse;

        private static float CalcRowHeight(string text, float labelWidth) {
            var oldAnchor = Text.Anchor; var oldWrap = Text.WordWrap;
            Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
            float h = Mathf.Ceil(Text.CalcHeight(text, labelWidth));
            Text.Anchor = oldAnchor; Text.WordWrap = oldWrap;
            return Mathf.Max(24f, h);
        }

        private void DrawLeft(Rect rect) {
            var ls = new Listing_Standard();
            ls.Begin(rect);

            // search
            var rSearch = ls.GetRect(28f);
            Widgets.Label(new Rect(rSearch.x, rSearch.y, 60f, 28f), "Search:");
            _search = Widgets.TextField(new Rect(rSearch.x + 62f, rSearch.y, rSearch.width - 62f, 28f), _search);
            ls.Gap(4f);

            // list source
            var listRect = ls.GetRect(rect.height - 64f);
            var source = string.IsNullOrWhiteSpace(_search) ? DefaultPoolForUI() : RRSettings.AllStatsForUI(_search);
            var items = source.Where(sd => !Settings.userStatNames.Contains(sd.defName)).ToList();

            const float btnW = 76f;
            float labelW = listRect.width - 16f - btnW - 4f;

            // 1st pass: total height
            float total = 0f;
            foreach (var sd in items) {
                string text = $"{sd.label.CapitalizeFirst()} ({sd.defName})";
                total += CalcRowHeight(text, labelW) + 2f;
            }
            if (items.Count > 0) total -= 2f; // last gap 제거
            float viewH = Mathf.Max(listRect.height, total);
            var view = new Rect(0, 0, listRect.width - 16f, viewH);

            // 2nd pass: draw
            Widgets.BeginScrollView(listRect, ref _leftScroll, view);
            float y = 0f;
            foreach (var sd in items) {
                string text = $"{sd.label.CapitalizeFirst()} ({sd.defName})";
                float rowH = CalcRowHeight(text, labelW);
                var labelRect = new Rect(0, y, labelW, rowH);
                var btnRect = new Rect(labelRect.xMax + 4f, y + (rowH - 24f) * 0.5f, btnW, 24f);

                var oldAnchor = Text.Anchor; var oldWrap = Text.WordWrap;
                Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
                Widgets.Label(labelRect, text);
                Text.Anchor = oldAnchor; Text.WordWrap = oldWrap;

                if (Widgets.ButtonText(btnRect, "Add")) {
                    Settings.userStatNames.Add(sd.defName);
                    WriteSettings();
                }
                TooltipHandler.TipRegion(labelRect, text);

                y += rowH + 2f;
            }
            Widgets.EndScrollView();

            ls.End();
        }

        private void DrawRight(Rect rect) {
            var ls = new Listing_Standard();
            ls.Begin(rect);
            ls.Label("Active candidates (rolled randomly):");
            ls.Gap(4f);

            var listRect = ls.GetRect(rect.height - 64f);
            var chosen = Settings.userStatNames
                .Select(n => DefDatabase<StatDef>.GetNamedSilentFail(n))
                .Where(sd => sd != null)
                .OrderBy(sd => sd.label ?? sd.defName)
                .ToList();
            if (chosen.Count == 0) {
                chosen = RRPool.DefaultActive
                    .Select(n => DefDatabase<StatDef>.GetNamedSilentFail(n))
                    .Where(sd => sd != null)
                    .OrderBy(sd => sd.label ?? sd.defName)
                    .ToList();
            }

            const float btnW = 76f;
            float labelW = listRect.width - 16f - btnW - 4f;

            // height pass
            float total = 0f;
            foreach (var sd in chosen) {
                string text = $"{sd.label.CapitalizeFirst()} ({sd.defName})";
                total += CalcRowHeight(text, labelW) + 2f;
            }
            if (chosen.Count > 0) total -= 2f;
            float viewH = Mathf.Max(listRect.height, total);
            var view = new Rect(0, 0, listRect.width - 16f, viewH);

            // draw pass
            Widgets.BeginScrollView(listRect, ref _rightScroll, view);
            float y = 0f;
            foreach (var sd in chosen) {
                string text = $"{sd.label.CapitalizeFirst()} ({sd.defName})";
                float rowH = CalcRowHeight(text, labelW);
                var labelRect = new Rect(0, y, labelW, rowH);
                var btnRect = new Rect(labelRect.xMax + 4f, y + (rowH - 24f) * 0.5f, btnW, 24f);

                var oldAnchor = Text.Anchor; var oldWrap = Text.WordWrap;
                Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
                Widgets.Label(labelRect, text);
                Text.Anchor = oldAnchor; Text.WordWrap = oldWrap;

                if (Settings.userStatNames.Contains(sd.defName)) {
                    if (Widgets.ButtonText(btnRect, "Remove")) {
                        Settings.userStatNames.Remove(sd.defName);
                        WriteSettings();
                    }
                }
                TooltipHandler.TipRegion(labelRect, text);

                y += rowH + 2f;
            }
            Widgets.EndScrollView();

            ls.End();
        }


    }
}
