using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    public class HediffCompProperties_ReduceHungerFall : HediffCompProperties
    {
        public float hungerFallMultiplier = 0.01f;

        public HediffCompProperties_ReduceHungerFall()
        {
            compClass = typeof(HediffComp_ReduceHungerFall);
        }
    }

    public class HediffComp_ReduceHungerFall : HediffComp
    {
        public HediffCompProperties_ReduceHungerFall Props =>
            (HediffCompProperties_ReduceHungerFall)props;
    }

    [StaticConstructorOnStartup]
    public static class LapinRace_HungerPatchStartup
    {
        static LapinRace_HungerPatchStartup()
        {
            Harmony harmony = new Harmony("LapinRace.HungerPatch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
    public static class Patch_NeedFood_FoodFallPerTickAssumingCategory
    {
        public static void Postfix(Need_Food __instance, ref float __result)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

            if (pawn?.health?.hediffSet == null)
                return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("LP_JambonBeurreFullness")
            );

            if (hediff == null)
                return;

            HediffComp_ReduceHungerFall comp = hediff.TryGetComp<HediffComp_ReduceHungerFall>();

            if (comp == null)
                return;

            __result *= comp.Props.hungerFallMultiplier;
        }
    }
}