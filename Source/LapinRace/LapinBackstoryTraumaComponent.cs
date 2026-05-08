using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", typeof(PawnGenerationRequest))]
    public static class LapinWarTraumaPatch
    {
        static void Postfix(Pawn __result)
        {
            Pawn pawn = __result;
            if (pawn == null) return;
            if (pawn.def?.defName != "Lapin") return;
            if (pawn.story == null) return;

            if (HasBackstory(pawn, "Lapin_Straggler"))
            {
                GiveTrait(pawn);
            }
        }

        private static bool HasBackstory(Pawn pawn, string key)
        {
            return pawn.story.Childhood?.defName == key ||
                   pawn.story.Childhood?.identifier == key ||
                   pawn.story.Adulthood?.defName == key ||
                   pawn.story.Adulthood?.identifier == key;
        }

        private static void GiveTrait(Pawn pawn)
        {
            TraitDef traitDef = DefDatabase<TraitDef>.GetNamedSilentFail("LP_WarTrauma");
            if (traitDef == null) return;

            if (pawn.story.traits.HasTrait(traitDef)) return;

            pawn.story.traits.GainTrait(new Trait(traitDef));

            Log.Message("[LapinRace] Added war trauma trait (generator) to " + pawn.LabelShort);
        }
    }
}