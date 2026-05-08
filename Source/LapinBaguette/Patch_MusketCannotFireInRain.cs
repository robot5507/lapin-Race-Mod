using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinBaguette
{
    [HarmonyPatch(typeof(Verb_Shoot), "TryCastShot")]
    public static class Patch_MusketCannotFireInRain
    {
        [HarmonyPrefix]
        public static bool Prefix(Verb_Shoot __instance, ref bool __result)
        {
            if (__instance?.EquipmentSource == null)
                return true;

            Thing weapon = __instance.EquipmentSource;

            if (weapon.def == null)
                return true;

            string defName = weapon.def.defName;

            if (defName != "MusketWithBayonet" &&
                defName != "LP_LapinPistol" &&
                defName != "LP_MusketRifle")
                return true;

            Pawn caster = __instance.CasterPawn;
            if (caster == null || caster.Map == null)
                return true;

            Map map = caster.Map;
            IntVec3 cell = caster.Position;

            bool isRaining = map.weatherManager.curWeather.rainRate > 0.01f;
            bool underRoof = cell.Roofed(map);

            if (isRaining && !underRoof)
            {
                if (caster.IsColonistPlayerControlled)
                {
                    Messages.Message(
                        "비 때문에 화약이 젖어 발사할 수 없다!",
                        caster,
                        MessageTypeDefOf.RejectInput,
                        false
                    );
                }

                __result = false;
                return false;
            }

            return true;
        }
    }
}