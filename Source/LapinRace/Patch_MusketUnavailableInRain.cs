using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.Available))]
    public static class Patch_MusketUnavailableInRain
    {
        public static void Postfix(Verb __instance, ref bool __result)
        {
            if (!__result)
                return;

            if (__instance.verbProps == null || __instance.verbProps.range <= 1.42f)
                return;

            Pawn pawn = __instance.CasterPawn;
            if (pawn?.Map == null)
                return;

            ThingWithComps weapon = pawn.equipment?.Primary;
            if (weapon == null)
                return;

            string defName = weapon.def.defName;

            bool isLapinMusket =
                defName == "MusketWithBayonet" ||
                defName == "LP_MusketRifle" ||
                defName == "LP_LapinPistol";

            if (!isLapinMusket)
                return;

            bool raining = pawn.Map.weatherManager.curWeather.rainRate > 0.25f;

            if (raining)
                __result = false;
        }
    }
}