using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LapinRace
{
    public class CompProperties_StrawberryWineFermenter : CompProperties
    {
        public ThingDef outputDef;
        public int outputAmount = 8;

        public int fermentationTicks = 30000;

        public float minSafeTemperature = 7f;
        public float maxSafeTemperature = 32f;

        public CompProperties_StrawberryWineFermenter()
        {
            compClass = typeof(CompStrawberryWineFermenter);
        }
    }

    public class CompStrawberryWineFermenter : ThingComp
    {
        private int ticksFermented;

        public CompProperties_StrawberryWineFermenter Props =>
            (CompProperties_StrawberryWineFermenter)props;

        private CompRefuelable Refuelable =>
            parent.GetComp<CompRefuelable>();

        private bool FullOfJuice =>
            Refuelable != null && Refuelable.Fuel >= Refuelable.Props.fuelCapacity;

        private float Progress =>
            Props.fermentationTicks <= 0 ? 1f : (float)ticksFermented / Props.fermentationTicks;

        private bool SafeTemperature
        {
            get
            {
                float temp = parent.AmbientTemperature;
                return temp >= Props.minSafeTemperature && temp <= Props.maxSafeTemperature;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!FullOfJuice)
            {
                ticksFermented = 0;
                return;
            }

            if (!SafeTemperature)
                return;

            ticksFermented++;

            if (ticksFermented >= Props.fermentationTicks)
            {
                FinishFermentation();
            }
        }

        private void FinishFermentation()
        {
            CompRefuelable refuelable = Refuelable;

            if (refuelable == null)
            {
                ticksFermented = 0;
                return;
            }

            if (Props.outputDef == null)
            {
                Log.Error("[LapinRace] Strawberry wine fermenter has no outputDef.");
                ticksFermented = 0;
                return;
            }

            // 채워진 딸기주스를 전부 소모
            refuelable.ConsumeFuel(refuelable.Fuel);

            // 와인 생성
            Thing wine = ThingMaker.MakeThing(Props.outputDef);
            wine.stackCount = Props.outputAmount;

            GenPlace.TryPlaceThing(
                wine,
                parent.Position,
                parent.Map,
                ThingPlaceMode.Near
            );

            ticksFermented = 0;

            Messages.Message(
                "딸기와인 발효가 완료되었습니다.",
                parent,
                MessageTypeDefOf.PositiveEvent
            );
        }

        public override string CompInspectStringExtra()
        {
            CompRefuelable refuelable = Refuelable;

            if (refuelable == null)
                return "딸기주스 저장 Comp가 없습니다.";

            if (!FullOfJuice)
            {
                return $"딸기주스가 가득 차야 발효가 시작됩니다.\n안전 온도: {Props.minSafeTemperature}C - {Props.maxSafeTemperature}C";
            }

            if (!SafeTemperature)
            {
                return $"온도가 맞지 않아 발효가 멈춤: {Progress.ToStringPercent()}\n안전 온도: {Props.minSafeTemperature}C - {Props.maxSafeTemperature}C";
            }

            return $"딸기와인 발효 중: {Progress.ToStringPercent()}\n안전 온도: {Props.minSafeTemperature}C - {Props.maxSafeTemperature}C";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ticksFermented, "ticksFermented", 0);
        }
    }
}