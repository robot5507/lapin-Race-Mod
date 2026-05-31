using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_CompRefuelable_AutomatonCorpse
    {
        static Patch_CompRefuelable_AutomatonCorpse()
        {
            Harmony harmony = new Harmony("LapinRace.CompRefuelable.AutomatonCorpse");
            harmony.Patch(
                AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.PostDraw)),
                prefix: new HarmonyMethod(typeof(Patch_CompRefuelable_AutomatonCorpse), nameof(PostDrawPrefix))
            );
        }

        private static bool PostDrawPrefix(CompRefuelable __instance)
        {
            if (__instance == null || __instance.parent == null)
            {
                return false;
            }

            Pawn pawn = __instance.parent as Pawn;

            if (pawn == null)
            {
                return true;
            }

            if (!IsAutomatonTank(pawn))
            {
                return true;
            }

            /*
             * 전차가 시체 안의 innerPawn으로 그려질 때는 Spawned가 false이거나 Dead 상태다.
             * 이때 CompRefuelable.PostDraw가 연료 게이지를 그리려다 NRE를 낸다.
             */
            if (pawn.Dead || !pawn.Spawned)
            {
                return false;
            }

            return true;
        }

        private static bool IsAutomatonTank(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.def != null && pawn.def.defName == "LP_AutomatonTankRace")
            {
                return true;
            }

            if (pawn.kindDef != null &&
                (pawn.kindDef.defName == "LP_AutomatonTank_Player" ||
                 pawn.kindDef.defName == "LP_AutomatonTank"))
            {
                return true;
            }

            return false;
        }
    }
}