using HarmonyLib;
using Verse;

namespace RepeatableResearch {
    [StaticConstructorOnStartup]
    public static class RR_Bootstrap {
        static RR_Bootstrap() {
            try {
                var h = new Harmony("com.memini.repeatableresearch");
                h.PatchAll();
                Log.Message("[RR] bootstrap ok: PatchAll() ran");
            } catch (System.Exception e) {
                Log.Error("[RR] bootstrap failed: " + e);
            }
        }
    }
}
