using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    public class CompProperties_DestroyWhenShieldEmpty : CompProperties
    {
        public CompProperties_DestroyWhenShieldEmpty()
        {
            compClass = typeof(CompDestroyWhenShieldEmpty);
        }
    }

    public class CompDestroyWhenShieldEmpty : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();

            if (!parent.IsHashIntervalTick(60))
                return;

            CompShield shield = parent.GetComp<CompShield>();
            if (shield == null) return;

            float energy = Traverse.Create(shield).Field("energy").GetValue<float>();

            if (energy <= 0.001f)
            {
                parent.Destroy();
            }
        }
    }
}