using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinBaguette
{
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip")]
    public static class LapinMusketShieldPatch
    {
        public static void Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (__result) return;
            if (thing == null || pawn == null) return;
            if (pawn.def?.defName != "Lapin") return;

            // 1. 보호막 벨트를 낀 상태에서 라핀 머스킷 장착 허용
            if (IsLapinMusket(thing) && PawnWearsBlockingShield(pawn))
            {
                __result = true;
                cantReason = null;
                return;
            }

            // 2. 라핀 머스킷을 든 상태에서 보호막 벨트 장착 허용
            if (IsBlockingShield(thing) && PawnHasLapinMusket(pawn))
            {
                __result = true;
                cantReason = null;
            }
        }

        private static bool IsLapinMusket(Thing thing)
        {
            string defName = thing.def?.defName;
            return defName == "MusketWithBayonet"
                || defName == "LP_MusketRifle";
        }

        private static bool PawnHasLapinMusket(Pawn pawn)
        {
            ThingWithComps weapon = pawn.equipment?.Primary;
            return weapon != null && IsLapinMusket(weapon);
        }

        private static bool PawnWearsBlockingShield(Pawn pawn)
        {
            return pawn.apparel?.WornApparel.Any(IsBlockingShield) ?? false;
        }

        private static bool IsBlockingShield(Thing thing)
        {
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null) return false;

            CompShield shield = thingWithComps.GetComp<CompShield>();
            return shield?.Props?.blocksRangedWeapons == true;
        }
    }
}