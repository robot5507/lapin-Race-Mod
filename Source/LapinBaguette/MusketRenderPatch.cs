using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LapinBaguette
{
    [HarmonyPatch]
    public static class Patch_DrawEquipmentAiming_MusketOffset
    {
        static MethodBase TargetMethod()
        {
            MethodInfo method =
                AccessTools.Method(typeof(PawnRenderUtility), "DrawEquipmentAiming",
                    new[] { typeof(Thing), typeof(Vector3), typeof(float) });

            if (method != null)
                return method;

            System.Type pawnRendererType = AccessTools.TypeByName("Verse.PawnRenderer");
            if (pawnRendererType != null)
            {
                method = AccessTools.Method(pawnRendererType, "DrawEquipmentAiming",
                    new[] { typeof(Thing), typeof(Vector3), typeof(float) });

                if (method != null)
                    return method;
            }

            Log.Error("[Lapin Baguette] DrawEquipmentAiming target method not found.");
            return null;
        }

        [HarmonyPrefix]
        public static void Prefix(Thing eq, ref Vector3 drawLoc, float aimAngle)
        {
            if (eq?.def == null)
                return;

            if (eq.def.defName != "MusketWithBayonet"&&
                eq.def.defName != "LP_MusketRifle")
                return;

            float forwardOffset = 0.31f;

            Vector3 forward = Quaternion.AngleAxis(aimAngle, Vector3.up) * Vector3.forward;
            drawLoc += forward * forwardOffset;
        }
    }
}