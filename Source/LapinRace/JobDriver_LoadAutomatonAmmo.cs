using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobDriver_LoadAutomatonAmmo : JobDriver
    {
        private const TargetIndex AutomatonInd = TargetIndex.A;
        private const TargetIndex AmmoInd = TargetIndex.B;

        private Pawn Automaton
        {
            get
            {
                return job.GetTarget(AutomatonInd).Thing as Pawn;
            }
        }

        private Thing Ammo
        {
            get
            {
                return job.GetTarget(AmmoInd).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn automaton = Automaton;
            Thing ammo = Ammo;

            if (automaton == null || ammo == null)
            {
                return false;
            }

            if (!pawn.Reserve(automaton, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (!pawn.Reserve(ammo, job, 1, job.count, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(AutomatonInd);
            this.FailOnDestroyedOrNull(AmmoInd);
            this.FailOnForbidden(AmmoInd);

            this.FailOn(() =>
            {
                Pawn automaton = Automaton;
                if (automaton == null || automaton.Dead)
                {
                    return true;
                }

                CompAutomatonAmmo comp = automaton.TryGetComp<CompAutomatonAmmo>();
                if (comp == null)
                {
                    return true;
                }

                return comp.IsFull;
            });

            yield return Toils_Goto.GotoThing(AmmoInd, PathEndMode.ClosestTouch);

            Toil pickUpAmmo = ToilMaker.MakeToil("PickUpAutomatonAmmo");
            pickUpAmmo.initAction = delegate
            {
                Thing ammo = Ammo;

                if (ammo == null || ammo.Destroyed || ammo.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int countToTake = job.count > 0 ? job.count : 1;

                if (countToTake > ammo.stackCount)
                {
                    countToTake = ammo.stackCount;
                }

                Pawn automaton = Automaton;
                CompAutomatonAmmo comp = automaton != null ? automaton.TryGetComp<CompAutomatonAmmo>() : null;
                if (comp != null && countToTake > comp.NeededAmmoCount)
                {
                    countToTake = comp.NeededAmmoCount;
                }

                if (countToTake <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing taken = ammo.SplitOff(countToTake);
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
            pickUpAmmo.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickUpAmmo;

            yield return Toils_Goto.GotoThing(AutomatonInd, PathEndMode.Touch);

            Toil loadAmmo = ToilMaker.MakeToil("LoadAutomatonAmmo");
            loadAmmo.initAction = delegate
            {
                Pawn automaton = Automaton;
                if (automaton == null || automaton.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                CompAutomatonAmmo comp = automaton.TryGetComp<CompAutomatonAmmo>();
                if (comp == null || comp.IsFull)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def != comp.AmmoDef || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };

            loadAmmo.defaultCompleteMode = ToilCompleteMode.Delay;

            int duration = 120;
            if (job.count > 1)
            {
                duration += 15 * (job.count - 1);
            }

            loadAmmo.defaultDuration = duration;
            loadAmmo.WithProgressBarToilDelay(AutomatonInd);

            EffecterDef effect = DefDatabase<EffecterDef>.GetNamedSilentFail("Smith");
            if (effect != null)
            {
                loadAmmo.WithEffect(effect, AutomatonInd);
            }

            SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Machining");
            if (sound != null)
            {
                loadAmmo.PlaySustainerOrSound(sound);
            }

            yield return loadAmmo;

            Toil finish = ToilMaker.MakeToil("FinishLoadAutomatonAmmo");
            finish.initAction = delegate
            {
                Pawn automaton = Automaton;
                if (automaton == null || automaton.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                CompAutomatonAmmo comp = automaton.TryGetComp<CompAutomatonAmmo>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def != comp.AmmoDef || carried.stackCount <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                int needed = comp.NeededAmmoCount;
                int available = carried.stackCount;
                int loadCount = needed < available ? needed : available;

                if (loadCount <= 0)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                Thing consumed = carried.SplitOff(loadCount);
                consumed.Destroy(DestroyMode.Vanish);

                if (carried.Destroyed || carried.stackCount <= 0)
                {
                    pawn.carryTracker.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
                }

                int loaded = comp.AddAmmo(loadCount);

                Messages.Message(
                    "오토마톤 전차에 포탄 " + loaded + "발을 적재했습니다.",
                    automaton,
                    MessageTypeDefOf.PositiveEvent,
                    false
                );
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}