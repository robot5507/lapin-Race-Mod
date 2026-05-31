using Verse;

namespace LapinRace
{
    public class CompProperties_MaidAndroidVirtualCooking : CompProperties
    {
        public int cookingLevel = 6;

        public CompProperties_MaidAndroidVirtualCooking()
        {
            compClass = typeof(CompMaidAndroidVirtualCooking);
        }
    }

    public class CompMaidAndroidVirtualCooking : ThingComp
    {
        private CompProperties_MaidAndroidVirtualCooking Props
        {
            get
            {
                return (CompProperties_MaidAndroidVirtualCooking)props;
            }
        }

        public int CookingLevel
        {
            get
            {
                return Props.cookingLevel;
            }
        }
    }
}