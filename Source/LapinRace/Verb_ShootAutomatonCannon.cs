using RimWorld;
using UnityEngine;
using Verse;

namespace LapinRace
{
    public class Verb_ShootAutomatonCannon : Verb_Shoot
    {
        public override bool Available()
        {
            if (!base.Available())
            {
                return false;
            }

            Pawn pawn = CasterPawn;
            if (pawn == null)
            {
                return true;
            }

            if (!IsAutomatonTank(pawn))
            {
                return true;
            }

            CompAutomatonAmmo ammoComp = pawn.TryGetComp<CompAutomatonAmmo>();
            if (ammoComp == null)
            {
                return true;
            }

            return ammoComp.HasAmmo;
        }

        protected override bool TryCastShot()
        {
            Pawn pawn = CasterPawn;

            if (IsAutomatonTank(pawn))
            {
                CompAutomatonAmmo ammoComp = pawn.TryGetComp<CompAutomatonAmmo>();

                if (ammoComp != null)
                {
                    if (!ammoComp.TryConsumeAmmo(1))
                    {
                        Messages.Message(
                            "오토마톤 전차포의 포탄이 부족합니다.",
                            pawn,
                            MessageTypeDefOf.RejectInput,
                            false
                        );

                        return false;
                    }
                }
            }

            bool result = base.TryCastShot();

            if (result && Caster != null && Caster.Map != null)
            {
                ThrowAutomatonCannonSmoke();
            }

            return result;
        }

        private void ThrowAutomatonCannonSmoke()
        {
            Map map = Caster.Map;
            Vector3 drawPos = Caster.DrawPos;

            Vector3 dir;

            if (currentTarget.IsValid)
            {
                dir = currentTarget.CenterVector3 - drawPos;
                dir.y = 0f;

                if (dir.sqrMagnitude < 0.001f)
                {
                    dir = Caster.Rotation.FacingCell.ToVector3();
                }

                dir = dir.normalized;
            }
            else
            {
                dir = Caster.Rotation.FacingCell.ToVector3().normalized;
            }

            // 전차는 크니까 일반 대포보다 포구 위치를 조금 더 앞쪽으로 잡는다.
            Vector3 muzzlePos = drawPos + dir * 1.6f;
            muzzlePos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // 발사 순간 총구 연기/먼지/섬광
            FleckMaker.ThrowSmoke(muzzlePos, map, 2.8f);
            FleckMaker.ThrowDustPuff(muzzlePos, map, 1.8f);
            FleckMaker.Static(muzzlePos, map, FleckDefOf.ExplosionFlash, 1.2f);

            // 기존 대포와 비슷한 2x2 연기
            SpawnThickSmoke2x2(muzzlePos, map);
        }

        private void SpawnThickSmoke2x2(Vector3 center, Map map)
        {
            IntVec3 centerCell = center.ToIntVec3();

            IntVec3[] cells =
            {
                centerCell,
                centerCell + IntVec3.East,
                centerCell + IntVec3.North,
                centerCell + IntVec3.East + IntVec3.North
            };

            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Vector3 pos = cell.ToVector3Shifted();
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                // 순간 연기 이펙트
                FleckMaker.ThrowSmoke(pos, map, 1.4f);
                FleckMaker.ThrowSmoke(pos + new Vector3(0.2f, 0f, 0.1f), map, 1.8f);
                FleckMaker.ThrowSmoke(pos + new Vector3(-0.2f, 0f, -0.1f), map, 1.8f);
                FleckMaker.ThrowDustPuff(pos, map, 1.2f);

                // 실제 잠깐 남는 연막.
                // 너무 강하면 0.04f, 더 진하게 하고 싶으면 0.08f 정도로 조절.
                GasUtility.AddGas(cell, map, GasType.BlindSmoke, 0.06f);
            }
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