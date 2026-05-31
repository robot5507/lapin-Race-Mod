using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobDriver_RefuelAutomaton : JobDriver
    {
        private const TargetIndex TargetInd = TargetIndex.A;
        private const TargetIndex FuelInd = TargetIndex.B;

        private Pawn TargetPawn
        {
            get
            {
                return job.GetTarget(TargetInd).Thing as Pawn;
            }
        }

        private Thing Fuel
        {
            get
            {
                return job.GetTarget(FuelInd).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn target = TargetPawn;
            Thing fuel = Fuel;

            if (target == null || fuel == null)
            {
                return false;
            }

            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (!pawn.Reserve(fuel, job, 1, job.count, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetInd);
            this.FailOnDestroyedOrNull(FuelInd);
            this.FailOnForbidden(FuelInd);

            this.FailOn(() =>
            {
                Pawn target = TargetPawn;
                if (target == null || target.Dead)
                {
                    return true;
                }

                return IsTargetFull(target);
            });

            // 1. 연료 위치로 이동
            yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch);

            // 2. 연료 집기
            Toil pickUpFuel = ToilMaker.MakeToil("PickUpAutomatonFuel");
            pickUpFuel.initAction = delegate
            {
                Thing fuel = Fuel;

                if (fuel == null || fuel.Destroyed || fuel.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Pawn target = TargetPawn;
                if (target == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int countToTake = job.count > 0 ? job.count : 1;

                if (countToTake > fuel.stackCount)
                {
                    countToTake = fuel.stackCount;
                }

                int needed = GetNeededFuelCount(target);
                if (needed > 0 && countToTake > needed)
                {
                    countToTake = needed;
                }

                if (countToTake <= 0)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                Thing takenFuel = fuel.SplitOff(countToTake);

                if (takenFuel == null || takenFuel.Destroyed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!pawn.carryTracker.TryStartCarry(takenFuel))
                {
                    if (!takenFuel.Destroyed)
                    {
                        takenFuel.Destroy(DestroyMode.Vanish);
                    }

                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };
            pickUpFuel.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickUpFuel;

            // 3. 대상이 시제형 안드로이드면, 정착민이 다가가는 동안 잠시 멈추게 함
            Toil makeTargetWaitBeforeRefuel = ToilMaker.MakeToil("MakeRefuelTargetWaitBeforeApproach");
            makeTargetWaitBeforeRefuel.initAction = delegate
            {
                MakeTargetWaitDuringInteraction(TargetPawn, 600);
            };
            makeTargetWaitBeforeRefuel.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return makeTargetWaitBeforeRefuel;

            // 4. 대상에게 이동
            yield return Toils_Goto.GotoThing(TargetInd, PathEndMode.Touch);

            // 5. 주입 작업
            Toil refuel = ToilMaker.MakeToil("RefuelAutomaton");
            refuel.initAction = delegate
            {
                Pawn target = TargetPawn;

                if (target == null || target.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (IsTargetFull(target))
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def != ThingDefOf.Chemfuel || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };

            refuel.defaultCompleteMode = ToilCompleteMode.Delay;

            int refuelDuration = 120;
            if (job.count > 1)
            {
                refuelDuration += 30 * (job.count - 1);
            }

            refuel.defaultDuration = refuelDuration;
            refuel.WithProgressBarToilDelay(TargetInd);

            refuel.tickAction = delegate
            {
                MakeTargetWaitDuringInteraction(TargetPawn, 120);
            };

            EffecterDef refuelEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("Smith");
            if (refuelEffect != null)
            {
                refuel.WithEffect(refuelEffect, TargetInd);
            }

            SoundDef refuelSound = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Machining");
            if (refuelSound != null)
            {
                refuel.PlaySustainerOrSound(refuelSound);
            }

            yield return refuel;

            // 6. 연료 소비 + 전력 충전
            Toil finish = ToilMaker.MakeToil("FinishRefuelAutomaton");
            finish.initAction = delegate
            {
                Pawn target = TargetPawn;

                if (target == null || target.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def != ThingDefOf.Chemfuel || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int needed = GetNeededFuelCount(target);
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

                float added = AddFuelEnergy(target, consumeCount);

                Messages.Message(
                    target.LabelShort + "에 화학연료 " + consumeCount + "개를 주입했습니다. +" + added.ToString("0") + " 전력",
                    target,
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

        private static bool IsTargetFull(Pawn target)
        {
            if (target == null)
            {
                return true;
            }

            CompAutomatonEnergy tankEnergy = target.TryGetComp<CompAutomatonEnergy>();
            if (tankEnergy != null)
            {
                return tankEnergy.IsFull;
            }

            CompPrototypeAndroidFuel androidFuel = target.TryGetComp<CompPrototypeAndroidFuel>();
            if (androidFuel != null)
            {
                return androidFuel.IsFull;
            }

            return true;
        }

        private static int GetNeededFuelCount(Pawn target)
        {
            if (target == null)
            {
                return 0;
            }

            CompAutomatonEnergy tankEnergy = target.TryGetComp<CompAutomatonEnergy>();
            if (tankEnergy != null)
            {
                return tankEnergy.NeededChemfuelCount;
            }

            CompPrototypeAndroidFuel androidFuel = target.TryGetComp<CompPrototypeAndroidFuel>();
            if (androidFuel != null)
            {
                return androidFuel.NeededFuelCount;
            }

            return 0;
        }

        private static float AddFuelEnergy(Pawn target, int consumeCount)
        {
            if (target == null || consumeCount <= 0)
            {
                return 0f;
            }

            CompAutomatonEnergy tankEnergy = target.TryGetComp<CompAutomatonEnergy>();
            if (tankEnergy != null)
            {
                return tankEnergy.AddEnergy(tankEnergy.ChemfuelToEnergy * consumeCount);
            }

            CompPrototypeAndroidFuel androidFuel = target.TryGetComp<CompPrototypeAndroidFuel>();
            if (androidFuel != null)
            {
                return androidFuel.AddEnergy(androidFuel.ChemfuelToEnergy * consumeCount);
            }

            return 0f;
        }
    }
}