using RimWorld;
using UnityEngine;
using Verse;

namespace LapinRace
{
    public class Verb_ShootCannonWithSmoke : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool result = base.TryCastShot();

            if (result && Caster != null && Caster.Map != null)
            {
                Map map = Caster.Map;
                Vector3 drawPos = Caster.DrawPos;

                Vector3 dir = currentTarget.IsValid
                    ? (currentTarget.CenterVector3 - drawPos).normalized
                    : Caster.Rotation.FacingCell.ToVector3().normalized;

                // 총구 위치
                Vector3 muzzlePos = drawPos + dir * 1.2f;
                muzzlePos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                // 발사 순간 총구 연기/먼지/섬광
                FleckMaker.ThrowSmoke(muzzlePos, map, 2.5f);
                FleckMaker.ThrowDustPuff(muzzlePos, map, 1.8f);
                FleckMaker.Static(muzzlePos, map, FleckDefOf.ExplosionFlash, 1.2f);

                // 2x2 정도 자욱한 연기
                SpawnThickSmoke2x2(muzzlePos, map);
            }

            return result;
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
                    continue;

                Vector3 pos = cell.ToVector3Shifted();
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                // 순간 연기 이펙트
                FleckMaker.ThrowSmoke(pos, map, 1.4f);
                FleckMaker.ThrowSmoke(pos + new Vector3(0.2f, 0f, 0.1f), map, 1.8f);
                FleckMaker.ThrowSmoke(pos + new Vector3(-0.2f, 0f, -0.1f), map, 1.8f);
                FleckMaker.ThrowDustPuff(pos, map, 1.2f);

                // 실제 잠깐 남는 연막
                GasUtility.AddGas(cell, map, GasType.BlindSmoke, 0.08f);
            }
        }
    }
}