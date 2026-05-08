using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace LapinRace
{
    public static class SabreSoundUtility
    {
        public static bool IsSabreAttack(Verb_MeleeAttack verb)
        {
            if (verb == null) return false;

            Pawn pawn = verb.CasterPawn;
            if (pawn == null || pawn.Map == null) return false;

            var weapon = pawn.equipment?.Primary;
            if (weapon == null) return false;

            if (weapon.def.defName != "LP_Weapon_MilitarySabre") return false;
            if (weapon.def.defName != "LP_Weapon_Knife") return false;
            if (weapon.def.defName != "LP_Weapon_Rapier") return false;
            if (verb.EquipmentSource == null) return false;
            if (verb.EquipmentSource != weapon) return false;

            return true;
        }
    }

    // 휘두르는 소리
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_SabreSwingSound
    {
        public static void Prefix(Verb_MeleeAttack __instance)
        {
            if (!SabreSoundUtility.IsSabreAttack(__instance)) return;

            Pawn pawn = __instance.CasterPawn;
            SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("LP_SwordSwing");
            if (sound == null)
            {
                Log.Warning("[LapinRace] LP_SwordSwing not found");
                return;
            }

            sound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        }

        // 사람 맞았을 때 커스텀 타격음
        public static void Postfix(Verb_MeleeAttack __instance)
        {
            if (!SabreSoundUtility.IsSabreAttack(__instance)) return;

            Pawn pawn = __instance.CasterPawn;
            if (pawn == null || pawn.Map == null) return;

            LocalTargetInfo target = __instance.CurrentTarget;
            if (!target.IsValid || !target.HasThing) return;

            if (target.Thing is Pawn)
            {
                SoundDef hitSound = DefDatabase<SoundDef>.GetNamedSilentFail("LP_SwordHit");
                if (hitSound == null)
                {
                    Log.Warning("[LapinRace] LP_SwordHit not found");
                    return;
                }

                hitSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }
        }
    }

    // 빗나감 기본음 차단
    [HarmonyPatch(typeof(Verb_MeleeAttack), "SoundMiss")]
    public static class Patch_Sabre_BlockMissSound
    {
        public static bool Prefix(Verb_MeleeAttack __instance)
        {
            return !SabreSoundUtility.IsSabreAttack(__instance);
        }
    }

    // 사람 명중 기본음 차단
    [HarmonyPatch(typeof(Verb_MeleeAttack), "SoundHitPawn")]
    public static class Patch_Sabre_BlockHitPawnSound
    {
        public static bool Prefix(Verb_MeleeAttack __instance)
        {
            return !SabreSoundUtility.IsSabreAttack(__instance);
        }
    }

    // 건물 명중 기본음 차단
    [HarmonyPatch(typeof(Verb_MeleeAttack), "SoundHitBuilding")]
    public static class Patch_Sabre_BlockHitBuildingSound
    {
        public static bool Prefix(Verb_MeleeAttack __instance)
        {
            return !SabreSoundUtility.IsSabreAttack(__instance);
        }
    }
}