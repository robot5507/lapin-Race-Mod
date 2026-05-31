using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_AutomatonVerbTargetControl
    {
        private static readonly Harmony Harmony = new Harmony("LapinRace.AutomatonVerbTargetControl");

        static Patch_AutomatonVerbTargetControl()
        {
            PatchCommandVerbTargetValidateTarget();

            Log.Message("[LapinRace] Automaton verb target control patch loaded.");
        }

        private static void PatchCommandVerbTargetValidateTarget()
        {
            Type commandVerbTargetType = AccessTools.TypeByName("Verse.Command_VerbTarget");
            if (commandVerbTargetType == null)
            {
                Log.Warning("[LapinRace] Verse.Command_VerbTarget 타입을 찾지 못했습니다.");
                return;
            }

            MethodInfo[] methods = commandVerbTargetType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];

                if (method == null)
                {
                    continue;
                }

                if (method.Name != "ValidateTarget")
                {
                    continue;
                }

                if (method.ReturnType != typeof(bool))
                {
                    continue;
                }

                try
                {
                    Harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(Patch_AutomatonVerbTargetControl), nameof(ValidateTargetPrefix))
                    );

                    Log.Message("[LapinRace] Patched Command_VerbTarget.ValidateTarget: " + method);
                }
                catch (Exception ex)
                {
                    Log.Warning("[LapinRace] Command_VerbTarget.ValidateTarget 패치 실패: " + ex);
                }
            }
        }

        private static bool ValidateTargetPrefix(object __instance, object[] __args, ref bool __result)
        {
            Verb verb = GetVerb(__instance);

            if (verb == null)
            {
                return true;
            }

            Pawn casterPawn = verb.CasterPawn;
            if (!AutomatonControlPatches.IsPlayerCombatAutomaton(casterPawn))
            {
                return true;
            }

            LocalTargetInfo target = LocalTargetInfo.Invalid;

            if (__args != null && __args.Length > 0)
            {
                for (int i = 0; i < __args.Length; i++)
                {
                    if (__args[i] is LocalTargetInfo localTarget)
                    {
                        target = localTarget;
                        break;
                    }
                }
            }

            if (!target.IsValid)
            {
                __result = false;
                return false;
            }

            try
            {
                if (!verb.CanHitTarget(target))
                {
                    __result = false;
                    return false;
                }
            }
            catch
            {
                // CanHitTarget에서 예외가 나면 원래 로직으로 넘긴다.
                return true;
            }

            __result = true;
            return false;
        }

        private static Verb GetVerb(object commandVerbTarget)
        {
            if (commandVerbTarget == null)
            {
                return null;
            }

            Type type = commandVerbTarget.GetType();

            FieldInfo field = type.GetField(
                "verb",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (field != null)
            {
                return field.GetValue(commandVerbTarget) as Verb;
            }

            PropertyInfo property = type.GetProperty(
                "verb",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (property != null)
            {
                return property.GetValue(commandVerbTarget, null) as Verb;
            }

            return null;
        }
    }
}