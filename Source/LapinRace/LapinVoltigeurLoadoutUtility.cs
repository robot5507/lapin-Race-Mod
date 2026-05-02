using RimWorld;
using Verse;

namespace LapinRace
{
    public static class LapinVoltigeurLoadoutUtility
    {
        public static void GiveVoltigeurLoadout(Pawn pawn)
        {
           

            if (pawn.apparel != null)
            {
                for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
                {
                    pawn.apparel.WornApparel[i].Destroy();
                }
            }

           

            var uniform = ThingMaker.MakeThing(ThingDef.Named("LP_UniformVlotigeur")) as Apparel;
            if (uniform != null)
            {
                pawn.apparel?.Wear(uniform, false);
            }

            var hat = ThingMaker.MakeThing(ThingDef.Named("LP_Hat_Vlotigeur")) as Apparel;
            if (hat != null)
            {
                pawn.apparel?.Wear(hat, false);
            }
        }
    }
}
