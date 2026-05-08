using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", typeof(PawnGenerationRequest))]
    public static class Patch_LapinArtilleryBackstory
    {
        public static void Postfix(Pawn __result)
        {
            if (__result == null) return;
            if (__result.def == null || __result.def.defName != "Lapin") return;
            if (__result.story == null) return;

            BackstoryDef adulthood = __result.story.Adulthood;
            if (adulthood == null) return;
            if (adulthood.defName != "Lapin_Artillery") return;

            if (LapinPawnKindDefOf.LapinSoldierArtillery != null)
            {
                __result.kindDef = LapinPawnKindDefOf.LapinSoldierArtillery;
            }
            else
            {
                Log.Warning("[LapinRace] LapinSoldierArtillery DefOf is null");
            }

            LapinLapinSoldierArtilleryLoadoutUtility.GiveArtilleryLoadout(__result);

            Log.Message("[LapinRace] Applied Voltigeur loadout to " + (__result.Name?.ToStringFull ?? "unnamed pawn"));
        }
    }
}
