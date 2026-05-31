using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_AutomatonVerbGizmoEnable
    {
        private static readonly Harmony Harmony = new Harmony("LapinRace.AutomatonVerbGizmoEnable");

        static Patch_AutomatonVerbGizmoEnable()
        {
            Harmony.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                postfix: new HarmonyMethod(typeof(Patch_AutomatonVerbGizmoEnable), nameof(GetGizmosPostfix))
            );

            Log.Message("[LapinRace] Automaton verb gizmo enable patch loaded.");
        }

        private static void GetGizmosPostfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!IsPlayerCombatAutomaton(__instance))
            {
                return;
            }

            __result = EnableAutomatonVerbGizmos(__result);
        }

        private static IEnumerable<Gizmo> EnableAutomatonVerbGizmos(IEnumerable<Gizmo> gizmos)
        {
            if (gizmos == null)
            {
                yield break;
            }

            foreach (Gizmo gizmo in gizmos)
            {
                TryEnableVerbTargetCommand(gizmo);
                yield return gizmo;
            }
        }

        private static void TryEnableVerbTargetCommand(Gizmo gizmo)
        {
            if (gizmo == null)
            {
                return;
            }

            Type type = gizmo.GetType();

            // 총/포 타겟팅 명령만 처리
            if (type.FullName != "Verse.Command_VerbTarget")
            {
                return;
            }

            // 보호 필드라 직접 접근하지 않고 reflection으로 해제
            SetFieldIfExists(gizmo, "disabled", false);
            SetFieldIfExists(gizmo, "disabledReason", null);

            // 혹시 Command 쪽 기본 타입에 필드가 있을 경우도 처리
            Command command = gizmo as Command;
            if (command != null)
            {
                SetFieldIfExists(command, "disabled", false);
                SetFieldIfExists(command, "disabledReason", null);
            }
        }

        private static void SetFieldIfExists(object obj, string fieldName, object value)
        {
            if (obj == null)
            {
                return;
            }

            Type type = obj.GetType();

            while (type != null)
            {
                try
                {
                    FieldInfo field = type.GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                    if (field != null)
                    {
                        field.SetValue(obj, value);
                        return;
                    }
                }
                catch
                {
                    return;
                }

                type = type.BaseType;
            }
        }

        private static bool IsPlayerCombatAutomaton(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
            {
                return false;
            }

            if (pawn.def != null)
            {
                string defName = pawn.def.defName;

                if (defName == "LP_AutomatonTankRace" ||
                    defName == "LP_CombatAndroidRace")
                {
                    return true;
                }
            }

            if (pawn.kindDef != null)
            {
                string kindName = pawn.kindDef.defName;

                if (kindName == "LP_AutomatonTank_Player" ||
                    kindName == "LP_AutomatonTank" ||
                    kindName == "LP_AutomatonTank_Enemy" ||
                    kindName == "LP_CombatAndroid")
                {
                    return true;
                }
            }

            return false;
        }
    }
}