using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", typeof(PawnGenerationRequest))]
    public static class LapinNoBaldHairPatch
    {
        public static void Postfix(Pawn __result)
        {
            Pawn pawn = __result;
            if (pawn == null) return;
            if (pawn.def?.defName != "Lapin") return;
            if (pawn.story == null) return;
            if (pawn.story.hairDef == null) return;

            string hairDefName = pawn.story.hairDef.defName;

            if (!IsBaldHair(hairDefName)) return;

            HairDef replacement = DefDatabase<HairDef>.AllDefs
                .Where(h => h != null)
                .Where(h => !IsBaldHair(h.defName))
                .Where(h => h.styleTags == null || h.styleTags.Contains("LP_Style"))
                .RandomElementWithFallback();

            if (replacement == null)
            {
                replacement = DefDatabase<HairDef>.AllDefs
                    .Where(h => h != null && !IsBaldHair(h.defName))
                    .RandomElementWithFallback();
            }

            if (replacement != null)
            {
                pawn.story.hairDef = replacement;
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();

                Log.Message("[LapinRace] Replaced bald hair for " + pawn.Name + " with " + replacement.defName);
            }
        }

        private static bool IsBaldHair(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return false;

            return defName.Contains("Bald") ||
                   defName.Contains("Shaved") ||
                   defName.Contains("Balding");
        }
    }
}