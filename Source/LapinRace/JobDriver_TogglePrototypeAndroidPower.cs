using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobDriver_TogglePrototypeAndroidPower : JobDriver
    {
        private const TargetIndex AndroidInd = TargetIndex.A;

        private Pawn Android
        {
            get
            {
                return job.GetTarget(AndroidInd).Thing as Pawn;
            }
        }

        private bool TurnStandbyOn
        {
            get
            {
                return job.count == 1;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn android = Android;

            if (android == null)
            {
                return false;
            }

            return pawn.Reserve(android, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(AndroidInd);

            this.FailOn(() =>
            {
                Pawn android = Android;

                if (android == null || android.Dead)
                {
                    return true;
                }

                CompPrototypeAndroidWorkControl comp = android.TryGetComp<CompPrototypeAndroidWorkControl>();
                return comp == null;
            });

            yield return Toils_Goto.GotoThing(AndroidInd, PathEndMode.Touch);

            Toil togglePower = ToilMaker.MakeToil("TogglePrototypeAndroidPower");
            togglePower.initAction = delegate
            {
                Pawn android = Android;

                if (android == null || android.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                CompPrototypeAndroidWorkControl comp = android.TryGetComp<CompPrototypeAndroidWorkControl>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (android.pather != null)
                {
                    android.pather.StopDead();
                }
            };

            togglePower.defaultCompleteMode = ToilCompleteMode.Delay;
            togglePower.defaultDuration = 120;
            togglePower.WithProgressBarToilDelay(AndroidInd);

            EffecterDef effect = DefDatabase<EffecterDef>.GetNamedSilentFail("Smith");
            if (effect != null)
            {
                togglePower.WithEffect(effect, AndroidInd);
            }

            SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Machining");
            if (sound != null)
            {
                togglePower.PlaySustainerOrSound(sound);
            }

            yield return togglePower;

            Toil finish = ToilMaker.MakeToil("FinishTogglePrototypeAndroidPower");
            finish.initAction = delegate
            {
                Pawn android = Android;

                if (android == null || android.Dead)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                CompPrototypeAndroidWorkControl comp = android.TryGetComp<CompPrototypeAndroidWorkControl>();
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                comp.SetStandbyMode(TurnStandbyOn);

                if (TurnStandbyOn)
                {
                    Messages.Message(
                        android.LabelShort + "의 전원을 대기 상태로 전환했습니다.",
                        android,
                        MessageTypeDefOf.NeutralEvent,
                        false
                    );
                }
                else
                {
                    Messages.Message(
                        android.LabelShort + "의 전원을 재가동했습니다.",
                        android,
                        MessageTypeDefOf.PositiveEvent,
                        false
                    );
                }
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}
