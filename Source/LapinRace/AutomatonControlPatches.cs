using System;
using HarmonyLib;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class AutomatonControlPatches
    {
        private static readonly Harmony Harmony = new Harmony("LapinRace.AutomatonControlPatches");

        static AutomatonControlPatches()
        {
            // 절대 IsColonist 자체는 패치하지 말 것.
            // IsColonist를 true로 만들면 인간 작업 AI가 개입해서 오류가 나기 쉬움.

            // 전차/전투형 안드로이드가 정착민 비상소집 UI와 명령을 받을 수 있게 함.
            PatchPawnBoolGetter("IsColonistPlayerControlled", nameof(ForcePlayerControlledPostfix));
            PatchPawnBoolGetter("IsPlayerControlled", nameof(ForcePlayerControlledPostfix));

            // Biotech 메카 통제관/명령범위 시스템에서 제외.
            PatchPawnBoolGetter("IsColonyMech", nameof(ForceColonyMechFalsePostfix));
            PatchPawnBoolGetter("IsColonyMechPlayerControlled", nameof(ForceColonyMechFalsePostfix));

            Log.Message("[LapinRace] Unified automaton control patches loaded.");
        }

        private static void PatchPawnBoolGetter(string propertyName, string postfixName)
        {
            try
            {
                var getter = AccessTools.PropertyGetter(typeof(Pawn), propertyName);
                if (getter == null)
                {
                    Log.Message("[LapinRace] Pawn." + propertyName + " getter를 찾지 못했습니다.");
                    return;
                }

                var postfix = AccessTools.Method(typeof(AutomatonControlPatches), postfixName);
                Harmony.Patch(getter, postfix: new HarmonyMethod(postfix));

                Log.Message("[LapinRace] Pawn." + propertyName + " 패치 완료.");
            }
            catch (Exception ex)
            {
                Log.Warning("[LapinRace] Pawn." + propertyName + " 패치 실패: " + ex);
            }
        }

        private static void ForcePlayerControlledPostfix(Pawn __instance, ref bool __result)
        {
            if (IsPlayerCombatAutomaton(__instance))
            {
                __result = true;
            }
        }

        private static void ForceColonyMechFalsePostfix(Pawn __instance, ref bool __result)
        {
            if (IsPlayerCombatAutomaton(__instance))
            {
                __result = false;
            }
        }

        public static bool IsPlayerCombatAutomaton(Pawn pawn)
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