using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LapinRace
{
    public class CompProperties_ActivatePrototypeAndroid : CompProperties
    {
        public string raceDefName = "LP_PrototypeAndroidRace";
        public string pawnKindDefName = "LP_PrototypeAndroid";

        public string commandLabel = "시제형 안드로이드 활성화";
        public string commandDesc = "비활성 시제형 안드로이드를 가동하여 플레이어가 운용 가능한 작업 오토마톤으로 전환합니다.";

        public bool destroyParentOnActivate = true;

        public CompProperties_ActivatePrototypeAndroid()
        {
            compClass = typeof(CompActivatePrototypeAndroid);
        }
    }

    public class CompActivatePrototypeAndroid : ThingComp
    {
        private CompProperties_ActivatePrototypeAndroid Props
        {
            get
            {
                return (CompProperties_ActivatePrototypeAndroid)props;
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
                Log.Error("[LapinRace] 시제형 안드로이드 Race ThingDef를 찾을 수 없습니다: " + Props.raceDefName);
                return;
            }

            PawnKindDef pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(Props.pawnKindDefName);
            if (pawnKindDef == null)
            {
                Log.Error("[LapinRace] 시제형 안드로이드 PawnKindDef를 찾을 수 없습니다: " + Props.pawnKindDefName);
                return;
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                Log.Error("[LapinRace] 플레이어 팩션을 찾을 수 없어 시제형 안드로이드를 활성화할 수 없습니다.");
                return;
            }

            Pawn pawn = ThingMaker.MakeThing(raceDef) as Pawn;
            if (pawn == null)
            {
                Log.Error("[LapinRace] 시제형 안드로이드 Pawn 생성에 실패했습니다: " + Props.raceDefName);
                return;
            }

            pawn.kindDef = pawnKindDef;

            PawnComponentsUtility.CreateInitialComponents(pawn);
            pawn.SetFactionDirect(playerFaction);

            TryInitializePawnBasics(pawn, pawnKindDef);

            GenSpawn.Spawn(pawn, spawnCell, map, parent.Rotation, WipeMode.Vanish, false, false);

            Messages.Message(
                "시제형 안드로이드가 활성화되었습니다.",
                pawn,
                MessageTypeDefOf.PositiveEvent,
                false
            );

            if (Props.destroyParentOnActivate && parent != null && !parent.Destroyed)
            {
                parent.Destroy(DestroyMode.Vanish);
            }
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
                    pawn.Name = new NameSingle("오토마톤");
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

            // 시제형/메이드형은 전투 소집용이 아니므로 drafter는 만들지 않는다.
        }
    }
}
