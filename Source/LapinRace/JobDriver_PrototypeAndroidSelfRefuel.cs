using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobDriver_PrototypeAndroidSelfRefuel : JobDriver
    {
        private const TargetIndex FuelInd = TargetIndex.A;

        private Thing Fuel
        {
            get { return job.GetTarget(FuelInd).Thing; }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing fuel = Fuel;

            if (fuel == null)
            {
                return false;
            }

            return pawn.Reserve(fuel, job, 1, job.count, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(FuelInd);
            this.FailOnForbidden(FuelInd);

            this.FailOn(() =>
            {
                CompPrototypeAndroidFuel comp = pawn.TryGetComp<CompPrototypeAndroidFuel>();
                if (comp == null)
                {
                    return true;
                }

                return comp.IsFull;
            });

            yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch);

            Toil pickUpFuel = ToilMaker.MakeToil("PickUpPrototypeAndroidFuel");
            pickUpFuel.initAction = delegate
            {
                Thing fuel = Fuel;

                if (fuel == null || fuel.Destroyed || fuel.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                CompPrototypeAndroidFuel comp = pawn.TryGetComp<CompPrototypeAndroidFuel>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int countToTake = job.count > 0 ? job.count : 1;

                if (countToTake > fuel.stackCount)
                {
                    countToTake = fuel.stackCount;
                }

                if (countToTake > comp.NeededFuelCount)
                {
                    countToTake = comp.NeededFuelCount;
                }

                if (countToTake <= 0)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                Thing taken = fuel.SplitOff(countToTake);

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
            pickUpFuel.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickUpFuel;

            Toil refuelSelf = ToilMaker.MakeToil("PrototypeAndroidRefuelSelf");
            refuelSelf.initAction = delegate
            {
                CompPrototypeAndroidFuel comp = pawn.TryGetComp<CompPrototypeAndroidFuel>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def != comp.FuelDef || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };

            refuelSelf.defaultCompleteMode = ToilCompleteMode.Delay;
            refuelSelf.defaultDuration = 90;
            refuelSelf.WithProgressBarToilDelay(FuelInd);

            EffecterDef effect = DefDatabase<EffecterDef>.GetNamedSilentFail("Smith");
            if (effect != null)
            {
                refuelSelf.WithEffect(effect, TargetIndex.A);
            }

            SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Machining");
            if (sound != null)
            {
                refuelSelf.PlaySustainerOrSound(sound);
            }

            yield return refuelSelf;

            Toil finish = ToilMaker.MakeToil("FinishPrototypeAndroidSelfRefuel");
            finish.initAction = delegate
            {
                CompPrototypeAndroidFuel comp = pawn.TryGetComp<CompPrototypeAndroidFuel>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def != comp.FuelDef || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int needed = comp.NeededFuelCount;
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

                float added = comp.AddEnergy(comp.ChemfuelToEnergy * consumeCount);

                Messages.Message(
                    "시제형 안드로이드가 화학연료 " + consumeCount + "개를 소모해 전력 +" + added.ToString("0") + "을 보충했습니다.",
                    pawn,
                    MessageTypeDefOf.PositiveEvent,
                    false
                );
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}