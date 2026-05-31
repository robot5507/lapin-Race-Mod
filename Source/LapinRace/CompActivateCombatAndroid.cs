using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LapinRace
{
    public class CompProperties_ActivateCombatAndroid : CompProperties
    {
        public string raceDefName = "LP_CombatAndroidRace";
        public string pawnKindDefName = "LP_CombatAndroid";

        public string weaponDefName = "MusketWithBayonet";

        public string commandLabel = "전투형 안드로이드 활성화";
        public string commandDesc = "비활성 전투형 안드로이드를 가동하여 소집 가능한 전투 오토마톤으로 전환합니다.";

        public bool destroyParentOnActivate = true;

        public CompProperties_ActivateCombatAndroid()
        {
            compClass = typeof(CompActivateCombatAndroid);
        }
    }

    public class CompActivateCombatAndroid : ThingComp
    {
        private CompProperties_ActivateCombatAndroid Props
        {
            get
            {
                return (CompProperties_ActivateCombatAndroid)props;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (parent == null || parent.Destroyed || parent.Map == null)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = Props.commandLabel,
                defaultDesc = Props.commandDesc,
                icon = TexCommand.DesirePower,
                action = delegate
                {
                    Activate();
                }
            };
        }

        private void Activate()
        {
            if (parent == null || parent.Destroyed || parent.Map == null)
            {
                return;
            }

            Map map = parent.Map;
            IntVec3 spawnCell = parent.Position;

            ThingDef raceDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.raceDefName);
            if (raceDef == null)
            {
                Log.Error("[LapinRace] 전투형 안드로이드 Race ThingDef를 찾을 수 없습니다: " + Props.raceDefName);
                return;
            }

            PawnKindDef pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(Props.pawnKindDefName);
            if (pawnKindDef == null)
            {
                Log.Error("[LapinRace] 전투형 안드로이드 PawnKindDef를 찾을 수 없습니다: " + Props.pawnKindDefName);
                return;
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                Log.Error("[LapinRace] 플레이어 팩션을 찾을 수 없어 전투형 안드로이드를 활성화할 수 없습니다.");
                return;
            }

            Pawn pawn = ThingMaker.MakeThing(raceDef) as Pawn;
            if (pawn == null)
            {
                Log.Error("[LapinRace] 전투형 안드로이드 Pawn 생성에 실패했습니다: " + Props.raceDefName);
                return;
            }

            pawn.kindDef = pawnKindDef;

            PawnComponentsUtility.CreateInitialComponents(pawn);
            pawn.SetFactionDirect(playerFaction);

            TryInitializePawnBasics(pawn, pawnKindDef);

            GenSpawn.Spawn(pawn, spawnCell, map, parent.Rotation, WipeMode.Vanish, false, false);

            GiveForcedWeapon(pawn);

            Messages.Message(
                pawn.LabelShort + "가 활성화되었습니다.",
                pawn,
                MessageTypeDefOf.PositiveEvent,
                false
            );

            if (Props.destroyParentOnActivate && parent != null && !parent.Destroyed)
            {
                parent.Destroy(DestroyMode.Vanish);
            }
        }

        private void GiveForcedWeapon(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            if (pawn.equipment == null)
            {
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
            }

            if (pawn.equipment.Primary != null)
            {
                return;
            }

            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.weaponDefName);
            if (weaponDef == null)
            {
                Log.Error("[LapinRace] 전투형 안드로이드 머스킷 ThingDef를 찾을 수 없습니다: " + Props.weaponDefName);
                return;
            }

            ThingWithComps weapon = ThingMaker.MakeThing(weaponDef) as ThingWithComps;
            if (weapon == null)
            {
                Log.Error("[LapinRace] 전투형 안드로이드 무기 생성 실패. ThingWithComps가 아닙니다: " + Props.weaponDefName);
                return;
            }

            weapon.SetColor(pawn.DrawColor, false);

            pawn.equipment.AddEquipment(weapon);
        }

        private static void TryInitializePawnBasics(Pawn pawn, PawnKindDef pawnKindDef)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.Name == null)
            {
                if (pawnKindDef != null && !pawnKindDef.label.NullOrEmpty())
                {
                    pawn.Name = new NameSingle(pawnKindDef.label.CapitalizeFirst());
                }
                else if (pawn.def != null && !pawn.def.label.NullOrEmpty())
                {
                    pawn.Name = new NameSingle(pawn.def.label.CapitalizeFirst());
                }
                else
                {
                    pawn.Name = new NameSingle("전투형 안드로이드");
                }
            }

            if (pawn.ageTracker != null)
            {
                pawn.ageTracker.AgeBiologicalTicks = 0;
                pawn.ageTracker.AgeChronologicalTicks = 0;
            }

            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }

            if (pawn.drafter == null)
            {
                pawn.drafter = new Pawn_DraftController(pawn);
            }
        }
    }
}