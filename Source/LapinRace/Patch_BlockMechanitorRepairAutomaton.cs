using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_BlockMechanitorRepairAutomaton
    {
        static Patch_BlockMechanitorRepairAutomaton()
        {
            Harmony harmony = new Harmony("LapinRace.BlockMechanitorRepairAutomaton");

            foreach (Type type in AccessTools.AllTypes())
            {
                if (type == null || type.FullName == null)
                {
                    continue;
                }

                string name = type.FullName;

                if (!name.Contains("Repair") || !name.Contains("Mech"))
                {
                    continue;
                }

                MethodInfo method = AccessTools.Method(type, "HasJobOnThing");

                if (method == null || method.ReturnType != typeof(bool))
                {
                    continue;
                }

                try
                {
                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(Patch_BlockMechanitorRepairAutomaton), nameof(HasJobOnThingPrefix))
                    );

                    Log.Message("[LapinRace] Blocked mechanitor repair hook: " + name + ".HasJobOnThing");
                }
                catch (Exception ex)
                {
                    Log.Warning("[LapinRace] Failed to patch mechanitor repair hook: " + name + "\n" + ex);
                }
            }
        }

        private static bool HasJobOnThingPrefix(object[] __args, ref bool __result)
        {
            if (__args == null)
            {
                return true;
            }

            for (int i = 0; i < __args.Length; i++)
            {
                Pawn pawn = __args[i] as Pawn;

                if (IsAutomatonTank(pawn))
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        private static bool IsAutomatonTank(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.def != null && pawn.def.defName == "LP_AutomatonTankRace")
            {
                return true;
            }

            if (pawn.kindDef != null)
            {
                string kindName = pawn.kindDef.defName;

                if (kindName == "LP_AutomatonTank_Player" ||
                    kindName == "LP_AutomatonTank")
                {
                    return true;
                }
            }

            return false;
        }
    }
}