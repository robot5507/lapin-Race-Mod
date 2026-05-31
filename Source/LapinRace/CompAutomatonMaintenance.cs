using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class CompProperties_AutomatonMaintenance : CompProperties
    {
        // 부품 1개당 수리량
        public float repairAmountPerComponent = 25f;

        public CompProperties_AutomatonMaintenance()
        {
            compClass = typeof(CompAutomatonMaintenance);
        }
    }

    public class CompAutomatonMaintenance : ThingComp
    {
        private Pawn Pawn
        {
            get
            {
                return parent as Pawn;
            }
        }

        private CompProperties_AutomatonMaintenance Props
        {
            get
            {
                return (CompProperties_AutomatonMaintenance)props;
            }
        }

        public float RepairAmountPerComponent
        {
            get
            {
                return Props.repairAmountPerComponent;
            }
        }

        public bool NeedsRepair
        {
            get
            {
                return TotalInjurySeverity > 0.01f;
            }
        }

        public float TotalInjurySeverity
        {
            get
            {
                Pawn pawn = Pawn;

                if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
                {
                    return 0f;
                }

                float total = 0f;
                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

                for (int i = 0; i < hediffs.Count; i++)
                {
                    Hediff_Injury injury = hediffs[i] as Hediff_Injury;

                    if (injury != null && injury.Severity > 0f)
                    {
                        total += injury.Severity;
                    }
                }

                return total;
            }
        }

        public int NeededComponentCount
        {
            get
            {
                if (Props.repairAmountPerComponent <= 0f)
                {
                    return 0;
                }

                float totalDamage = TotalInjurySeverity;

                if (totalDamage <= 0f)
                {
                    return 0;
                }

                return Mathf.CeilToInt(totalDamage / Props.repairAmountPerComponent);
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            Pawn automaton = Pawn;

            if (automaton == null || automaton.Dead || automaton.Map == null)
            {
                yield break;
            }

            if (selPawn == null || selPawn.Map != automaton.Map)
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

            if (!NeedsRepair)
            {
                yield return new FloatMenuOption("오토마톤 수리: 손상 없음", null);
                yield break;
            }

            if (!selPawn.CanReserveAndReach(automaton, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("오토마톤 수리: 전차에 접근할 수 없음", null);
                yield break;
            }

            Thing component = FindReachableComponentFor(selPawn);

            if (component == null)
            {
                yield return new FloatMenuOption("오토마톤 수리: 사용할 수 있는 부품 없음", null);
                yield break;
            }

            yield return new FloatMenuOption("오토마톤 수리", delegate
            {
                JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_RepairAutomaton");

                if (jobDef == null)
                {
                    Log.Error("[LapinRace] LP_RepairAutomaton JobDef를 찾을 수 없습니다.");
                    return;
                }

                Job job = JobMaker.MakeJob(jobDef, automaton, component);
                job.count = 1;

                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });

            int neededComponents = NeededComponentCount;

            if (neededComponents > 1)
            {
                int availableInStack = component.stackCount;
                int countToUse = neededComponents;

                if (countToUse > availableInStack)
                {
                    countToUse = availableInStack;
                }

                yield return new FloatMenuOption("오토마톤 완전 수리 (" + countToUse + "개)", delegate
                {
                    JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_RepairAutomaton");

                    if (jobDef == null)
                    {
                        Log.Error("[LapinRace] LP_RepairAutomaton JobDef를 찾을 수 없습니다.");
                        return;
                    }

                    Job job = JobMaker.MakeJob(jobDef, automaton, component);
                    job.count = countToUse;

                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        private Thing FindReachableComponentFor(Pawn worker)
        {
            if (worker == null || worker.Map == null)
            {
                return null;
            }

            ThingDef componentDef = DefDatabase<ThingDef>.GetNamedSilentFail("ComponentIndustrial");

            if (componentDef == null)
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(
                worker.Position,
                worker.Map,
                ThingRequest.ForDef(componentDef),
                PathEndMode.ClosestTouch,
                TraverseParms.For(worker, Danger.Deadly, TraverseMode.ByPawn),
                9999f,
                thing =>
                {
                    if (thing == null || thing.Destroyed)
                    {
                        return false;
                    }

                    if (thing.IsForbidden(worker))
                    {
                        return false;
                    }

                    if (thing.stackCount <= 0)
                    {
                        return false;
                    }

                    if (!worker.CanReserve(thing))
                    {
                        return false;
                    }

                    return true;
                }
            );
        }

        public float RepairDamage(float amount)
        {
            Pawn pawn = Pawn;

            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return 0f;
            }

            if (amount <= 0f)
            {
                return 0f;
            }

            float remaining = amount;
            float repaired = 0f;

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                Hediff_Injury injury = hediffs[i] as Hediff_Injury;

                if (injury == null || injury.Severity <= 0f)
                {
                    continue;
                }

                float repair = injury.Severity < remaining ? injury.Severity : remaining;

                injury.Severity -= repair;
                remaining -= repair;
                repaired += repair;

                if (injury.Severity <= 0.01f)
                {
                    pawn.health.RemoveHediff(injury);
                }

                if (remaining <= 0f)
                {
                    break;
                }
            }

            return repaired;
        }
    }
}