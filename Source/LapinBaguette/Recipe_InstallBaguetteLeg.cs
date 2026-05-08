using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LapinBaguette
{
    public class Recipe_InstallBaguetteLeg : Recipe_InstallArtificialBodyPart
    {
        public override void ApplyOnPawn(
            Pawn pawn,
            BodyPartRecord part,
            Pawn billDoer,
            List<Thing> ingredients,
            Bill bill)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

            HediffDef baguetteLegDef = DefDatabase<HediffDef>.GetNamedSilentFail("BaguetteLeg");
            if (baguetteLegDef == null) return;

            Hediff added = pawn.health.hediffSet.hediffs
                .FirstOrDefault(h => h.def == baguetteLegDef && h.Part == part);

            if (added == null)
            {
                Messages.Message(
                    $"{pawn.LabelShort}의 바게트 발 수술이 실패했다!",
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
                return;
            }

            Messages.Message(
                $"{pawn.LabelShort}의 발이 맛있는 바게트로 대체되었다!",
                pawn,
                MessageTypeDefOf.PositiveEvent
            );
        }
    }
}