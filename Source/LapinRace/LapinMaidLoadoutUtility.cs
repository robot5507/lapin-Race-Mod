using RimWorld;
using Verse;

namespace LapinRace
{
    public static class LapinMaidLoadoutUtility
    {
        public static void GiveLapinMaidLoadout(Pawn pawn)
        {
            if (pawn.apparel == null) return;

            // 기존 옷 제거
            for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
            {
                pawn.apparel.WornApparel[i].Destroy();
            }

            // 메이드 복장
            WearIfExists(pawn, "LP_MaidDress");
            WearIfExists(pawn, "LP_Apron");
            WearIfExists(pawn, "LP_Hat_Maid");
        }

        private static void WearIfExists(Pawn pawn, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return;

            Apparel apparel = ThingMaker.MakeThing(def) as Apparel;
            if (apparel != null)
            {
                pawn.apparel.Wear(apparel, false);
            }
        }
    }
}

