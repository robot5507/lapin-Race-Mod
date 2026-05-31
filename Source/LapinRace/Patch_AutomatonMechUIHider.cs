using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_AutomatonMechUIHider
    {
        static Patch_AutomatonMechUIHider()
        {
            Harmony harmony = new Harmony("LapinRace.AutomatonMechUIHider");

            harmony.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.GetInspectString)),
                postfix: new HarmonyMethod(typeof(Patch_AutomatonMechUIHider), nameof(GetInspectStringPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                postfix: new HarmonyMethod(typeof(Patch_AutomatonMechUIHider), nameof(GetGizmosPostfix))
            );

            Log.Message("[LapinRace] Automaton mech UI hider loaded.");
        }

        private static void GetInspectStringPostfix(Pawn __instance, ref string __result)
        {
            if (!IsLapinAutomaton(__instance))
            {
                return;
            }

            if (string.IsNullOrEmpty(__result))
            {
                return;
            }

            string[] lines = __result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<string> keptLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (ShouldHideInspectLine(line))
                {
                    continue;
                }

                keptLines.Add(line);
            }

            __result = string.Join("\n", keptLines.ToArray());
        }

        private static void GetGizmosPostfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!IsLapinAutomaton(__instance))
            {
                return;
            }

            __result = FilterGizmos(__result);
        }

        private static IEnumerable<Gizmo> FilterGizmos(IEnumerable<Gizmo> gizmos)
        {
            if (gizmos == null)
            {
                yield break;
            }

            foreach (Gizmo gizmo in gizmos)
            {
                if (ShouldHideGizmo(gizmo))
                {
                    continue;
                }

                yield return gizmo;
            }
        }

        private static bool ShouldHideInspectLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            if (line.Contains("통제관"))
            {
                return true;
            }

            if (line.Contains("제어되지 않음"))
            {
                return true;
            }

            if (line.Contains("Overseer"))
            {
                return true;
            }

            if (line.Contains("Uncontrolled"))
            {
                return true;
            }

            if (line.Contains("No overseer"))
            {
                return true;
            }

            return false;
        }

        private static bool ShouldHideGizmo(Gizmo gizmo)
        {
            if (gizmo == null)
            {
                return false;
            }

            string typeName = gizmo.GetType().FullName ?? "";

            if (typeName.Contains("Repair") && typeName.Contains("Mech"))
            {
                return true;
            }

            if (gizmo is Command command)
            {
                string label = command.defaultLabel ?? "";
                string desc = command.defaultDesc ?? "";

                if (label.Contains("자동") && label.Contains("수리"))
                {
                    return true;
                }

                if (desc.Contains("자동") && desc.Contains("수리"))
                {
                    return true;
                }

                if (label.Contains("자동수리") || desc.Contains("자동수리"))
                {
                    return true;
                }

                if (label.Contains("Auto") && label.Contains("repair"))
                {
                    return true;
                }

                if (desc.Contains("Auto") && desc.Contains("repair"))
                {
                    return true;
                }

                if (label.Contains("Self-repair") || desc.Contains("Self-repair"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLapinAutomaton(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.def != null)
            {
                string defName = pawn.def.defName;

                if (defName == "LP_AutomatonTankRace" ||
       defName == "LP_PrototypeAndroidRace" ||
       defName == "LP_MaidAndroidRace" ||
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
      kindName == "LP_PrototypeAndroid" ||
      kindName == "LP_MaidAndroid" ||
      kindName == "LP_CombatAndroid")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
