using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LapinRace
{
    public class CompProperties_ActivateAutomatonTank : CompProperties
    {
        public string raceDefName = "LP_AutomatonTankRace";
        public string pawnKindDefName = "LP_AutomatonTank_Player";
        public string cannonDefName = "LP_AutomatonCannonGun";

        public string commandLabel = "오토마톤 전차 활성화";
        public string commandDesc = "비활성 오토마톤 전차를 가동하여 플레이어가 운용 가능한 오토마톤 전차로 전환합니다.";

        public bool destroyParentOnActivate = true;

        public CompProperties_ActivateAutomatonTank()
        {
            compClass = typeof(CompActivateAutomatonTank);
        }
    }

    public class CompActivateAutomatonTank : ThingComp
    {
        private CompProperties_ActivateAutomatonTank Props
        {
            get
            {
                return (CompProperties_ActivateAutomatonTank)props;
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

            Command_Action command = new Command_Action
            {
                defaultLabel = Props.commandLabel,
                defaultDesc = Props.commandDesc,
                icon = TexCommand.DesirePower,
                action = delegate
                {
                    Activate();
                }
            };

            yield return command;
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
                Log.Error("[LapinRace] 오토마톤 전차 Race ThingDef를 찾을 수 없습니다: " + Props.raceDefName);
                return;
            }

            PawnKindDef pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(Props.pawnKindDefName);
            if (pawnKindDef == null)
            {
                Log.Error("[LapinRace] 오토마톤 전차 PawnKindDef를 찾을 수 없습니다: " + Props.pawnKindDefName);
                return;
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                Log.Error("[LapinRace] 플레이어 팩션을 찾을 수 없어 오토마톤 전차를 활성화할 수 없습니다.");
                return;
            }

            Pawn pawn = ThingMaker.MakeThing(raceDef) as Pawn;
            if (pawn == null)
            {
                Log.Error("[LapinRace] 오토마톤 전차 Pawn 생성에 실패했습니다: " + Props.raceDefName);
                return;
            }

            pawn.kindDef = pawnKindDef;

            PawnComponentsUtility.CreateInitialComponents(pawn);

            pawn.SetFactionDirect(playerFaction);

            TryInitializePawnBasics(pawn);
            GiveAutomatonCannon(pawn);

            GenSpawn.Spawn(pawn, spawnCell, map, parent.Rotation, WipeMode.Vanish, false, false);

            Messages.Message(
                "오토마톤 전차가 활성화되었습니다.",
                pawn,
                MessageTypeDefOf.PositiveEvent,
                false
            );

            if (Props.destroyParentOnActivate && parent != null && !parent.Destroyed)
            {
                parent.Destroy(DestroyMode.Vanish);
            }
        }

        private static void TryInitializePawnBasics(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.Name == null)
            {
                pawn.Name = new NameSingle("오토마톤 전차");
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

        private void GiveAutomatonCannon(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.equipment == null)
            {
                PawnComponentsUtility.CreateInitialComponents(pawn);
            }

            if (pawn.equipment == null)
            {
                Log.Warning("[LapinRace] 오토마톤 전차에 equipment tracker가 없어 전차포를 장비시킬 수 없습니다.");
                return;
            }

            if (pawn.equipment.Primary != null)
            {
                return;
            }

            ThingDef cannonDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.cannonDefName);
            if (cannonDef == null)
            {
                Log.Error("[LapinRace] 오토마톤 전차포 ThingDef를 찾을 수 없습니다: " + Props.cannonDefName);
                return;
            }

            ThingWithComps cannon = ThingMaker.MakeThing(cannonDef) as ThingWithComps;
            if (cannon == null)
            {
                Log.Error("[LapinRace] 오토마톤 전차포 생성에 실패했습니다: " + Props.cannonDefName);
                return;
            }

            pawn.equipment.AddEquipment(cannon);
        }
    }
}
