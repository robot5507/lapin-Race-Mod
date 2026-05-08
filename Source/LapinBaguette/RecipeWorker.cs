using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LapinBaguette
{
    public class Recipe_InstallBaguetteArm : Recipe_InstallArtificialBodyPart
    {
        public override void ApplyOnPawn(
            Pawn pawn,
            BodyPartRecord part,
            Pawn billDoer,
            List<Thing> ingredients,
            Bill bill)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

            HediffDef baguetteArmDef = DefDatabase<HediffDef>.GetNamedSilentFail("BaguetteArm");
            if (baguetteArmDef == null) return;

            // 핵심: "같은 부위(part)"에 붙은 바게트 팔만 찾기
            Hediff added = pawn.health.hediffSet.hediffs
                .FirstOrDefault(h => h.def == baguetteArmDef && h.Part == part);

            if (added == null)
            {
                Messages.Message(
                    $"{pawn.LabelShort}의 바게트 팔 수술이 실패했다!",
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
                return;
            }

            HediffComp_BaguetteQuality comp = added.TryGetComp<HediffComp_BaguetteQuality>();

            QualityCategory foundQuality = QualityCategory.Normal;

            if (ingredients != null)
            {
                foreach (Thing ingredient in ingredients)
                {
                    if (ingredient.def.defName == "LP_Lapin_Bagette")
                    {
                        CompQuality cq = ingredient.TryGetComp<CompQuality>();
                        if (cq != null)
                            foundQuality = cq.Quality;
                        break;
                    }
                }
            }

            if (comp != null)
                comp.quality = foundQuality;

            Messages.Message(
                $"{pawn.LabelShort}의 팔이 {foundQuality.GetLabel()} 등급 바게트로 대체되었다!",
                pawn,
                MessageTypeDefOf.PositiveEvent
            );
        }
    }
}