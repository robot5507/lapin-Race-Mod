using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LapinRace
{
    public class RecipeWorker_ExtractLapinBrain : Recipe_Surgery
    {
        public override void ApplyOnPawn(
            Pawn pawn,
            BodyPartRecord part,
            Pawn billDoer,
            List<Thing> ingredients,
            Bill bill)
        {
            if (pawn == null)
            {
                return;
            }

            BodyPartRecord brainPart = pawn.health?.hediffSet?.GetBrain();

            bool success = !CheckSurgeryFail(
                billDoer,
                pawn,
                ingredients,
                brainPart,
                bill
            );

            if (!success)
            {
                return;
            }

            ThingDef brainDef = DefDatabase<ThingDef>.GetNamedSilentFail("LP_LapinBrain");

            if (brainDef != null && pawn.Map != null)
            {
                Thing brain = ThingMaker.MakeThing(brainDef);
                brain.stackCount = 1;

                GenPlace.TryPlaceThing(
                    brain,
                    pawn.Position,
                    pawn.Map,
                    ThingPlaceMode.Near
                );
            }

            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.SurgicalCut,
                99999f,
                999f,
                -1f,
                billDoer,
                brainPart
            );

            pawn.TakeDamage(dinfo);

            if (!pawn.Dead)
            {
                pawn.Kill(dinfo);
            }

            Messages.Message(
                "보존된 라핀 뇌를 적출했습니다.",
                new TargetInfo(pawn.PositionHeld, pawn.MapHeld),
                MessageTypeDefOf.NegativeEvent
            );
        }
    }
}