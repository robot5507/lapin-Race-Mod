using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_AutomatonTankDeathSound
    {
        private struct DeathSoundData
        {
            public Map map;
            public IntVec3 cell;
            public Vector3 drawPos;
        }

        private static readonly Dictionary<int, DeathSoundData> pendingDeathSounds =
            new Dictionary<int, DeathSoundData>();

        static Patch_AutomatonTankDeathSound()
        {
            Harmony harmony = new Harmony("LapinRace.AutomatonTankDeathSound");

            harmony.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
                prefix: new HarmonyMethod(typeof(Patch_AutomatonTankDeathSound), nameof(KillPrefix)),
                postfix: new HarmonyMethod(typeof(Patch_AutomatonTankDeathSound), nameof(KillPostfix))
            );
        }

        private static void KillPrefix(Pawn __instance)
        {
            if (!IsAutomatonTank(__instance))
            {
                return;
            }

            if (__instance == null || __instance.Map == null)
            {
                return;
            }

            int id = __instance.thingIDNumber;

            pendingDeathSounds[id] = new DeathSoundData
            {
                map = __instance.Map,
                cell = __instance.Position,
                drawPos = __instance.DrawPos
            };
        }

        private static void KillPostfix(Pawn __instance)
        {
            if (!IsAutomatonTank(__instance))
            {
                return;
            }

            if (__instance == null)
            {
                return;
            }

            int id = __instance.thingIDNumber;

            if (!pendingDeathSounds.TryGetValue(id, out DeathSoundData data))
            {
                return;
            }

            pendingDeathSounds.Remove(id);

            if (data.map == null || !data.cell.InBounds(data.map))
            {
                return;
            }

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail("LP_AutomatonTankDestroyed");

            // 커스텀 사운드가 안 잡혔을 때 테스트용 바닐라 대체음
            if (soundDef == null)
            {
                soundDef = DefDatabase<SoundDef>.GetNamedSilentFail("Explosion_Bomb");
            }

            if (soundDef != null)
            {
                soundDef.PlayOneShot(new TargetInfo(data.cell, data.map));
            }
            else
            {
                Log.Warning("[LapinRace] LP_AutomatonTankDestroyed 또는 Explosion_Bomb SoundDef를 찾지 못했습니다.");
            }

            Vector3 pos = data.drawPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            FleckMaker.Static(pos, data.map, FleckDefOf.ExplosionFlash, 1.6f);
            FleckMaker.ThrowDustPuff(pos, data.map, 2.4f);
            FleckMaker.ThrowSmoke(pos, data.map, 2.8f);
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
                    kindName == "LP_AutomatonTank" ||
                    kindName == "LP_AutomatonTank_Enemy")
                {
                    return true;
                }
            }

            return false;
        }
    }
}