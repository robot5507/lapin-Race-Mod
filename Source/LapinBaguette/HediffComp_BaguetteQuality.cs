using RimWorld;
using Verse;

namespace LapinBaguette
{
    public class HediffCompProperties_BaguetteQuality : HediffCompProperties
    {
        public HediffCompProperties_BaguetteQuality()
        {
            this.compClass = typeof(HediffComp_BaguetteQuality);
        }
    }

    public class HediffComp_BaguetteQuality : HediffComp
    {
        public QualityCategory quality = QualityCategory.Normal;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref quality, "baguetteQuality", QualityCategory.Normal);
        }

        public float DamageMultiplier
        {
            get
            {
                switch (quality)
                {
                    case QualityCategory.Awful: return 0.0f;
                    case QualityCategory.Poor: return 0.2f;
                    case QualityCategory.Normal: return 1.0f;
                    case QualityCategory.Good: return 1.15f;
                    case QualityCategory.Excellent: return 2.3f;
                    case QualityCategory.Masterwork: return 3.55f;
                    case QualityCategory.Legendary: return 4.9f;
                    default: return 1.0f;
                }
            }
        }

        public override string CompLabelInBracketsExtra => quality.GetLabel();

        public override string CompTipStringExtra
        {
            get
            {
                return "바게트 품질: " + quality.GetLabel() + "\n"
                     + "공격력 배율: x" + DamageMultiplier.ToString("0.##");
            }
        }
    }
}
