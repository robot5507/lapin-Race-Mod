using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    public static class LapinSpecialForcesMillitarLoadoutUtility
    {
        public static void GiveSpecialForcesMillitarLoadout(Pawn pawn)
        {
            if (pawn == null) return;

            if (pawn.apparel != null)
            {
                for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
                {
                    pawn.apparel.Remove(pawn.apparel.WornApparel[i]);
                }
            }

            pawn.equipment?.DestroyAllEquipment();

            WearIfExists(pawn, "LP_TacticalShieldBelt");
            WearIfExists(pawn, "LP_MaidDress");
            WearIfExists(pawn, "LP_Apron");
            WearIfExists(pawn, "LP_Hat_Maid");
            WearShieldBeltCharged(pawn);

            EquipIfExists(pawn, "LP_Weapon_Rapier");
        }

        private static void WearIfExists(Pawn pawn, string defName)
        {
            if (pawn.apparel == null) return;

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return;

            Apparel apparel = ThingMaker.MakeThing(def) as Apparel;
            if (apparel != null)
            {
                pawn.apparel.Wear(apparel, false);
            }
        }

        private static void WearShieldBeltCharged(Pawn pawn)
        {
            if (pawn.apparel == null) return;

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("LP_TacticalShieldBelt");
            if (def == null) return;

            Apparel belt = ThingMaker.MakeThing(def) as Apparel;
            if (belt == null) return;

            pawn.apparel.Wear(belt, false);

            CompShield shield = belt.GetComp<CompShield>();
            if (shield != null)
            {
                Traverse.Create(shield).Field("energy").SetValue(2f);
            }
        }

        private static void EquipIfExists(Pawn pawn, string defName)
        {
            if (pawn.equipment == null) return;

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return;

            ThingWithComps weapon = ThingMaker.MakeThing(def) as ThingWithComps;
            if (weapon != null)
            {
                pawn.equipment.AddEquipment(weapon);
            }
        }
    }
}