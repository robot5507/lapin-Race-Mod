using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LapinRace
{
    public class CompProperties_CombatAndroidKeepWeapon : CompProperties
    {
        public string weaponDefName = "MusketWithBayonet";

        public CompProperties_CombatAndroidKeepWeapon()
        {
            compClass = typeof(CompCombatAndroidKeepWeapon);
        }
    }

    public class CompCombatAndroidKeepWeapon : ThingComp
    {
        private CompProperties_CombatAndroidKeepWeapon Props
        {
            get
            {
                return (CompProperties_CombatAndroidKeepWeapon)props;
            }
        }

        private Pawn Pawn
        {
            get
            {
                return parent as Pawn;
            }
        }

        public string WeaponDefName
        {
            get
            {
                return Props.weaponDefName;
            }
        }

        public ThingDef WeaponDef
        {
            get
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail(Props.weaponDefName);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureWeapon();
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(250))
            {
                return;
            }

            EnsureWeapon();
        }

        public void EnsureWeapon()
        {
            Pawn pawn = Pawn;

            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return;
            }

            if (!IsCombatAndroid(pawn))
            {
                return;
            }

            if (pawn.equipment == null)
            {
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
            }

            ThingWithComps primary = pawn.equipment.Primary;
            if (primary != null)
            {
                return;
            }

            ThingDef weaponDef = WeaponDef;
            if (weaponDef == null)
            {
                Log.Error("[LapinRace] 전투형 안드로이드 무기 ThingDef를 찾을 수 없습니다: " + Props.weaponDefName);
                return;
            }

            // 먼저 근처에 떨어진 자기 머스킷을 회수한다.
            ThingWithComps droppedWeapon = TryFindDroppedWeaponNearby(pawn, weaponDef);
            if (droppedWeapon != null)
            {
                if (droppedWeapon.Spawned)
                {
                    droppedWeapon.DeSpawn();
                }

                pawn.equipment.AddEquipment(droppedWeapon);
                return;
            }

            // 없을 때만 새로 생성한다.
            ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
            if (weapon == null)
            {
                Log.Error("[LapinRace] 전투형 안드로이드 무기 생성 실패. ThingWithComps가 아닙니다: " + Props.weaponDefName);
                return;
            }

            pawn.equipment.AddEquipment(weapon);
        }

        private static ThingWithComps TryFindDroppedWeaponNearby(Pawn pawn, ThingDef weaponDef)
        {
            if (pawn == null || pawn.Map == null || weaponDef == null)
            {
                return null;
            }

            // 다운/드롭 직후에는 보통 같은 칸 또는 주변 몇 칸에 떨어진다.
            List<Thing> things = pawn.Map.listerThings.ThingsOfDef(weaponDef);
            if (things == null || things.Count == 0)
            {
                return null;
            }

            ThingWithComps best = null;
            float bestDist = 999999f;

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];

                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }

                ThingWithComps weapon = thing as ThingWithComps;
                if (weapon == null)
                {
                    continue;
                }

                float dist = pawn.Position.DistanceToSquared(weapon.Position);

                // 너무 멀리 있는 플레이어 소유 머스킷까지 가져오지 않게 제한
                if (dist > 49f)
                {
                    continue;
                }

                if (best == null || dist < bestDist)
                {
                    best = weapon;
                    bestDist = dist;
                }
            }

            return best;
        }

        public static bool IsCombatAndroid(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.def != null && pawn.def.defName == "LP_CombatAndroidRace")
            {
                return true;
            }

            if (pawn.kindDef != null && pawn.kindDef.defName == "LP_CombatAndroid")
            {
                return true;
            }

            return false;
        }

        public static bool IsBoundWeaponOf(Pawn pawn, ThingWithComps weapon)
        {
            if (pawn == null || weapon == null)
            {
                return false;
            }

            if (!IsCombatAndroid(pawn))
            {
                return false;
            }

            CompCombatAndroidKeepWeapon comp = pawn.TryGetComp<CompCombatAndroidKeepWeapon>();
            if (comp == null)
            {
                return false;
            }

            ThingDef weaponDef = comp.WeaponDef;
            if (weaponDef == null)
            {
                return false;
            }

            return weapon.def == weaponDef;
        }
    }
}