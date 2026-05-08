using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", typeof(PawnGenerationRequest))]
    public static class Patch_LapinVoltigeurBackstory
    {
        public static void Postfix(Pawn __result)
        {
            if (__result == null) return;
            if (__result.def == null || __result.def.defName != "Lapin") return;
            if (__result.story == null) return;

            BackstoryDef adulthood = __result.story.Adulthood;
            if (adulthood == null) return;
            if (adulthood.defName != "Lapin_Voltigeur") return;

            if (LapinPawnKindDefOf.LapinSoldier != null)
            {
                __result.kindDef = LapinPawnKindDefOf.LapinSoldier;
            }
            else
            {
                Log.Warning("[LapinRace] LapinSoldier DefOf is null");
            }

            LapinVoltigeurLoadoutUtility.GiveVoltigeurLoadout(__result);

            Log.Message("[LapinRace] Applied Voltigeur loadout to " + (__result.Name?.ToStringFull ?? "unnamed pawn"));
        }
    }
}
