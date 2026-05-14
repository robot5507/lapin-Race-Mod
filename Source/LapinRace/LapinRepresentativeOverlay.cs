using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class LapinRepresentativeOverlay
    {
        private static readonly Material QuestionMarkMat =
            MaterialPool.MatFrom(
                "UI/Overlays/QuestionMark",
                ShaderDatabase.MetaOverlay
            );

        public static void DrawOverlay(Pawn pawn)
        {
            if (pawn == null ||
                pawn.Destroyed ||
                pawn.Map == null ||
                pawn.Dead ||
                !pawn.Spawned)
            {
                return;
            }

            if (!ShouldShowOverlay(pawn))
            {
                return;
            }

            Vector3 drawPos = pawn.DrawPos;

            // 위치 조정
            drawPos.x -= 0.28f;
            drawPos.z += 0.82f;

            drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            // 깜빡이는 알파값
            float pulse =
      0.25f +
      Mathf.Abs(
          Mathf.Sin(
              Time.time * 2.0f
          )
      ) * 0.75f;

            Color color = new Color(
                1f,
                1f,
                1f,
                pulse
            );

            Material material = MaterialPool.MatFrom(
                "UI/Overlays/QuestionMark",
                ShaderDatabase.MetaOverlay,
                color
            );

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(
                    drawPos,
                    Quaternion.identity,
                    new Vector3(0.85f, 1f, 0.85f)
                ),
                material,
                0
            );
        }

        private static bool ShouldShowOverlay(Pawn pawn)
        {
            if (pawn.kindDef == null ||
                pawn.kindDef.defName != "LapinNobleOfficer")
            {
                return false;
            }

            Lord lord = pawn.GetLord();

            if (lord == null)
            {
                return false;
            }

            return lord.LordJob is LordJob_LapinWaitForSupplies;
        }
    }

    [StaticConstructorOnStartup]
    public static class LapinRepresentativeOverlayPatch
    {
        static LapinRepresentativeOverlayPatch()
        {
            Harmony harmony = new Harmony("LapinRace.RepresentativeOverlay");

            harmony.Patch(
                AccessTools.Method(typeof(Pawn), "DrawAt"),
                postfix: new HarmonyMethod(
                    typeof(LapinRepresentativeOverlayPatch),
                    nameof(Postfix)
                )
            );
        }

        public static void Postfix(Pawn __instance)
        {
            LapinRepresentativeOverlay.DrawOverlay(__instance);
        }
    }
}