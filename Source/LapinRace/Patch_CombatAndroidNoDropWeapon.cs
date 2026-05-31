using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_CombatAndroidNoDropWeapon
    {
        private static readonly Harmony Harmony = new Harmony("LapinRace.CombatAndroidNoDropWeapon");

        static Patch_CombatAndroidNoDropWeapon()
        {
            PatchDropAllEquipment();
            PatchTryDropEquipment();

            Log.Message("[LapinRace] Combat android no-drop weapon patches loaded.");
        }

        private static void PatchDropAllEquipment()
        {
            MethodInfo method = AccessTools.Method(typeof(Pawn_EquipmentTracker), "DropAllEquipment");

            if (method == null)
            {
                Log.Warning("[LapinRace] Pawn_EquipmentTracker.DropAllEquipment 메서드를 찾지 못했습니다.");
                return;
            }

            Harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(Patch_CombatAndroidNoDropWeapon), nameof(DropAllEquipmentPrefix))
            );
        }

        private static void PatchTryDropEquipment()
        {
            MethodInfo[] methods = typeof(Pawn_EquipmentTracker).GetMethods(
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

                if (method.Name != "TryDropEquipment")
                {
                    continue;
                }

                try
                {
                    Harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(Patch_CombatAndroidNoDropWeapon), nameof(TryDropEquipmentPrefix))
                    );

                    Log.Message("[LapinRace] Patched Pawn_EquipmentTracker.TryDropEquipment: " + method);
                }
                catch (Exception ex)
                {
                    Log.Warning("[LapinRace] TryDropEquipment 패치 실패: " + ex);
                }
            }
        }

        private static bool DropAllEquipmentPrefix(Pawn_EquipmentTracker __instance)
        {
            Pawn pawn = GetPawn(__instance);

            if (pawn == null)
            {
                return true;
            }

            if (!CompCombatAndroidKeepWeapon.IsCombatAndroid(pawn))
            {
                return true;
            }

            // 죽었을 때는 전리품으로 떨어뜨리고 싶으면 true.
            // 다운/기절/강제 드롭은 막는다.
            if (pawn.Dead)
            {
                return true;
            }

            return false;
        }

        private static bool TryDropEquipmentPrefix(Pawn_EquipmentTracker __instance, object[] __args, ref bool __result)
        {
            Pawn pawn = GetPawn(__instance);

            if (pawn == null)
            {
                return true;
            }

            if (!CompCombatAndroidKeepWeapon.IsCombatAndroid(pawn))
            {
                return true;
            }

            if (pawn.Dead)
            {
                return true;
            }

            ThingWithComps equipment = null;

            if (__args != null && __args.Length > 0)
            {
                equipment = __args[0] as ThingWithComps;
            }

            if (equipment == null)
            {
                return true;
            }

            if (!CompCombatAndroidKeepWeapon.IsBoundWeaponOf(pawn, equipment))
            {
                return true;
            }

            // 전투형 안드로이드의 내장 머스킷은 떨어뜨릴 수 없다.
            // 수동 드롭/다운 드롭 모두 실패 처리.
            __result = false;

            // out ThingWithComps resultingEq가 있다면 null로 세팅
            TrySetOutResultingEquipmentNull(__args);

            return false;
        }

        private static void TrySetOutResultingEquipmentNull(object[] args)
        {
            if (args == null)
            {
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is ThingWithComps)
                {
                    // 첫 번째 인자는 보통 드롭하려는 장비라서 건드리지 않는다.
                    continue;
                }
            }
        }

        private static Pawn GetPawn(Pawn_EquipmentTracker tracker)
        {
            if (tracker == null)
            {
                return null;
            }

            Type type = tracker.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(
                    "pawn",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

                if (field != null)
                {
                    return field.GetValue(tracker) as Pawn;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
