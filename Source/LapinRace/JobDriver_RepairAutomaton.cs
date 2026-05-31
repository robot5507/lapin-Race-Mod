using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobDriver_RepairAutomaton : JobDriver
    {
        private const TargetIndex AutomatonInd = TargetIndex.A;
        private const TargetIndex ComponentInd = TargetIndex.B;

        private Pawn Automaton
        {
            get
            {
                return job.GetTarget(AutomatonInd).Thing as Pawn;
            }
        }

        private Thing Component
        {
            get
            {
                return job.GetTarget(ComponentInd).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn automaton = Automaton;
            Thing component = Component;

            if (automaton == null || component == null)
            {
                return false;
            }

            if (!pawn.Reserve(automaton, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (!pawn.Reserve(component, job, 1, job.count, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(AutomatonInd);
            this.FailOnDestroyedOrNull(ComponentInd);
            this.FailOnForbidden(ComponentInd);

            this.FailOn(() =>
            {
                Pawn automaton = Automaton;
                if (automaton == null || automaton.Dead)
                {
                    return true;
                }

                CompAutomatonMaintenance comp = automaton.TryGetComp<CompAutomatonMaintenance>();
                if (comp == null)
                {
                    return true;
                }

                return !comp.NeedsRepair;
            });

            // 1. 부품 위치로 이동
            yield return Toils_Goto.GotoThing(ComponentInd, PathEndMode.ClosestTouch);

            // 2. 부품 집기
            Toil pickUpComponent = ToilMaker.MakeToil("PickUpAutomatonRepairComponent");
            pickUpComponent.initAction = delegate
            {
                Thing component = Component;

                if (component == null || component.Destroyed || component.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int countToTake = job.count > 0 ? job.count : 1;

                if (countToTake > component.stackCount)
                {
                    countToTake = component.stackCount;
                }

                if (countToTake <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing taken = component.SplitOff(countToTake);

                if (taken == null || taken.Destroyed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!pawn.carryTracker.TryStartCarry(taken))
                {
                    if (!taken.Destroyed)
                    {
                        taken.Destroy(DestroyMode.Vanish);
                    }

                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };
            pickUpComponent.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickUpComponent;

            // 3. 대상이 시제형 안드로이드면, 정착민이 다가가는 동안 잠시 멈추게 함
            Toil makeTargetWaitBeforeRepair = ToilMaker.MakeToil("MakeRepairTargetWaitBeforeApproach");
            makeTargetWaitBeforeRepair.initAction = delegate
            {
                MakeTargetWaitDuringInteraction(Automaton, 600);
            };
            makeTargetWaitBeforeRepair.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return makeTargetWaitBeforeRepair;

            // 4. 수리 대상에게 이동
            yield return Toils_Goto.GotoThing(AutomatonInd, PathEndMode.Touch);

            // 5. 수리 작업
            Toil repair = ToilMaker.MakeToil("RepairAutomaton");
            repair.initAction = delegate
            {
                Pawn automaton = Automaton;

                if (automaton == null || automaton.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                CompAutomatonMaintenance comp = automaton.TryGetComp<CompAutomatonMaintenance>();
                if (comp == null || !comp.NeedsRepair)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def == null || carried.def.defName != "ComponentIndustrial" || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };

            repair.defaultCompleteMode = ToilCompleteMode.Delay;

            int repairDuration = 240;
            if (job.count > 1)
            {
                repairDuration += 60 * (job.count - 1);
            }

            repair.defaultDuration = repairDuration;
            repair.WithProgressBarToilDelay(AutomatonInd);

            repair.tickAction = delegate
            {
                MakeTargetWaitDuringInteraction(Automaton, 120);
            };

            EffecterDef repairEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("Repair");
            if (repairEffect == null)
            {
                repairEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("ConstructMetal");
            }
            if (repairEffect == null)
            {
                repairEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("Smith");
            }

            if (repairEffect != null)
            {
                repair.WithEffect(repairEffect, AutomatonInd);
            }

            SoundDef repairSound = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Machining");
            if (repairSound == null)
            {
                repairSound = DefDatabase<SoundDef>.GetNamedSilentFail("Interact_ConstructMetal");
            }

            if (repairSound != null)
            {
                repair.PlaySustainerOrSound(repairSound);
            }

            yield return repair;

            // 6. 부품 소비 + 손상 회복
            Toil finish = ToilMaker.MakeToil("FinishRepairAutomaton");
            finish.initAction = delegate
            {
                Pawn automaton = Automaton;

                if (automaton == null || automaton.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                CompAutomatonMaintenance comp = automaton.TryGetComp<CompAutomatonMaintenance>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def == null || carried.def.defName != "ComponentIndustrial" || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int needed = comp.NeededComponentCount;
                int available = carried.stackCount;
                int consumeCount = needed < available ? needed : available;

                if (consumeCount <= 0)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                Thing consumed = carried.SplitOff(consumeCount);
                consumed.Destroy(DestroyMode.Vanish);

                if (carried.Destroyed || carried.stackCount <= 0)
                {
                    pawn.carryTracker.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
                }

                float repaired = comp.RepairDamage(comp.RepairAmountPerComponent * consumeCount);

                Messages.Message(
                    automaton.LabelShort + "를 수리했습니다. 부품 " + consumeCount + "개 사용, 손상 " + repaired.ToString("0") + " 회복",
                    automaton,
                    MessageTypeDefOf.PositiveEvent,
                    false
                );
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }

        private static void MakeTargetWaitDuringInteraction(Pawn target, int ticks)
        {
            if (target == null || target.Dead || target.Downed)
            {
                return;
            }

            // 시제형 안드로이드만 멈춘다.
            CompPrototypeAndroidFuel androidFuel = target.TryGetComp<CompPrototypeAndroidFuel>();
            if (androidFuel == null)
            {
                return;
            }

            if (target.pather != null)
            {
                target.pather.StopDead();
            }

            if (target.jobs == null)
            {
                return;
            }

            if (target.CurJob != null && target.CurJob.def == JobDefOf.Wait)
            {
                target.CurJob.expiryInterval = ticks;
                return;
            }

            Job waitJob = JobMaker.MakeJob(JobDefOf.Wait);
            waitJob.expiryInterval = ticks;
            waitJob.locomotionUrgency = LocomotionUrgency.None;

            target.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);
        }
    }
}