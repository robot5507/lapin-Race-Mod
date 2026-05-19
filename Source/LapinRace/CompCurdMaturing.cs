using RimWorld;
using Verse;

namespace LapinRace
{
    public class CompProperties_CurdMaturing : CompProperties
    {
        public ThingDef productDef;

        public int ticksToMature = 360000; // 6일 = 60000 * 6

        public float minTemperature = 5f;
        public float maxTemperature = 25f;

        public CompProperties_CurdMaturing()
        {
            compClass = typeof(CompCurdMaturing);
        }
    }

    public class CompCurdMaturing : ThingComp
    {
        private int matureTicks;

        private CompProperties_CurdMaturing Props => (CompProperties_CurdMaturing)props;

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (!parent.Spawned || parent.Map == null)
                return;

            float temperature = parent.Position.GetTemperature(parent.Map);

            if (temperature < Props.minTemperature || temperature > Props.maxTemperature)
                return;

            matureTicks += 250;

            if (matureTicks >= Props.ticksToMature)
            {
                TurnIntoCheese();
            }
        }

        private void TurnIntoCheese()
        {
            if (Props.productDef == null)
            {
                Log.Error("[LapinRace] Curd maturing productDef is null.");
                return;
            }

            IntVec3 position = parent.Position;
            Map map = parent.Map;
            int count = parent.stackCount;

            parent.Destroy(DestroyMode.Vanish);

            while (count > 0)
            {
                int stackCount = System.Math.Min(count, Props.productDef.stackLimit);

                Thing cheese = ThingMaker.MakeThing(Props.productDef);
                cheese.stackCount = stackCount;

                GenPlace.TryPlaceThing(
                    cheese,
                    position,
                    map,
                    ThingPlaceMode.Near);

                count -= stackCount;
            }

            Messages.Message(
                "치즈 숙성이 완료되었습니다!",
                new TargetInfo(position, map),
                MessageTypeDefOf.PositiveEvent
            );
        }

        public override string CompInspectStringExtra()
        {
            if (!parent.Spawned || parent.Map == null)
                return null;

            float temperature = parent.Position.GetTemperature(parent.Map);

            if (temperature < Props.minTemperature || temperature > Props.maxTemperature)
            {
                return "숙성 정지: 온도가 적절하지 않음";
            }

            float progress = (float)matureTicks / Props.ticksToMature;
            return "치즈 숙성 중: " + progress.ToStringPercent();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref matureTicks, "matureTicks", 0);
        }
    }
}
