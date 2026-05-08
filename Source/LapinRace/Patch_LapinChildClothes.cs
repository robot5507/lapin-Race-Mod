using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", typeof(PawnGenerationRequest))]
    public static class Patch_LapinChildClothes
    {
        public static void Postfix(Pawn __result)
        {
            Pawn pawn = __result;
            if (pawn == null) return;
            if (pawn.def?.defName != "Lapin") return;
            if (pawn.apparel == null) return;

            bool isBabyOrChild =
                pawn.DevelopmentalStage.Has(DevelopmentalStage.Child);

            if (!isBabyOrChild) return;

            // 이미 옷 있으면 건드리지 않음
            if (pawn.apparel.WornApparelCount > 0) return;

            Apparel childDress = ThingMaker.MakeThing(ThingDef.Named("LP_Childdress")) as Apparel;
            if (childDress != null)
            {
                pawn.apparel.Wear(childDress, false);
            }
        }
    }
}