using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;

namespace LapinBaguette
{
    [StaticConstructorOnStartup]
    public static class BaguetteArmPatch
    {
        static BaguetteArmPatch()
        {
            var harmony = new Harmony("com.lapin.baguette");
            harmony.PatchAll();
            Log.Message("[Lapin Baguette] 바게트 팔/다리 패치 로드 완료!");
        }
    }

    // 수술 메시지
    [HarmonyPatch(typeof(Hediff_AddedPart), nameof(Hediff_AddedPart.PostAdd))]
    public static class Patch_BaguettePartAdded
    {
        [HarmonyPostfix]
        public static void Postfix(Hediff_AddedPart __instance)
        {
            
        }
    }

    // 제거 메시지
    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostRemoved))]
    public static class Patch_HediffPostRemoved
    {
        [HarmonyPostfix]
        public static void Postfix(Hediff __instance)
        {
            if (__instance?.def == null) return;

            Pawn pawn = __instance.pawn;
            if (pawn == null) return;

            if (__instance.def.defName == "BaguetteArm")
            {
                Messages.Message(
                    pawn.LabelShort + "의 바게트 팔이 떨어졌다!",
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
            }
            else if (__instance.def.defName == "BaguetteLeg")
            {
                Messages.Message(
                    pawn.LabelShort + "의 바게트 발이 떨어졌다!",
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
            }
        }
    }

    // 근접 공격 적중 시 타겟에게 "바게트 팔로 때림" Hediff 부여
    [HarmonyPatch]
    public static class Patch_TakeDamage_Baguette
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage), new[] { typeof(DamageInfo) });
        }

        [HarmonyPrefix]
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null) return;

            if (dinfo.Def != null && dinfo.Def.isRanged) return;

            HediffDef baguetteArmDef = DefDatabase<HediffDef>.GetNamedSilentFail("BaguetteArm");
            if (baguetteArmDef == null) return;

            Hediff arm = attacker.health.hediffSet.GetFirstHediffOfDef(baguetteArmDef);
            if (arm == null) return;

            // 👉 품질 가져오기
            HediffComp_BaguetteQuality comp = arm.TryGetComp<HediffComp_BaguetteQuality>();
            float multiplier = 1.0f;

            if (comp != null)
            {
                multiplier = comp.DamageMultiplier;
            }

            // 👉 데미지 적용
            dinfo.SetAmount(dinfo.Amount * multiplier);

            // 👉 히트 Hediff 부여
            Pawn target = __instance as Pawn;
            if (target != null)
            {
                HediffDef hitDef = DefDatabase<HediffDef>.GetNamedSilentFail("BaguetteArmHit");
                if (hitDef != null)
                {
                    if (target.health.hediffSet.GetFirstHediffOfDef(hitDef) == null)
                    {
                        target.health.AddHediff(HediffMaker.MakeHediff(hitDef, target));
                    }
                }
            }

            Log.Message($"[Lapin Baguette] {attacker.LabelShort} → {__instance.LabelShort} (x{multiplier})");
        }
    }
}