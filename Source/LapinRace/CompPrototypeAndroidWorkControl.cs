using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class CompProperties_PrototypeAndroidWorkControl : CompProperties
    {
        public CompProperties_PrototypeAndroidWorkControl()
        {
            compClass = typeof(CompPrototypeAndroidWorkControl);
        }
    }

    public class CompPrototypeAndroidWorkControl : ThingComp
    {
        private bool standbyMode = false;

        private Pawn Pawn
        {
            get
            {
                return parent as Pawn;
            }
        }

        public bool StandbyMode
        {
            get
            {
                return standbyMode;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref standbyMode, "standbyMode", false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            Pawn pawn = Pawn;
            if (pawn != null)
            {
                EnsurePrototypeAndroidSetup(pawn);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(120))
            {
                return;
            }

            EnsurePrototypeAndroidSetup(pawn);

            if (standbyMode)
            {
                ForceStandby(pawn);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Map == null)
            {
                yield break;
            }

            EnsurePrototypeAndroidSetup(pawn);

            yield return new Command_Action
            {
                defaultLabel = GetAreaLabel(pawn),
                defaultDesc = "시제형 안드로이드가 활동할 작업구역을 지정합니다. 운반, 배회, 자동 연료 보충은 이 구역 안에서만 수행됩니다.",
                icon = TexCommand.ForbidOff,
                action = delegate
                {
                    OpenAreaFloatMenu(pawn);
                }
            };
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            Pawn android = Pawn;

            if (android == null || android.Dead || android.Map == null)
            {
                yield break;
            }

            if (selPawn == null || selPawn.Map != android.Map)
            {
                yield break;
            }

            if (selPawn.Faction == null || !selPawn.Faction.IsPlayer)
            {
                yield break;
            }

            if (selPawn.Dead || selPawn.Downed)
            {
                yield break;
            }

            if (!selPawn.CanReserveAndReach(android, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("전원 조작: 안드로이드에 접근할 수 없음", null);
                yield break;
            }

            if (!standbyMode)
            {
                yield return new FloatMenuOption("시제형 안드로이드 전원 끄기", delegate
                {
                    StartTogglePowerJob(selPawn, android, true);
                });
            }
            else
            {
                yield return new FloatMenuOption("시제형 안드로이드 전원 재가동", delegate
                {
                    StartTogglePowerJob(selPawn, android, false);
                });
            }
        }

        private static void StartTogglePowerJob(Pawn worker, Pawn android, bool turnStandbyOn)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_TogglePrototypeAndroidPower");
            if (jobDef == null)
            {
                Log.Error("[LapinRace] LP_TogglePrototypeAndroidPower JobDef를 찾을 수 없습니다.");
                return;
            }

            Job job = JobMaker.MakeJob(jobDef, android);

            // 1 = 전원 대기 ON, 0 = 전원 재가동
            job.count = turnStandbyOn ? 1 : 0;

            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static void EnsurePrototypeAndroidSetup(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
        }

        private static void ForceStandby(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed)
            {
                return;
            }

            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }

            if (pawn.jobs == null)
            {
                return;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Wait)
            {
                return;
            }

            Job waitJob = JobMaker.MakeJob(JobDefOf.Wait);
            waitJob.expiryInterval = 600;
            waitJob.locomotionUrgency = LocomotionUrgency.None;

            pawn.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);
        }

        public void SetStandbyMode(bool value)
        {
            Pawn pawn = Pawn;

            standbyMode = value;

            if (pawn == null)
            {
                return;
            }

            if (standbyMode)
            {
                ForceStandby(pawn);
            }
            else
            {
                if (pawn.jobs != null && pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Wait)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }

        public static bool IsInStandby(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            CompPrototypeAndroidWorkControl comp = pawn.TryGetComp<CompPrototypeAndroidWorkControl>();
            if (comp == null)
            {
                return false;
            }

            return comp.StandbyMode;
        }

        private static string GetAreaLabel(Pawn pawn)
        {
            if (pawn == null || pawn.playerSettings == null)
            {
                return "작업구역: 제한 없음";
            }

            Area area = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;

            if (area == null)
            {
                return "작업구역: 제한 없음";
            }

            return "작업구역: " + area.Label;
        }

        private static void OpenAreaFloatMenu(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return;
            }

            EnsurePrototypeAndroidSetup(pawn);

            List<FloatMenuOption> options = new List<FloatMenuOption>();

            options.Add(new FloatMenuOption("제한 없음", delegate
            {
                SetAreaRestriction(pawn, null);
            }));

            AreaManager areaManager = pawn.Map.areaManager;
            if (areaManager != null)
            {
                List<Area> allAreas = areaManager.AllAreas;

                for (int i = 0; i < allAreas.Count; i++)
                {
                    Area area = allAreas[i];

                    if (area == null)
                    {
                        continue;
                    }

                    Area localArea = area;

                    options.Add(new FloatMenuOption(localArea.Label, delegate
                    {
                        SetAreaRestriction(pawn, localArea);
                    }));
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void SetAreaRestriction(Pawn pawn, Area area)
        {
            if (pawn == null)
            {
                return;
            }

            EnsurePrototypeAndroidSetup(pawn);

            if (pawn.playerSettings == null)
            {
                return;
            }

            pawn.playerSettings.AreaRestrictionInPawnCurrentMap = area;

            if (area == null)
            {
                Messages.Message(
                    pawn.LabelShort + "의 작업구역을 제한 없음으로 설정했습니다.",
                    pawn,
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
            }
            else
            {
                Messages.Message(
                    pawn.LabelShort + "의 작업구역을 " + area.Label + "로 설정했습니다.",
                    pawn,
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
            }
        }
    }
}