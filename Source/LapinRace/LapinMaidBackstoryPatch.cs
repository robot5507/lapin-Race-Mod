using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", typeof(PawnGenerationRequest))]
    public static class Patch_LapinLapinMaidBackstory
    {
        public static void Postfix(Pawn __result)
        {
            if (__result == null) return;
            if (__result.def == null || __result.def.defName != "Lapin") return;
            if (__result.story == null) return;

            BackstoryDef adulthood = __result.story.Adulthood;
            if (adulthood == null) return;
            if (adulthood.defName != "Lapin_Maid") return;

            if (LapinPawnKindDefOf.LapinMaid != null)
            {
                __result.kindDef = LapinPawnKindDefOf.LapinMaid;
            }
            else
            {
                Log.Warning("[LapinRace] LapinMaid DefOf is null");
            }

            LapinMaidLoadoutUtility.GiveLapinMaidLoadout(__result);

            Log.Message("[LapinRace] Applied LapinMaid loadout to " + (__result.Name?.ToStringFull ?? "unnamed pawn"));
        }
    }
}
