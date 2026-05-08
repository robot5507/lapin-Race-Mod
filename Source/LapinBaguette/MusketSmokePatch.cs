using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LapinBaguette
{
    [HarmonyPatch(typeof(Projectile), "Launch", new System.Type[]
    {
        typeof(Thing),
        typeof(Vector3),
        typeof(LocalTargetInfo),
        typeof(LocalTargetInfo),
        typeof(ProjectileHitFlags),
        typeof(bool),
        typeof(Thing),
        typeof(ThingDef)
    })]
    public static class Patch_MusketSmokeOnLaunch
    {
        [HarmonyPostfix]
        public static void Postfix(
            Projectile __instance,
            Thing launcher,
            Vector3 origin)
        {
            if (__instance == null)
                return;

            Pawn pawn = launcher as Pawn;
            if (pawn == null || pawn.Map == null)
                return;

            ThingWithComps weapon = pawn.equipment?.Primary;
            if (weapon == null || weapon.def == null)
                return;

            if (weapon.def.defName != "MusketWithBayonet" &&
                weapon.def.defName != "LP_LapinPistol" &&
                weapon.def.defName != "LP_MusketRifle")
                return;

            Map map = pawn.Map;

            float aimAngle = pawn.Rotation.AsAngle;
            Vector3 forward = Quaternion.AngleAxis(aimAngle, Vector3.up) * Vector3.forward;
            Vector3 smokePos = origin + forward * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector3 randOffset = new Vector3(
                    Rand.Range(-0.12f, 0.12f),
                    0f,
                    Rand.Range(-0.12f, 0.12f)
                );

                FleckCreationData data = FleckMaker.GetDataStatic(
                    smokePos + randOffset,
                    map,
                    FleckDefOf.Smoke,
                    Rand.Range(0.9f, 1.5f)
                );

                data.velocityAngle = aimAngle + Rand.Range(-20f, 20f);
                data.velocitySpeed = Rand.Range(0.25f, 0.7f);
                data.rotationRate = Rand.Range(-15f, 15f);

                map.flecks.CreateFleck(data);
            }

            IntVec3 gasCell = smokePos.ToIntVec3();

            if (gasCell.InBounds(map) && Rand.Chance(0.4f))
            {
                GasUtility.AddGas(gasCell, map, GasType.BlindSmoke, 0.04f);
            }
        }
    }
}